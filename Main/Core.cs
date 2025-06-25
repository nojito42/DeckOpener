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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StackedDeckOpener.Main
{
    public class Core : BaseSettingsPlugin<Settings>
    {
        public static Core Plugin { get; private set; }
        public bool IsShortCutEnabled { get; private set; }

        private bool isAnyHovered;
        private CachedValue<bool> ingameUIisVisible;
        private static readonly ConcurrentDictionary<NormalInventoryItem, RectangleF> incubatedItems =
            new ConcurrentDictionary<NormalInventoryItem, RectangleF>();

        private static readonly List<InventoryIndex> slots = new List<InventoryIndex>()
        {
            InventoryIndex.Amulet,
            InventoryIndex.Belt,
            InventoryIndex.Boots,
            InventoryIndex.Chest,
            InventoryIndex.Gloves,
            InventoryIndex.Helm,
            InventoryIndex.LRing,
            InventoryIndex.RRing,
            InventoryIndex.LWeapon,
            InventoryIndex.RWeapon,
            InventoryIndex.PlayerInventory
        };

        public override void OnLoad()
        {
            CanUseMultiThreading = true;
            Name = "Stacked Deck Opener";


        }

        public override bool Initialise()
        {
            var _ingameUI = GameController.IngameState.IngameUi;
            ingameUIisVisible = new TimeCache<bool>(() =>
            {
                return _ingameUI.SyndicatePanel.IsVisibleLocal
                       || _ingameUI.TreePanel.IsVisibleLocal
                       || _ingameUI.Atlas.IsVisibleLocal;
            }, 250);

            Plugin = this;



            return true;
        }

        public override Job Tick()
        {


            if (Input.GetKeyState(Settings.StartOpening.Value))
            {
                IsShortCutEnabled = true;
                Thread.Sleep(100);

            }

            if (Input.GetKeyState(Settings.StopOpening.Value))
            {
                IsShortCutEnabled = false;
                Thread.Sleep(100);

            }

            TickLogic();

            return null;
        }

        public override void Render()
        {
            //if (ingameUIisVisible.Value) return;
            //if (!Settings.LablesWhileHovered && isAnyHovered) return;

            //foreach (var item in incubatedItems.Keys)
            //    Graphics.DrawFrame(incubatedItems[item], Color.YellowGreen, 4);

            var inv = GameController.IngameState.IngameUi.InventoryPanel.GetClientRect();
            var invX = new System.Numerics.Vector2(inv.TopLeft.X - 25.0f, (float)(inv.TopLeft.Y + inv.Height / 1.70f));
            Graphics.DrawFrame(invX, new System.Numerics.Vector2(invX.X - 1, invX.Y - 17.0f), Color.Red, 2.0f, 0, 0);

        }

        private void Input_ReleaseKey(object sender, System.Windows.Forms.Keys e)
        {
            LogError(e.ToString());

            if (e == System.Windows.Forms.Keys.F2)
            {
                LogError(e.ToString());
            }
        }

        private void TickLogic()
        {
            var _ingameUI = GameController.IngameState.IngameUi;

            ingameUIisVisible = new TimeCache<bool>(() =>
            {
                return _ingameUI.SyndicatePanel.IsVisibleLocal
                       || _ingameUI.TreePanel.IsVisibleLocal
                       || _ingameUI.Atlas.IsVisibleLocal;
            }, 250);
            if (GameController.Area.CurrentArea.IsHideout)
            {
                LogError("your in Hideout Noob");
                IsShortCutEnabled = false;
                incubatedItems.Clear();
                return;
            }

            if (ingameUIisVisible.Value || !GameController.IngameState.IngameUi.InventoryPanel.IsVisible)
            {

                LogError("your inventory is not open Fool");
                IsShortCutEnabled = false;
                incubatedItems.Clear();
                return;
            }


            if (Input.GetKeyState(System.Windows.Forms.Keys.F2))
            {
                IsShortCutEnabled = true ;
                if (!IsShortCutEnabled) { return; }
            }

            if (Input.GetKeyState(System.Windows.Forms.Keys.F3))
            {
                IsShortCutEnabled = false;
               return;
            }
            var _inventories = GameController.Game.IngameState.IngameUi.InventoryPanel;
            var _inventoryItemList = new List<NormalInventoryItem>();
            incubatedItems.Clear();

            if (_inventories.IsVisibleLocal)
            {
                foreach (var index in slots.Where(s => s.ToString().Equals(InventoryIndex.PlayerInventory.ToString())))
                {
                    _inventoryItemList.AddRange(_inventories[index].VisibleInventoryItems.Where(i => i != null && i.Item != null && i.Item.Metadata.EndsWith("DivinationCardDeck")));
                }
            }



            foreach (var item in _inventoryItemList.Where(i => i != null))
            {
                if (!IsShortCutEnabled) return;

                if (Settings.Debug) LogMessage($"{Name}: {item.Item.Metadata}");

                var _rectangle = item?.GetClientRect();

                if (_rectangle == null) continue;

                incubatedItems.AddOrUpdate(item, (RectangleF)_rectangle, (key, oldLValue) => (RectangleF)_rectangle);
            }


            if (incubatedItems.Count <= 0)
            {
                LogError("no stacked decks found");
                IsShortCutEnabled = false;
                return;
            }



            if (IsShortCutEnabled)
            {
                if (!IsShortCutEnabled) return;

                var cur = GameController.IngameState.IngameUi.Cursor;
                var inv = GameController.IngameState.IngameUi.InventoryPanel.GetClientRect();
                var invX = new System.Numerics.Vector2(inv.TopLeft.X - 17.0f, (float)(inv.TopLeft.Y + inv.Height / 1.70f));
                var item = incubatedItems.FirstOrDefault();

                Input.SetCursorPos(new Vector2(item.Key.Center.X, item.Key.Center.Y));
                Thread.Sleep(Settings.WaitMS);
                if (GameController.IngameState.UIHover.Entity.Metadata.EndsWith("DivinationCardDeck"))
                {
                    Input.Click(System.Windows.Forms.MouseButtons.Right);
                    Thread.Sleep(Settings.WaitMS);
                    var WTS = GameController.IngameState.Camera.WorldToScreen(GameController.Player.Pos);

                    if (cur.ChildCount > 0)
                    {
                        Input.SetCursorPos(new Vector2(invX.X, invX.Y));
                        Thread.Sleep(Settings.WaitMS);
                        Input.KeyDown(System.Windows.Forms.Keys.LShiftKey);
                        Thread.Sleep(Settings.WaitMS);

                        Input.Click(System.Windows.Forms.MouseButtons.Left);
                        Thread.Sleep(Settings.WaitMS);

                        Input.KeyUp(System.Windows.Forms.Keys.LShiftKey);

                        Thread.Sleep(Settings.WaitMS);
                    }





                }
            }
        }
    }
}
//186