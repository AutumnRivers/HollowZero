using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using HollowZero.Daemons;

namespace HollowZero.Patches
{
    [HarmonyPatch]
    public class InfecTrackerPatch
    {
        public const int SECTIONS = 2;
        public const int MAX_MALWARE = HollowZeroCore.MAX_MALWARE;

        public static readonly Color LowColor = Color.Green;
        public static readonly Color MedColor = Color.Goldenrod;
        public static readonly Color HighColor = Color.Red;

        private static string lastMessage = "malware info";
        private static bool needsMessage = false;
        private static bool mouseUp = true;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OS),nameof(OS.drawModules))]
        public static void ShowInfecTrackerPatch(OS __instance)
        {
            if (HollowZeroCore.GuidebookIsActive) return;

            var topBar = __instance.topBar;
            Rectangle infecTrackerBox = new Rectangle()
            {
                X = topBar.X + (topBar.Width / 5),
                Y = topBar.Y,
                Width = topBar.Width / 2,
                Height = topBar.Height
            };

            int sectionWidth = infecTrackerBox.Width / SECTIONS;
            RenderedRectangle.doRectangle(infecTrackerBox.X, infecTrackerBox.Y,
                infecTrackerBox.Width, infecTrackerBox.Height, Color.Black);

            // Section 1 - Malware Count
            int malwareBoxWidth = sectionWidth / MAX_MALWARE;
            int offset = 0;

            for(var i = 0; i < MAX_MALWARE; i++)
            {
                bool isMalware = HollowZeroCore.CollectedMalware.Count >= i + 1;
                Rectangle malwareBox = new Rectangle()
                {
                    X = infecTrackerBox.X + offset,
                    Y = infecTrackerBox.Y,
                    Width = malwareBoxWidth,
                    Height = infecTrackerBox.Height
                };

                RenderedRectangle.doRectangleOutline(malwareBox.X, malwareBox.Y,
                    malwareBox.Width, malwareBox.Height, 1, (isMalware ? Color.Red : Color.LightGray) * 0.5f);
                HollowDaemon.DrawTrueCenteredText(malwareBox, isMalware ? "< ! >" : "n/a", GuiData.tinyfont,
                    isMalware ? Color.Red : Color.LightGray);

                if (malwareBox.Contains(GuiData.getMousePoint()) && !GuiData.blockingInput)
                {
                    float opacity = 0.25f;
                    if (GuiData.isMouseLeftDown())
                    {
                        mouseUp = false;
                        needsMessage = true;

                        if(isMalware)
                        {
                            var mal = HollowZeroCore.CollectedMalware[i];
                            lastMessage = $"MALWARE: {mal.DisplayName} - {mal.Description}";
                        } else { needsMessage = false; }

                        opacity = 0.15f;
                    } else { mouseUp = true; }

                    if(needsMessage && mouseUp)
                    {
                        __instance.terminal.writeLine(lastMessage);
                        needsMessage = false;
                    }

                    RenderedRectangle.doRectangle(malwareBox.X, malwareBox.Y, malwareBox.Width, malwareBox.Height,
                        (isMalware ? Color.Red : Color.White) * opacity);
                }

                offset += malwareBoxWidth;
            }

            // Section 2 - Infection Level
            int infection = HollowZeroCore.InfectionLevel;
            Color meterColor = infection < 50 ? Color.Lerp(LowColor, MedColor, (float)infection / 50) :
                Color.Lerp(MedColor, HighColor, ((float)infection - 50) / 50);
            Rectangle meterBox = new Rectangle()
            {
                X = infecTrackerBox.X + offset,
                Y = infecTrackerBox.Y,
                Width = sectionWidth,
                Height = infecTrackerBox.Height
            };
            int meterWidth = (int)(meterBox.Width * ((float)infection / 100));

            RenderedRectangle.doRectangle(infecTrackerBox.X + offset,
                infecTrackerBox.Y, meterWidth, infecTrackerBox.Height, meterColor);
            HollowDaemon.DrawTrueCenteredText(meterBox, $"{infection}%", GuiData.tinyfont,
                infection >= 50 ? Color.Black : Color.White);
        }
    }
}
