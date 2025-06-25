using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackedDeckOpener.Main
{
    public class Settings : ISettings
    {
        public Settings ()
        {
            Enable = new ToggleNode(false);
            Debug = new ToggleNode(false);
            LablesWhileHovered = new ToggleNode(false);
        }

        [Menu("Enable")] public ToggleNode Enable { get; set; }
        [Menu("Enable multithreading")] public ToggleNode MultiThreading { get; set; }
        [Menu("Debug mode")] public ToggleNode Debug { get; set; }
        [Menu("Draw labels in inventory while any item is hovered?")] public ToggleNode LablesWhileHovered { get; set; }
        [Menu("Wait MS between clicks")] public RangeNode<int> WaitMS { get; set; } = new RangeNode<int>(10, 1, 200);

        [Menu("Start Opening")] public HotkeyNode StartOpening { get; set; } = new HotkeyNode(System.Windows.Forms.Keys.F2);

        [Menu("Start Opening")] public HotkeyNode StopOpening { get; set; } = new HotkeyNode(System.Windows.Forms.Keys.F3);

    }
}
