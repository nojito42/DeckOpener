using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Keys = System.Windows.Forms.Keys;

namespace StackedDeckOpener.Main
{
    public class Core : BaseSettingsPlugin<Settings>
    {
        public static Core Plugin { get; private set; }
        private CachedValue<bool> ingameUIisVisible;
        private static readonly ConcurrentDictionary<NormalInventoryItem, RectangleF> stackedDecks = new();
        private static readonly List<InventoryIndex> slots = new()
        {
            InventoryIndex.PlayerInventory
        };

        private bool IsOpening = false;
        private bool IsRunning = false;

        public override void OnLoad()
        {
            CanUseMultiThreading = true;
            Name = "Stacked Deck Opener";
        }

        public override bool Initialise()
        {
            Plugin = this;
            ingameUIisVisible = new TimeCache<bool>(() =>
            {
                var ui = GameController?.IngameState?.IngameUi;
                return ui != null && (ui.SyndicatePanel.IsVisibleLocal || ui.TreePanel.IsVisibleLocal || ui.Atlas.IsVisibleLocal);
            }, 250);
            return true;
        }

        public override Job Tick()
        {
            if (Input.GetKeyState(Settings.StartOpening.Value))
            {
                IsOpening = true;
                TryStartOpeningLoop();
            }

            if (Input.GetKeyState(Settings.StopOpening.Value))
            {
                IsOpening = false;
            }

            return null;
        }

        private async void TryStartOpeningLoop()
        {
            if (IsRunning || !IsOpening)
                return;

            IsRunning = true;

            try
            {
                while (IsOpening)
                {
                    if (!IsValidGameState())
                    {
                        IsOpening = false;
                        break;
                    }

                    await FindStackedDecksAsync();

                    if (stackedDecks.Count == 0)
                    {
                        LogError("No stacked decks found.");
                        IsOpening = false;
                        break;
                    }

                    var item = stackedDecks.Keys.FirstOrDefault();
                    if (item != null && item.IsValid)
                    {
                        OpenDeck(item);
                        await Task.Delay(Settings.WaitMS);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            finally
            {
                IsRunning = false;
            }
        }

        private bool IsValidGameState()
        {
            var ui = GameController?.IngameState?.IngameUi;
            if (ui == null || !GameController.Area.CurrentArea.IsHideout && (ingameUIisVisible.Value || !ui.InventoryPanel.IsVisible))
            {
                LogError("Invalid UI state or not in hideout.");
                return false;
            }
            return true;
        }

        private async Task FindStackedDecksAsync()
        {
            stackedDecks.Clear();

            var inventories = GameController?.IngameState?.IngameUi?.InventoryPanel;
            if (inventories == null || !inventories.IsVisibleLocal)
                return;

            foreach (var index in slots)
            {
                var inventory = inventories[index];
                if (inventory == null || inventory.VisibleInventoryItems == null)
                    continue;

                foreach (var item in inventory.VisibleInventoryItems)
                {
                    if (item?.Item?.Metadata?.EndsWith("DivinationCardDeck") == true)
                    {
                        var rect = item.GetClientRect();
                        stackedDecks.TryAdd(item, rect);
                    }
                }
            }

            await Task.Yield(); // make async cooperative
        }

        private void OpenDeck(NormalInventoryItem item)
        {
            if (item == null || !item.IsValid)
                return;

            var rect = item.GetClientRect();

            // Maintenir Shift enfoncé pour tout le processus
            Input.KeyDown(Keys.LShiftKey);
            Thread.Sleep(Settings.WaitMS / 2); // petite pause pour assurer la prise en compte

            // Déplacement souris + clic droit pour ouvrir
            Input.SetCursorPos(new SharpDX.Vector2(rect.Center.X, rect.Center.Y));
            Thread.Sleep(Settings.WaitMS);
            Input.Click(System.Windows.Forms.MouseButtons.Right);
            Thread.Sleep(Settings.WaitMS); // animation ouverture

            // Clic gauche pour drop dans l'inventaire (toujours en maintenant Shift)
            var ui = GameController?.IngameState?.IngameUi;
            var inv = ui?.InventoryPanel?.GetClientRect();
            if (inv != null)
            {
                var dropPos = new System.Numerics.Vector2(inv.Value.TopLeft.X - 20f, inv.Value.TopLeft.Y + inv.Value.Height / 1.70f);
                Input.SetCursorPos(new System.Numerics.Vector2(dropPos.X, dropPos.Y));
                Thread.Sleep(Settings.WaitMS);
                Input.Click(System.Windows.Forms.MouseButtons.Left);
                Thread.Sleep(Settings.WaitMS / 2); // pour la stabilité
            }

            // Relâcher Shift à la toute fin
            Input.KeyUp(Keys.LShiftKey);
        }
    }
}
