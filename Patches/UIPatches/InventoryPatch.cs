using System;

using Hacknet;
using Hacknet.Effects;
using Hacknet.Gui;
using Hacknet.UIUtils;

using HarmonyLib;

using HollowZero.Daemons;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.GUI;

using static HollowZero.HollowLogger;

namespace HollowZero.Patches
{
    [HarmonyPatch]
    public class InventoryPatch
    {
        public const int BOX_OFFSET = GuidebookPatch.BOX_OFFSET;
        public const int BOX_OUTLINE_THICKNESS = GuidebookPatch.BOX_OUTLINE_THICKNESS;

        private static Color InventoryBacking => OS.currentInstance.moduleColorBacking;
        private static Color InventoryBorder => OS.currentInstance.moduleColorSolid;

        private static int ExitButtonID = PFButton.GetNextID();

        private enum TabCategories
        {
            Modifications, Corruptions, Malware
        }
        private static TabCategories ActiveCategory = TabCategories.Modifications;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OS),nameof(OS.drawModules))]
        public static bool DrawInventory(OS __instance, GameTime gameTime)
        {
            if (HollowZeroCore.CurrentUIState != HollowZeroCore.UIState.Inventory) return true;
            if(HollowGlobalManager.TargetTheme == OSTheme.TerminalOnlyBlack)
            {
                HollowZeroCore.CurrentUIState = HollowZeroCore.UIState.Game;
                return true;
            }

            var screen = GuiData.spriteBatch.GraphicsDevice.Viewport;
            Rectangle bounds = new Rectangle()
            {
                X = screen.X + BOX_OFFSET,
                Y = screen.Y + BOX_OFFSET,
                Width = screen.Width - (BOX_OFFSET * 2),
                Height = screen.Height - (BOX_OFFSET * 2)
            };

            RenderedRectangle.doRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, InventoryBacking);
            RenderedRectangle.doRectangleOutline(bounds.X, bounds.Y, bounds.Width, bounds.Height,
                BOX_OUTLINE_THICKNESS, InventoryBorder);

            int categoryCount = Enum.GetValues(typeof(TabCategories)).Length;
            int tabWidth = bounds.Width / categoryCount;

            int tabOffset = 0;
            foreach(var category in Enum.GetNames(typeof(TabCategories)))
            {
                Rectangle tabRect = new Rectangle()
                {
                    X = bounds.X + tabOffset,
                    Y = bounds.Y,
                    Width = tabWidth,
                    Height = 50
                };
                TabCategories cat = (TabCategories)Enum.Parse(typeof(TabCategories), category);
                drawTab(tabRect, cat);

                tabOffset += tabWidth;
            }

            switch(ActiveCategory)
            {
                case TabCategories.Modifications:
                default:
                    listModifications(bounds);
                    break;
                case TabCategories.Corruptions:
                    listCorruptions(bounds);
                    break;
                case TabCategories.Malware:
                    listMalware(bounds);
                    break;
            }

            HollowButton ExitButton = new HollowButton(ExitButtonID,
                bounds.X + bounds.Width - 90, bounds.Y + bounds.Height - 50, 75, 35, "Exit...", Color.Red);
            ExitButton.OnPressed = delegate ()
            {
                HollowZeroCore.CurrentUIState = HollowZeroCore.UIState.Game;
            };
            ExitButton.DoButton();

            return false;
        }

        public readonly static Color InactiveTabColor = Color.Transparent;
        public readonly static Color InactiveTabTextColor = Color.White;
        public readonly static Color InactiveTabUnderlineColor = OS.currentInstance.highlightColor;

        public readonly static Color ActiveTabColor = OS.currentInstance.highlightColor;
        public readonly static Color ActiveTabTextColor = Color.Black;

        public readonly static SpriteFont TabFont = GuiData.font;

        internal static int TallestTabHeight = 0;

        private static void drawTab(Rectangle tab, TabCategories category)
        {
            bool active = ActiveCategory == category;

            Color tabBackingColor = active ? ActiveTabColor : InactiveTabColor;

            if(tab.Height > TallestTabHeight) { TallestTabHeight = tab.Height; }

            if (tab.Contains(GuiData.getMousePoint()))
            {
                if(!active && InactiveTabColor == Color.Transparent)
                {
                    tabBackingColor = ActiveTabColor * 0.3f;
                } else if(active && ActiveTabColor == Color.Transparent)
                {
                    tabBackingColor = InactiveTabColor * 0.3f;
                } else
                {
                    tabBackingColor *= 0.75f;
                }

                if(GuiData.isMouseLeftDown())
                {
                    ActiveCategory = category;
                    GuiData.scrollOffset = Vector2.Zero;
                }
            }

            RenderedRectangle.doRectangle(tab.X, tab.Y, tab.Width, tab.Height, tabBackingColor);
            HollowManager.DrawTrueCenteredText(tab, Enum.GetName(typeof(TabCategories), category),
                TabFont, active ? ActiveTabTextColor : InactiveTabTextColor);

            RenderedRectangle.doRectangle(tab.X, tab.Y + tab.Height - (BOX_OUTLINE_THICKNESS + 2), tab.Width,
                BOX_OUTLINE_THICKNESS + 2, InactiveTabUnderlineColor);
        }

        private static ScrollableSectionedPanel modPanel;

        private static void listModifications(Rectangle bounds)
        {
            int yOffset = TallestTabHeight;
            modPanel ??= new ScrollableSectionedPanel((bounds.Height - TallestTabHeight) / 4, GuiData.spriteBatch.GraphicsDevice);

            modPanel.NumberOfPanels = HollowZeroCore.CollectedMods.Count;

            void drawModificationEntry(Modification mod, Rectangle bounds)
            {
                Rectangle modEntry = new Rectangle()
                {
                    X = bounds.X,
                    Y = bounds.Y,
                    Width = bounds.Width,
                    Height = bounds.Height
                };

                int headerHeight = HollowDaemon.GetStringHeight(GuiData.font, mod.DisplayName);

                TextItem.doLabel(new Vector2(modEntry.X + 15, modEntry.Y + 15), mod.DisplayName, Color.White);

                var trimmedDesc = Utils.SuperSmartTwimForWidth(mod.Description, bounds.Width, GuiData.smallfont);
                TextItem.doSmallLabel(new Vector2(modEntry.X + 15, modEntry.Y + 15 + headerHeight + 10),
                    trimmedDesc, Color.White);

                int footerHeight = HollowDaemon.GetStringHeight(GuiData.tinyfont, "Upgraded: TRUE");
                TextItem.doTinyLabel(new Vector2(modEntry.X + 15, modEntry.Y + modEntry.Height - footerHeight - 15),
                    $"Upgraded: {mod.Upgraded}", Color.White);

                RenderedRectangle.doRectangle(modEntry.X, modEntry.Y + modEntry.Height - 2, modEntry.Width,
                    2, InventoryBorder);

                yOffset += modEntry.Height;
            }

            Action<int, Rectangle, SpriteBatch> drawModList = delegate (int index, Rectangle bounds, SpriteBatch sb)
            {
                drawModificationEntry(HollowZeroCore.CollectedMods[index], bounds);
            };

            Rectangle panel = new Rectangle()
            {
                X = bounds.X,
                Y = bounds.Y + TallestTabHeight,
                Width = bounds.Width,
                Height = bounds.Height - TallestTabHeight
            };
            try
            {
                modPanel.Draw(drawModList, GuiData.spriteBatch, panel);
            } catch(Exception e)
            {
                LogError("<!> Oops. Something BAD happened...");
                LogError(e.ToString());
            }
        }

        private static ScrollableSectionedPanel corPanel;

        private static void listCorruptions(Rectangle bounds)
        {
            int yOffset = TallestTabHeight;
            corPanel ??= new ScrollableSectionedPanel((bounds.Height - TallestTabHeight) / 4, GuiData.spriteBatch.GraphicsDevice);

            corPanel.NumberOfPanels = HollowZeroCore.CollectedCorruptions.Count;

            void drawCorruptionEntry(Corruption cor, Rectangle bounds)
            {
                Rectangle corEntry = new Rectangle()
                {
                    X = bounds.X,
                    Y = bounds.Y,
                    Width = bounds.Width,
                    Height = bounds.Height
                };

                int headerHeight = HollowDaemon.GetStringHeight(GuiData.font, cor.DisplayName);
                Vector2 titleVec = GuiData.font.MeasureString(cor.DisplayName);
                Rectangle titleContainer = new Rectangle()
                {
                    X = corEntry.X + 15, Y = corEntry.Y + 15,
                    Width = (int)titleVec.X, Height = (int)titleVec.Y
                };

                TextItem.doLabel(new Vector2(corEntry.X + 15, corEntry.Y + 15), cor.DisplayName, Color.White);
                FlickeringTextEffect.DrawFlickeringText(titleContainer, cor.DisplayName,
                    5.5f, 0.65f, GuiData.font, OS.currentInstance, Color.White);

                var trimmedDesc = Utils.SuperSmartTwimForWidth(cor.Description, bounds.Width, GuiData.smallfont);
                TextItem.doSmallLabel(new Vector2(corEntry.X + 15, corEntry.Y + 15 + headerHeight + 10),
                    trimmedDesc, Color.White);


                int footerHeight = HollowDaemon.GetStringHeight(GuiData.tinyfont, "Upgraded: TRUE");
                TextItem.doTinyLabel(new Vector2(corEntry.X + 15, corEntry.Y + corEntry.Height - footerHeight - 15),
                    $"Upgraded: {cor.Upgraded} / {cor.StepsLeft} Step(s) Left", Color.White);

                RenderedRectangle.doRectangle(corEntry.X, corEntry.Y + corEntry.Height - 2, corEntry.Width,
                    2, InventoryBorder);

                yOffset += corEntry.Height;
            }

            Action<int, Rectangle, SpriteBatch> drawCorruptionList = delegate (int index, Rectangle bounds, SpriteBatch sb)
            {
                drawCorruptionEntry(HollowZeroCore.CollectedCorruptions[index], bounds);
            };

            Rectangle panel = new Rectangle()
            {
                X = bounds.X,
                Y = bounds.Y + TallestTabHeight,
                Width = bounds.Width,
                Height = bounds.Height - TallestTabHeight
            };

            try
            {
                corPanel.Draw(drawCorruptionList, GuiData.spriteBatch, panel);
            } catch(Exception e)
            {
                LogError("<!> Oops. Something BAD happened...");
                LogError(e.ToString());
            }
        }

        private const int MAX_MALWARE = HollowZeroCore.MAX_MALWARE;
        private static int MalwareIndex = 0;

        public static readonly Color BoxColor = Utils.SlightlyDarkGray;
        public static readonly Color MalwareColor = Color.Red;

        public const float BOX_BACKING_TRANSPARENCY = 0.15f;

        private static void listMalware(Rectangle bounds)
        {
            int malwareBoxWidth = bounds.Width / MAX_MALWARE;
            int malwareBoxHeight = bounds.Height / 3;

            int xOffset = 0;
            bounds.Y += TallestTabHeight;

            for(int i = 0; i < MAX_MALWARE; i++)
            {
                bool isMalware = HollowZeroCore.CollectedMalware.Count > i;
                float opacity = BOX_BACKING_TRANSPARENCY;

                Rectangle malwareBox = new Rectangle(bounds.X + xOffset, bounds.Y, malwareBoxWidth, malwareBoxHeight);
                if (malwareBox.Contains(GuiData.getMousePoint()))
                {
                    opacity = 0.35f;

                    if(GuiData.isMouseLeftDown())
                    {
                        MalwareIndex = i;
                    }
                }

                RenderedRectangle.doRectangle(bounds.X + xOffset, bounds.Y, malwareBoxWidth, malwareBoxHeight,
                    (isMalware ? MalwareColor : BoxColor) * opacity);
                RenderedRectangle.doRectangleOutline(bounds.X + xOffset, bounds.Y, malwareBoxWidth, malwareBoxHeight,
                    1, isMalware ? MalwareColor : BoxColor);

                HollowManager.DrawTrueCenteredText(malwareBox, isMalware ? "!!" : "...", GuiData.titlefont,
                    isMalware ? Color.Red : Color.White);

                xOffset += malwareBoxWidth;
            }

            if(HollowZeroCore.CollectedMalware.Count > MalwareIndex)
            {
                var malware = HollowZeroCore.CollectedMalware[MalwareIndex];
                int titleHeight = HollowDaemon.GetStringHeight(GuiData.titlefont, malware.DisplayName);
                Vector2 titleVec = GuiData.titlefont.MeasureString(malware.DisplayName);
                Rectangle malwareTitleContainer = new Rectangle(bounds.X + 15, bounds.Y + malwareBoxHeight + 15,
                    (int)titleVec.X, (int)titleVec.Y);

                FlickeringTextEffect.DrawLinedFlickeringText(malwareTitleContainer, malware.DisplayName.ToUpper(), 5f, 0.55f,
                    GuiData.titlefont, OS.currentInstance, Color.Pink);

                var trimmedDesc = Utils.SuperSmartTwimForWidth(malware.Description, bounds.Width - 30, GuiData.font);
                TextItem.doLabel(new Vector2(bounds.X + 15, bounds.Y + malwareBoxHeight + titleHeight + 15),
                    trimmedDesc, Color.White);
            }
        }

        private static void showProgress(Rectangle bounds) { }
    }
}
