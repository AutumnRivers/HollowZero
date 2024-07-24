using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using HarmonyLib;

using Pathfinder.GUI;

using HollowZero.Daemons;

namespace HollowZero.Patches
{
    [HarmonyPatch]
    public class GuidebookPatch
    {
        public const int BOX_OFFSET = 35;
        public const int BOX_OUTLINE_THICKNESS = 1;
        public const int ENTRIES_WIDTH = 200;

        private static Color GuidebookBacking => OS.currentInstance.moduleColorBacking;
        private static Color GuidebookBorder => OS.currentInstance.moduleColorSolid;
        private static readonly int ExitButtonID = PFButton.GetNextID();
        private static readonly int SelectListID = PFButton.GetNextID();

        internal static List<string> GuidebookEntryTitles = new List<string>();
        internal static List<GuidebookEntry> GuidebookEntries = DefaultGuidebookEntries.entries;

        private static int selectedEntry = -1;
        private static int guidebookScroll = 0;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS),nameof(OS.drawModules))]
        public static bool ShowGuidebook(OS __instance)
        {
            if (!HollowZeroCore.GuidebookIsActive) return true;

            var screen = GuiData.spriteBatch.GraphicsDevice.Viewport;
            Rectangle guidebookBounds = new Rectangle()
            {
                X = screen.X + BOX_OFFSET,
                Y = screen.Y + BOX_OFFSET,
                Width = screen.Width - (BOX_OFFSET * 2),
                Height = screen.Height - (BOX_OFFSET * 2)
            };
            Rectangle headerBounds = new Rectangle()
            {
                X = screen.X, Y = screen.Y + 5, Width = screen.Width,
                Height = BOX_OFFSET - 5
            };

            RenderedRectangle.doRectangle(guidebookBounds.X, guidebookBounds.Y, guidebookBounds.Width, guidebookBounds.Height, GuidebookBacking);
            RenderedRectangle.doRectangleOutline(guidebookBounds.X, guidebookBounds.Y, guidebookBounds.Width, guidebookBounds.Height,
                BOX_OUTLINE_THICKNESS, GuidebookBorder);

            HollowDaemon.DrawTrueCenteredText(headerBounds, "Hollow Zero Guidebook", GuiData.smallfont, GuidebookBorder);

            var exitButton = Button.doButton(ExitButtonID, guidebookBounds.X + guidebookBounds.Width - 160,
                guidebookBounds.Y + 10, 150, 35, "Close Guidebook", Color.Red);

            if(exitButton) { HollowZeroCore.GuidebookIsActive = false; }

            // Scrollable List
            SelectableTextList.scrollOffset = guidebookScroll;
            selectedEntry = SelectableTextList.doFancyList(SelectListID,
                guidebookBounds.X, guidebookBounds.Y, ENTRIES_WIDTH, guidebookBounds.Height,
                GuidebookEntryTitles.ToArray(), selectedEntry, __instance.highlightColor, true);
            guidebookScroll = SelectableTextList.scrollOffset;

            RenderedRectangle.doRectangle(guidebookBounds.X + ENTRIES_WIDTH, guidebookBounds.Y,
                1, guidebookBounds.Height, __instance.moduleColorSolid);

            DrawEntryContent();

            return false;
        }

        private static readonly GuidebookEntry DefaultEntry = new GuidebookEntry()
        {
            Title = "Welcome to Hollow Zero",
            Content = "This guidebook will walk you through everything you need to know about Hollow Zero (HZ).\n\n" +
            "To get started, click an entry on the left of the guidebook."
        };

        private static void DrawEntryContent()
        {
            var currentEntry = selectedEntry > -1 && selectedEntry < GuidebookEntries.Count ? GuidebookEntries[selectedEntry] : DefaultEntry;
            var screen = GuiData.spriteBatch.GraphicsDevice.Viewport;
            Rectangle guidebookBounds = new Rectangle()
            {
                X = screen.X + BOX_OFFSET,
                Y = screen.Y + BOX_OFFSET,
                Width = screen.Width - (BOX_OFFSET * 2),
                Height = screen.Height - (BOX_OFFSET * 2)
            };
            Rectangle contentBounds = new Rectangle()
            {
                X = guidebookBounds.X + ENTRIES_WIDTH + 20,
                Y = guidebookBounds.Y + 10,
                Width = guidebookBounds.Width - (guidebookBounds.X + ENTRIES_WIDTH + 20),
                Height = guidebookBounds.Height - 20
            };

            int yOffset = 0;

            TextItem.doLabel(new Vector2(contentBounds.X, contentBounds.Y + yOffset), currentEntry.Title, Color.White);
            yOffset += HollowDaemon.GetStringHeight(GuiData.font, currentEntry.Title) + 5;
            string content = Utils.SmartTwimForWidth(currentEntry.Content, contentBounds.Width, GuiData.smallfont);
            TextItem.doSmallLabel(new Vector2(contentBounds.X, contentBounds.Y + yOffset), content, Color.White);
        }
    }

    internal class GuidebookEntry
    {
        public string Title;
        public string Content;
        public string ShortTitle;
    }
}
