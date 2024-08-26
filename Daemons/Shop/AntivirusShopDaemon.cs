using Hacknet;
using Hacknet.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero.Daemons.Shop
{
    public class AntivirusShopDaemon : ShopDaemon
    {
        public AntivirusShopDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public override string Identifier => "Aive's E-Med Center";
        public static new bool Registerable => true;

        public const int BASE_COST = 350;

        public readonly int RemoveMalwareCost = BASE_COST * 2;
        public readonly float RemoveMalwareMultiplier = 1.5f;

        public readonly int LowerInfectionCost = BASE_COST;
        public readonly float LowerInfectionMultiplier = 1.25f;
        public const int LOWER_INFECTION_BY = 30;

        public int RemoveCorruptionCost => RemoveMalwareCost + LowerInfectionCost + 100;
        public int UpgradeCorruptionChance = 10;

        public const int MAX_MALWARE_REMOVAL = 2;
        public const int MAX_INFECTION_DECREASES = 5;
        public const int UPGRADE_CORRUPTION_THRESHOLD = 3;

        private int malwareRemovalsPurchased = 0;
        private int infectionDecreasesPurchased = 0;
        private int corruptionRemovalsPurchased = 0;
        private int corruptionsUpgraded = 0;

        /*
         * 
         * Buttons:
         * 1. Enter Shop
         * 2. Exit Shop
         * 3. Remove Malware
         * 4. Lower Infection
         * 5. Enter Corruption Removal
         * 6. Exit Corruption Removal
         * 7. Remove Corruption
         * 8. Meet Aive
         * 
         */
        private readonly int[] ButtonIDs = new int[8];

        public override void navigatedTo()
        {
            for(var i = 0; i < ButtonIDs.Length; i++)
            {
                ButtonIDs[i] = PFButton.GetNextID();
            }

            base.navigatedTo();
        }

        internal override void OnDisconnect()
        {
            foreach(var id in ButtonIDs)
            {
                PFButton.ReturnID(id);
            }

            RecreateValuesFileIfMissing();

            base.OnDisconnect();
        }

        private const string VALUES_FILENAME = "ShopValues";
        private const char VALUES_SEPERATOR = '|';

        private void RecreateValuesFileIfMissing()
        {
            var sys = comp.getFolderFromPath("sys");

            if(sys.containsFile(VALUES_FILENAME))
            {
                CheckValuesFile();
                return;
            }

            CreateValuesFile();
        }

        private void CheckValuesFile()
        {
            bool valid = true;
            var sys = comp.getFolderFromPath("sys");
            sys.TryFindFile(VALUES_FILENAME, out var valFile);
            var values = valFile.data.Split('\n');
            if(values.Length != 3)
            {
                sys.files.Remove(valFile);
                CreateValuesFile();
                return;
            }

            var malware = values[0];
            var infection = values[1];
            var corruption = values[2];

            // Check Malware Values
            try
            {
                var mValues = malware.Split(VALUES_SEPERATOR);

                int malwareBought = int.Parse(mValues[1]);
                int malwareCost = int.Parse(mValues[2]);

                valid = valid ? malwareBought == malwareRemovalsPurchased : false;
                valid = valid ? malwareCost == RemoveMalwareCost : false;
            }
            catch(Exception e)
            {
                valid = false;
            }

            // Check Infection Values
            try
            {
                var infecValues = infection.Split(VALUES_SEPERATOR);

                int infectionsBought = int.Parse(infecValues[1]);
                int infectionsCost = int.Parse(infecValues[2]);

                valid = valid ? infectionsBought == infectionDecreasesPurchased : false;
                valid = valid ? infectionsCost == LowerInfectionCost : false;
            }
            catch (Exception e)
            {
                valid = false;
            }

            // Check Corruption Values
            try
            {
                var corValues = corruption.Split(VALUES_SEPERATOR);

                int corrsBought = int.Parse(corValues[1]);
                int corrsCost = int.Parse(corValues[2]);
                int corrsChance = int.Parse(corValues[3]);

                valid = valid ? corrsBought == corruptionRemovalsPurchased : false;
                valid = valid ? corrsCost == RemoveCorruptionCost : false;
                valid = valid ? corrsChance == UpgradeCorruptionChance : false;
            }
            catch (Exception e)
            {
                valid = false;
            }

            if (!valid)
            {
                sys.files.Remove(valFile);
                CreateValuesFile();
            }
        }

        private void CreateValuesFile()
        {
            StringBuilder malwareValues = new StringBuilder("MALWARE" + VALUES_SEPERATOR);
            malwareValues.Append(malwareRemovalsPurchased + VALUES_SEPERATOR);
            malwareValues.Append(RemoveMalwareCost);
            malwareValues.Append('\n');

            StringBuilder infectionValues = new StringBuilder("INFECTION" + VALUES_SEPERATOR);
            infectionValues.Append(infectionDecreasesPurchased + VALUES_SEPERATOR);
            infectionValues.Append(LowerInfectionCost);
            infectionValues.Append('\n');

            StringBuilder corruptionValues = new StringBuilder("CORRUPTION" + VALUES_SEPERATOR);
            corruptionValues.Append(corruptionRemovalsPurchased + VALUES_SEPERATOR);
            corruptionValues.Append(RemoveCorruptionCost + VALUES_SEPERATOR);
            corruptionValues.Append(UpgradeCorruptionChance);

            var finalValues = malwareValues.ToString() + infectionValues.ToString() + corruptionValues.ToString();

            FileEntry valuesFile = new(finalValues, VALUES_FILENAME);
            comp.getFolderFromPath("sys").files.Add(valuesFile);
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            DrawBackground(bounds);

            if(!HollowZeroCore.SeenEvents.Contains("met_aive"))
            {
                DrawGreeting(bounds);
                return;
            }

            switch(CurrentScreen)
            {
                case StoreScreen.Main:
                    DrawMainScreen(bounds);
                    break;
                case StoreScreen.Shop:
                    DrawShop(bounds);
                    break;
                case StoreScreen.CorrShop:
                    DrawCorruptionRemoval(bounds);
                    break;
            }
        }

        private Texture2D kellisLogo => OS.currentInstance.display.compAltIcons["kellis"];
        private Color patternColor = OS.currentInstance.brightUnlockedColor;
        private Color altPatternColor = Color.Black;
        private float patternOffset = 0.5f;

        private void DrawBackground(Rectangle bounds)
        {
            PatternDrawer.draw(bounds, patternOffset, altPatternColor * 0.5f,
                patternColor * 0.12f, GuiData.spriteBatch);
            Rectangle logoBounds = new()
            {
                X = (int)((bounds.X + (bounds.Width / 2)) * 0.5f),
                Y = (int)((bounds.Y + (bounds.Width / 2)) * 0.5f),
                Width = (int)(bounds.Width / 2 * 1.55f),
                Height = bounds.Width / 2
            };
            GuiData.spriteBatch.Draw(kellisLogo, logoBounds, Color.White * 0.25f);
        }

        public const string GREETING_HEADER_TEXT = "And... you are?";
        public const float GREETING_HEADER_SCALE = 1.3f;

        public const string GREETING_BODY_TEXT = "Oh, you're #PLAYERNAME#. I've heard a bit about you. " +
            "My name is Aive, and I am a sentient Artificial Intelligence designed for Computer Security and Antiviruses.\n\n" +
            "It is a pleasure to make your acquantince. Please, allow me to assist you in any way I can.";

        public const int HEADER_OFFSET = 20;

        private void DrawGreeting(Rectangle bounds)
        {
            int headerHeight = (int)(GetStringHeight(GuiData.font, GREETING_HEADER_TEXT) * GREETING_HEADER_SCALE);
            DrawCenteredScaleText(bounds, GREETING_HEADER_TEXT, GuiData.font, headerHeight + HEADER_OFFSET, Color.White, GREETING_HEADER_SCALE);

            var filteredBodyText = ComputerLoader.filter(GREETING_BODY_TEXT);
            filteredBodyText = Utils.SuperSmartTwimForWidth(filteredBodyText, bounds.Width - 50, GuiData.font);
            TextItem.doLabel(new Vector2(bounds.X + 25, bounds.Y + headerHeight + HEADER_OFFSET + 50), filteredBodyText, Color.White);

            HollowButton metAiveButton = new(ButtonIDs[7], bounds.X + (bounds.Width / 3),
                bounds.Y + bounds.Height - (50 + HEADER_OFFSET), bounds.Width / 3, 50, "I'll keep that in mind.\n(Met Aive)", Color.Green);
            metAiveButton.OnPressed = delegate ()
            {
                HollowZeroCore.SeenEvents.Add("met_aive");
            };
            metAiveButton.DoButton();
        }

        public const string MAIN_SCREEN_HEADER_TEXT = "Welcome back!";
        public const string MAIN_SCREEN_BODY_TEXT = "Let me know how I can help, #PLAYERNAME#.";

        private void DrawMainScreen(Rectangle bounds)
        {
            patternColor = OS.currentInstance.brightUnlockedColor;
            altPatternColor = Color.Black;
            patternOffset = 0.5f;

            int headerHeight = (int)(GetStringHeight(GuiData.font, MAIN_SCREEN_HEADER_TEXT) * GREETING_HEADER_SCALE);
            DrawCenteredScaleText(bounds, MAIN_SCREEN_HEADER_TEXT, GuiData.font, headerHeight + HEADER_OFFSET, Color.White, GREETING_HEADER_SCALE);

            var filteredBodyText = ComputerLoader.filter(MAIN_SCREEN_BODY_TEXT);
            int bodyHeight = GetStringHeight(GuiData.font, filteredBodyText);
            DrawCenteredText(bounds, filteredBodyText, GuiData.font, headerHeight + HEADER_OFFSET + bodyHeight + 10, Color.White);

            int buttonX = bounds.X + (bounds.Width / 3);
            int buttonWidth = bounds.Width / 3;

            HollowButton enterShopButton = new(ButtonIDs[0], buttonX,
                bounds.Y + bounds.Height - (65 * 2), buttonWidth, 50,
                "I ain't feeling too hot, doc.\n(Lower Infec./Remove Malware)", Color.Red);
            enterShopButton.OnPressed = delegate () { CurrentScreen = StoreScreen.Shop; };
            enterShopButton.DoButton();

            HollowButton enterCorrRemovalButton = new(ButtonIDs[4], buttonX,
                bounds.Y + bounds.Height - 65, buttonWidth, 50,
                "Something just feels off.\n(Remove Corruption)", Color.DarkRed);
            enterCorrRemovalButton.OnPressed = delegate () { CurrentScreen = StoreScreen.CorrShop; };
            enterCorrRemovalButton.DoButton();
        }

        public const string SHOP_HEADER_TEXT = "Infec./Mal. Removal";
        private string ShopMessage = "Sure, let me see what I can do.";

        private readonly string[] shopResponses = new string[]
        {
            "That should calm you down.",    // Lower Infection
            "There, good as new.",           // Remove Malware
            "Sorry, I need time to reboot."  // Out of Infection/Malware Removals
        };

        private void DrawShop(Rectangle bounds)
        {
            patternColor = OS.currentInstance.brightUnlockedColor;
            altPatternColor = Color.Black;
            patternOffset = 0.5f;

            int headerHeight = (int)(GetStringHeight(GuiData.font, SHOP_HEADER_TEXT) * GREETING_HEADER_SCALE);
            DrawCenteredScaleText(bounds, SHOP_HEADER_TEXT, GuiData.font, headerHeight + HEADER_OFFSET, Color.White, GREETING_HEADER_SCALE);

            int bodyHeight = GetStringHeight(GuiData.font, ShopMessage);
            DrawCenteredText(bounds, ShopMessage, GuiData.font, headerHeight + HEADER_OFFSET + bodyHeight + 10, Color.White);

            int malwareCost = RemoveMalwareCost;
            int infecCost = LowerInfectionCost;

            if(malwareRemovalsPurchased > 0)
            {
                malwareCost = (int)(Math.Round(malwareCost * (malwareRemovalsPurchased * RemoveMalwareMultiplier) / 5.0) * 5);
            }
            if(infectionDecreasesPurchased > 0)
            {
                infecCost = (int)(Math.Round(infecCost * (infectionDecreasesPurchased * LowerInfectionMultiplier) / 5.0) * 5); 
            }

            int buttonX = bounds.X + (bounds.Width / 4);
            int buttonWidth = bounds.Width / 2;

            HollowButton removeMalwareButton = new(ButtonIDs[2], buttonX, bounds.Center.Y + 25,
                buttonWidth, 50, $"Remove Malware (${malwareCost})", Color.Red);
            if (HollowZeroCore.PlayerCredits < malwareCost)
            {
                removeMalwareButton.Disabled = true;
                removeMalwareButton.DisabledMessage = "<!> You don't have enough credits for that!";
            } else if(!HollowZeroCore.CollectedMalware.Any())
            {
                removeMalwareButton.Disabled = true;
                removeMalwareButton.DisabledMessage = "<...> You don't have any malware to remove.";
            }
            if (malwareRemovalsPurchased >= MAX_MALWARE_REMOVAL)
            {
                removeMalwareButton.Disabled = true;
                removeMalwareButton.DisabledMessage = "<!> " + shopResponses[2];
                removeMalwareButton.Text = "Remove Malware (UNAVAILABLE)";
            }
            removeMalwareButton.OnPressed = delegate ()
            {
                HollowZeroCore.RemovePlayerCredits(malwareCost);
                HollowZeroCore.RemoveMalware(HollowZeroCore.CollectedMalware[0]);
                ShopMessage = shopResponses[1];
                malwareRemovalsPurchased++;
            };
            removeMalwareButton.DoButton();

            HollowButton lowerInfectionButton = new(ButtonIDs[3], buttonX, bounds.Center.Y + 25 + 50 + 10,
                buttonWidth, 50, $"Lower Infection by {LOWER_INFECTION_BY}% (${infecCost})", Color.DarkRed);
            if (HollowZeroCore.PlayerCredits < infecCost)
            {
                lowerInfectionButton.Disabled = true;
                lowerInfectionButton.DisabledMessage = "<!> You don't have enough credits for that!";
            } else if(HollowZeroCore.InfectionLevel == 0)
            {
                lowerInfectionButton.Disabled = true;
                lowerInfectionButton.DisabledMessage = "<...> Your infection level is already at 0%...";
            }
            if(infectionDecreasesPurchased >= MAX_INFECTION_DECREASES)
            {
                lowerInfectionButton.Disabled = true;
                lowerInfectionButton.DisabledMessage = "<!> " + shopResponses[2];
                lowerInfectionButton.Text = "Lower Infection (UNAVAILABLE)";
            }
            lowerInfectionButton.OnPressed = delegate ()
            {
                HollowZeroCore.RemovePlayerCredits(infecCost);
                HollowZeroCore.DecreaseInfection(LOWER_INFECTION_BY);
                ShopMessage = shopResponses[0];
                infectionDecreasesPurchased++;
            };
            lowerInfectionButton.DoButton();

            HollowButton exitButton = new(ButtonIDs[1], buttonX, bounds.Center.Y + 25 + 50 + 10 + 50 + 10,
                buttonWidth, 25, "Exit Shop...", Color.Black);
            exitButton.OnPressed = delegate ()
            {
                CurrentScreen = StoreScreen.Main;
            };
            exitButton.DoButton();
        }

        public const string CORR_HEADER_TEXT = "Corr. Removal (Alpha)";
        private string CorrRemovalMessage = "Just so you know, this is still in alpha...";

        private bool failFlash = false;
        private float failProg = 0.0f;

        private readonly string[] corrResponses = new string[]
        {
            "And... removed. Bless my steady hands.",           // Corruption Successfully Removed
            "Oh, dear... I apologize.",                         // Corruption Accidentally Upgraded
            "I'm sorry, but it's best if we leave it for now."  // Corruption Upgrade Threshold Reached
        };

        private int DebugFailButtonID = PFButton.GetNextID();

        private void DrawCorruptionRemoval(Rectangle bounds)
        {
            if(!failFlash)
            {
                patternColor = OS.currentInstance.brightLockedColor;
                patternOffset = -0.5f;
                failProg = 0.0f;
            } else
            {
                failProg += (float)OS.currentInstance.lastGameTime.ElapsedGameTime.TotalSeconds * 0.5f;
                failFlash = failProg < 1.0f;
                patternColor = Color.Lerp(Color.Red, OS.currentInstance.brightLockedColor, failProg);
                altPatternColor = Color.Lerp(Color.Red, Color.Black, failProg);
                patternOffset = MathHelper.Lerp(-5.0f, -0.5f, failProg);
            }

            int headerHeight = (int)(GetStringHeight(GuiData.font, CORR_HEADER_TEXT) * GREETING_HEADER_SCALE);
            DrawCenteredScaleText(bounds, CORR_HEADER_TEXT, GuiData.font, headerHeight + HEADER_OFFSET, Color.LightPink, GREETING_HEADER_SCALE);
            int bodyHeight = GetStringHeight(GuiData.font, CorrRemovalMessage);

            if(!failFlash)
            {
                DrawCenteredText(bounds, CorrRemovalMessage, GuiData.font, headerHeight + HEADER_OFFSET + bodyHeight + 10, Color.White);
            } else
            {
                DrawCenteredFlickeringText(bounds, CorrRemovalMessage, GuiData.font, headerHeight + HEADER_OFFSET + bodyHeight + 10, Color.White);
            }

            int corrRemovalCost = (int)(RemoveCorruptionCost * ((corruptionRemovalsPurchased + 1) * 0.5f));
            if(HollowZeroCore.CollectedCorruptions.Any(c => c.Upgraded))
            {
                corrRemovalCost += HollowZeroCore.CollectedCorruptions.Where(c => c.Upgraded).Count() * 50;
            }

            int buttonX = bounds.X + (bounds.Width / 4);
            int buttonWidth = bounds.Width / 2;
            HollowButton removeCorrButton = new(ButtonIDs[6], buttonX, bounds.Center.Y + 25, buttonWidth,
                50, $"Remove Random Corr. (${corrRemovalCost})", Color.Red);
            if(!HollowZeroCore.CollectedCorruptions.Any())
            {
                removeCorrButton.Disabled = true;
                removeCorrButton.DisabledMessage = "<...> You don't have any corruptions to remove.";
            } else if(HollowZeroCore.PlayerCredits < corrRemovalCost)
            {
                removeCorrButton.Disabled = true;
                removeCorrButton.DisabledMessage = "<!> You don't have enough credits for that!";
            } else if(corruptionsUpgraded >= UPGRADE_CORRUPTION_THRESHOLD || UpgradeCorruptionChance >= 95)
            {
                removeCorrButton.Disabled = true;
                removeCorrButton.DisabledMessage = "<!> " + corrResponses[2];
                removeCorrButton.Text = "[[ DISABLED ]]";
            }
            removeCorrButton.OnPressed = delegate ()
            {
                Random random = new();
                int chance = random.Next(0, 100);
                bool upgrade = chance <= UpgradeCorruptionChance;

                if(upgrade)
                {
                    corruptionsUpgraded++;
                    if(HollowZeroCore.CollectedCorruptions.Any(c => !c.Upgraded))
                    {
                        var corr = HollowZeroCore.CollectedCorruptions.Where(c => !c.Upgraded).GetRandom();
                        HollowZeroCore.UpgradeCorruption(corr);
                    }
                    CorrRemovalMessage = corrResponses[1];
                    failFlash = true;
                } else
                {
                    HollowZeroCore.CollectedCorruptions.GetRandom().Discard();
                    CorrRemovalMessage = corrResponses[0];
                }

                HollowZeroCore.RemovePlayerCredits(corrRemovalCost);
                UpgradeCorruptionChance += 15;
                corruptionRemovalsPurchased++;
            };
            removeCorrButton.DoButton();

            HollowButton exitButton = new(ButtonIDs[5], buttonX, bounds.Center.Y + 95,
                buttonWidth, 25, "Exit...", Color.Black);
            exitButton.OnPressed = delegate () { CurrentScreen = StoreScreen.Main; };
            exitButton.DoButton();

            if(OS.DEBUG_COMMANDS)
            {
                HollowButton debugFailButton = new(DebugFailButtonID, buttonX, bounds.Center.Y + 95 + 25 + 10,
                    buttonWidth, 25, "Debug Fail", Color.Red);
                debugFailButton.OnPressed = delegate () {
                    failFlash = true;
                    CorrRemovalMessage = corrResponses[1];
                };
                debugFailButton.DoButton();
            }
        }
    }
}
