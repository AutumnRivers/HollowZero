using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Hacknet;
using Hacknet.Gui;

using HollowZero.Managers;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.GUI;

using static HollowZero.Managers.PlayerManager;
using static HollowZero.Nodes.LayerSystem.LayerGenerator;

namespace HollowZero.Daemons.Shop
{
    public class ProgramShopDaemon : ShopDaemon
    {
        public ProgramShopDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public static new bool Registerable => true;

        public override string Identifier => "Program Shop";

        public const int MAX_ITEMS = 5;

        private List<HollowProgram> UserPrograms = new();
        private readonly List<HollowBundle> ProgramBundles = new();

        private readonly Dictionary<string, int> ItemsForSale = new();
        private HollowBundle CurrentBundleForSale;

        private HollowProgram PriorityProgram = null;

        /*
         * Buttons Needed:
         * 1. Main Screen -> Shop
         * 2. Shop -> Main Screen
         * 3. Featured Bundle
         * 4. Confirm Purchase
         * 5. Exit Purchase
         * 6. OK Button
         * 7. Just in case...
         */
        private readonly int[] ButtonIDs = new int[7 + MAX_ITEMS];

        public override void RobStore(string itemName)
        {
            OS os = OS.currentInstance;
            if(hasBeenRobbed)
            {
                os.write("<!> This shop is on high alert! You can't steal from it again.");
                return;
            }
            itemName = itemName.ToLower();
            if(!CanDecrypt && !itemName.StartsWith("dec suite") && CurrentBundleForSale?.ID == "DEC Suite")
            {
                badIdea();
            } else if(!CanMemDump && !itemName.StartsWith("memory master") && CurrentBundleForSale?.ID == "Memory Master")
            {
                badIdea();
            } else if(!CanWireshark && itemName != "wireshark" && CurrentLayer >= 10)
            {
                badIdea();
            } else
            {
                if(ItemsForSale.Any(i => i.Key.ToLower().StartsWith(itemName)))
                {
                    var program = ItemsForSale.First(i => i.Key.ToLower().StartsWith(itemName));
                    int itemCost = GetFinalPrice(program.Value);
                    if(itemCost > 1000)
                    {
                        os.write("<!> You can't steal that item -- it's very expensive, so it's on lockdown!");
                        return;
                    }
                    var userProgram = GetProgramByName(program.Key);
                    AddProgramToPlayerPC(userProgram.DisplayName, userProgram.FileContent);
                    int infectionLevelRise = (int)Math.Floor(itemCost * 0.1f);
                    IncreaseInfection(infectionLevelRise, true);
                    ItemsForSale.Remove(program.Key);
                    hasBeenRobbed = true;
                    os.write($"<!*!> You snatch the program, but set off the alarm. +{infectionLevelRise} Infection.");
                    os.beepSound.Play();
                } else if(CurrentBundleForSale.ID.ToLower().StartsWith(itemName))
                {
                    int bundleCost = GetFinalPrice(CurrentBundleForSale.Price);
                    if(bundleCost > 1000)
                    {
                        os.write("<!> You can't steal that bundle -- it's very expensive, so it's on lockdown!");
                        return;
                    }
                    foreach(var program in CurrentBundleForSale.Programs)
                    {
                        AddProgramToPlayerPC(program.DisplayName, program.FileContent);
                    }
                    int infectionLevelRise = (int)Math.Floor(bundleCost * 0.1f);
                    IncreaseInfection(infectionLevelRise, true);
                    CurrentBundleForSale = null;
                    hasBeenRobbed = true;
                    os.write($"<!*!> As you load the programs onto your PC, you set off the alarm. +{infectionLevelRise} Infection.");
                    os.beepSound.Play();
                } else
                {
                    os.write("<!> That item doesn't exist! Are you sure you typed it correctly?");
                    return;
                }
                os.write("<!*!> The alarm's been tripped. You can't steal from this shop again.");
            }

            void badIdea()
            {
                os.write("<!> Bad idea. You should steal something you need.");
                os.warningFlash();
            }
        }

        public override void initFiles()
        {
            base.initFiles();

            for (var i = 0; i < ButtonIDs.Length; i++)
            {
                ButtonIDs[i] = PFButton.GetNextID();
            }

            UserPrograms.Clear();
            UserPrograms = GetUserPrograms();

            PropagateProgramsForSale();

            List<HollowProgram> DecSuiteBundlePrograms = new List<HollowProgram>()
            {
                BaseGamePrograms.First(ByName("Decypher")),
                BaseGamePrograms.First(ByName("DECHead"))
            };
            ProgramBundles.Add(new("DEC Suite", 500, DecSuiteBundlePrograms));

            List<HollowProgram> MemSuiteBundlePrograms = new List<HollowProgram>()
            {
                BaseGamePrograms.First(ByName("MemForensics")),
                BaseGamePrograms.First(ByName("MemDumpGenerator"))
            };
            ProgramBundles.Add(new("Memory Master", 500, MemSuiteBundlePrograms));

            List<HollowProgram> ShellMasterBundlePrograms = new List<HollowProgram>()
            {
                BaseGamePrograms.First(ByName("OpShell")),
                BaseGamePrograms.First(ByName("ComShell"))
            };
            ProgramBundles.Add(new("Shell Shire", 1000, ShellMasterBundlePrograms));

            List<HollowProgram> ClockBundle = new List<HollowProgram>()
            {
                BaseGamePrograms.First(ByName("Clock")),
                BaseGamePrograms.First(ByName("HexClock")),
                BaseGamePrograms.First(ByName("ClockV2"))
            };
            ProgramBundles.Add(new("Time Trailblazer", 5000, ClockBundle));

            List<HollowBundle> RemoveBundles = new();
            foreach (var bundle in ProgramBundles)
            {
                foreach (var program in bundle.Programs)
                {
                    RemoveProgram(program.DisplayName);

                    if (UserPrograms.Contains(program))
                    {
                        if (!RemoveBundles.Contains(bundle))
                        {
                            RemoveBundles.Add(bundle);
                        }
                    }
                }
            }
            foreach (var bundle in RemoveBundles)
            {
                ProgramBundles.Remove(bundle);
            }

            if (CheckForItemFile()) return;

            foreach (var program in UserPrograms)
            {
                RemoveProgram(program.DisplayName);
            }

            for (var i = 0; i < MAX_ITEMS; i++)
            {
                var item = GetRandomItem();
                if (!ItemsForSale.Any(i => i.Key == "Wireshark") && !CanWireshark)
                {
                    ItemsForSale.Add("Wireshark", 650);
                    continue;
                }
                ItemsForSale.Add(item.Key.DisplayName, item.Value);
            }

            KeyValuePair<HollowProgram, int> GetRandomItem()
            {
                var item = ProgramsForSale.GetRandom();
                if (ItemsForSale.ContainsKey(item.Key.DisplayName))
                {
                    return GetRandomItem();
                }
                return item;
            }

            Random random = new Random();
            int chance = random.Next(0, 100);
            if (chance > (100 * (CurrentLayer % 5)) - 15 && ProgramBundles.Any())
            {
                HollowBundle bundle;
                if (NeedsDecListed)
                {
                    bundle = ProgramBundles.First(p => p.ID == "DEC Suite");
                } else if(NeedsMemListed)
                {
                    bundle = ProgramBundles.First(p => p.ID == "Memory Master");
                } else
                {
                    bundle = ProgramBundles.GetRandom();
                }
                CurrentBundleForSale = bundle;
            }

            FileEntry itemsForSaleFile = new FileEntry();
            StringBuilder itemsForSaleFileContent = new StringBuilder();
            int itemIndex = 0;
            foreach (var item in ItemsForSale)
            {
                itemsForSaleFileContent.Append($"{item.Key},{item.Value}");
                if (itemIndex == ItemsForSale.Count - 1) continue;
                itemsForSaleFileContent.Append("|");
                itemIndex++;
            }

            itemsForSaleFile.name = "ShopItems";
            itemsForSaleFile.data = itemsForSaleFileContent.ToString();

            comp.getFolderFromPath("sys").files.Add(itemsForSaleFile);

            if (CurrentBundleForSale == default) return;

            FileEntry bundleForSaleFile = new FileEntry(CurrentBundleForSale.ID, "BundleForSale");
            comp.getFolderFromPath("sys").files.Add(bundleForSaleFile);
        }

        public void CreateSaleFilesIfMissing()
        {
            var sysFolder = comp.getFolderFromPath("sys");
            StringBuilder itemsForSaleFileContent = new StringBuilder();
            int itemIndex = 0;
            foreach (var item in ItemsForSale)
            {
                itemsForSaleFileContent.Append($"{item.Key},{item.Value}");
                if (itemIndex == ItemsForSale.Count - 1) continue;
                itemsForSaleFileContent.Append("|");
                itemIndex++;
            }
            if (!sysFolder.containsFile("ShopItems", itemsForSaleFileContent.ToString()))
            {
                if (sysFolder.containsFile("ShopItems"))
                {
                    sysFolder.files.Remove(sysFolder.searchForFile("ShopItems"));
                }
                sysFolder.files.Add(new FileEntry(itemsForSaleFileContent.ToString(), "ShopItems"));
            }
            if (CurrentBundleForSale == default)
            {
                if (sysFolder.containsFile("BundleForSale"))
                {
                    sysFolder.files.Remove(sysFolder.searchForFile("BundleForSale"));
                }
                return;
            }
            if (!sysFolder.containsFile("BundleForSale", CurrentBundleForSale.ID))
            {
                if (sysFolder.containsFile("BundleForSale"))
                {
                    sysFolder.files.Remove(sysFolder.searchForFile("BundleForSale"));
                }
                sysFolder.files.Add(new FileEntry(CurrentBundleForSale.ID, "BundleForSale"));
            }
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);
            PatternDrawer.draw(bounds, 0.5f, Color.Black * 0.5f, OS.currentInstance.highlightColor * 0.12f, GuiData.spriteBatch,
                PatternDrawer.thinStripe);

            switch (CurrentScreen)
            {
                case StoreScreen.Main:
                default:
                    DrawMainScreen(bounds);
                    break;
                case StoreScreen.ProgShop:
                case StoreScreen.Shop:
                    DrawShopListing(bounds);
                    break;
            }
        }

        private const int UI_BUTTON_HEIGHT = 50;
        private const int SPECIAL_BUTTON_HEIGHT = 60;
        private const int ITEM_BUTTON_HEIGHT = 40;
        private const int EXIT_BUTTON_HEIGHT = 20;

        private int BundlePrice => GetFinalPrice(CurrentBundleForSale.Price);

        private void DrawMainScreen(Rectangle bounds)
        {
            var textHeight = GetStringHeight(GuiData.titlefont, "PROGRAM SHOP");
            DrawCenteredText(bounds, "PROGRAM SHOP", GuiData.titlefont,
                textHeight + 15, Color.White);

            int yOffset = 0;
            if (CurrentBundleForSale != default)
            {
                string programsInBundle = "Includes: ";
                for (var i = 0; i < CurrentBundleForSale.Programs.Count; i++)
                {
                    var program = CurrentBundleForSale.Programs[i];
                    programsInBundle += program.DisplayName;
                    if (i == CurrentBundleForSale.Programs.Count - 1) continue;
                    programsInBundle += ", ";
                }

                HollowButton BundleButton = new HollowButton(ButtonIDs[0],
                bounds.X + (bounds.Width / 4), (bounds.Center.Y - (SPECIAL_BUTTON_HEIGHT / 2)) + yOffset, bounds.Width / 2,
                SPECIAL_BUTTON_HEIGHT,
                $"{CurrentBundleForSale.ID} Bundle (FEATURED) - ${BundlePrice}\n{programsInBundle}",
                OS.currentInstance.brightUnlockedColor);
                if (BundlePrice > PlayerCredits)
                {
                    BundleButton.Disabled = true;
                    BundleButton.DisabledMessage = "<!> You don't have enough credits for this bundle!";
                    if(!hasBeenRobbed)
                    {
                        BundleButton.DisabledMessage += "\n<?> If you really need it, have you considered... stealing it?";
                    }
                    BundleButton.Color = Utils.SlightlyDarkGray;
                }

                BundleButton.OnPressed = delegate ()
                {
                    PurchaseBundle();
                };
                BundleButton.DoButton();

                yOffset += SPECIAL_BUTTON_HEIGHT + 10;
            }

            HollowButton EnterShopButton = new HollowButton(ButtonIDs[1],
                bounds.X + (bounds.Width / 4), (bounds.Center.Y - (UI_BUTTON_HEIGHT / 2)) + yOffset, bounds.Width / 2,
                UI_BUTTON_HEIGHT, "Enter Shop ->",
                Color.Lerp(Color.Green, Color.CornflowerBlue, 0.5f));
            EnterShopButton.OnPressed = delegate ()
            {
                CurrentScreen = StoreScreen.ProgShop;
            };
            EnterShopButton.DoButton();
        }

        private void DrawShopListing(Rectangle bounds)
        {
            int yOffset = EXIT_BUTTON_HEIGHT + 10;

            GuiData.spriteBatch.DrawString(GuiData.font, "Shop Listing",
                new Vector2(bounds.X + 10, bounds.Y + 10), Color.White, 0f, Vector2.Zero,
                1.7f, SpriteEffects.None, 0.01f);
            float textOffset = GetStringHeight(GuiData.font, "Shop Listing") * 1.7f;

            var description = Utils.SuperSmartTwimForWidth("To make a purchase, " +
                "click on the item you'd like to purchase. No refunds!", bounds.Width - 20,
                GuiData.font);
            TextItem.doLabel(new Vector2(bounds.X + 10, bounds.Y + textOffset + 10),
                description, Color.White);

            HollowButton ExitShopButton = new HollowButton(ButtonIDs[2], bounds.X + 10,
                (bounds.Y + bounds.Height) - yOffset, bounds.Width / 3, EXIT_BUTTON_HEIGHT,
                "Go back to main menu...", Color.Red);
            ExitShopButton.OnPressed = delegate () { CurrentScreen = StoreScreen.Main; };
            ExitShopButton.DoButton();
            yOffset += EXIT_BUTTON_HEIGHT + ITEM_BUTTON_HEIGHT + 10;

            if (!ItemsForSale.Any())
            {
                TextItem.doSmallLabel(new Vector2(bounds.X + 10,
                    (bounds.Y + bounds.Height) - yOffset),
                    "We're outta items... thank you for being a loyal patron!",
                    Color.White);
                return;
            }

            for (var i = 0; i < ItemsForSale.Count; i++)
            {
                var key = ItemsForSale.ElementAt(i).Key;
                var cost = GetFinalPrice(ItemsForSale[key]);
                var program = ProgramsForSale.First(p => p.Key.DisplayName == key).Key;
                HollowButton ItemButton = new(ButtonIDs[7 + i], bounds.X + 10,
                    (bounds.Y + bounds.Height) - yOffset, bounds.Width / 3, ITEM_BUTTON_HEIGHT,
                    $"{program.DisplayName} - ${cost}", OS.currentInstance.highlightColor);
                ItemButton.OnPressed = delegate ()
                {
                    PurchaseProgram(program.DisplayName, cost);
                };
                if (cost > PlayerCredits)
                {
                    ItemButton.Disabled = true;
                    ItemButton.DisabledMessage = "<!> You don't have enough credits for that!";
                    ItemButton.Color = Utils.SlightlyDarkGray;
                }
                ItemButton.DoButton();
                yOffset += ITEM_BUTTON_HEIGHT + 10;
            }
        }

        private void PurchaseProgram(string programName, int cost)
        {
            var program = GetProgramByName(programName);
            if (program == null)
            {
                OS.currentInstance.write("Whoops -- there was an error making your purchase.");
                OS.currentInstance.write("Reason: Item Not Found");
                OS.currentInstance.write("Please report this to the mod developer.");
                return;
            }
            if (!AttemptPurchaseItem(cost)) return;
            ItemsForSale.Remove(programName);

            OS.currentInstance.warningFlash();
            OS.currentInstance.write("--- PURCHASE CONFIRMED ---");
            OS.currentInstance.write($"Thank you for your purchase of {programName}!");
            OS.currentInstance.write($"New Credits Balance: ${PlayerCredits}");

            AddProgramToPlayerPC(programName, program.FileContent);
        }

        private void PurchaseBundle()
        {
            var bundle = CurrentBundleForSale;
            if (bundle == null)
            {
                OS.currentInstance.write("Whoops -- there was an error making your purchase.");
                OS.currentInstance.write("Reason: Item Not Found");
                OS.currentInstance.write("Please report this to the mod developer.");
                return;
            }
            if (!AttemptPurchaseItem(BundlePrice)) return;
            CurrentBundleForSale = null;

            foreach (var prog in bundle.Programs)
            {
                AddProgramToPlayerPC(prog.DisplayName, prog.FileContent);
            }

            OS.currentInstance.warningFlash();
            OS.currentInstance.write("--- PURCHASE CONFIRMED ---");
            OS.currentInstance.write($"Thank you for your purchase of the {bundle.ID} Bundle!");
            OS.currentInstance.write($"New Credits Balance: ${PlayerCredits}");
        }

        private bool CheckForItemFile()
        {
            if (!comp.getFolderFromPath("sys").containsFile("ShopItems")) return false;

            string shopItems = comp.getFolderFromPath("sys").searchForFile("ShopItems").data;
            var itemsWithPrices = shopItems.Split('|');

            foreach (var item in itemsWithPrices)
            {
                string itemName = item.Split(',')[0];
                int itemPrice = int.Parse(item.Split(',')[1]);

                ItemsForSale.Add(itemName, itemPrice);
            }

            if (!comp.getFolderFromPath("sys").containsFile("BundleForSale")) return true;

            string bundleName = comp.getFolderFromPath("sys").searchForFile("BundleForSale").data;
            var bundle = ProgramBundles.FirstOrDefault(b => b.ID == bundleName);
            if (bundle == default) return true;

            CurrentBundleForSale = bundle;

            return true;
        }

        private HollowProgram GetProgramByName(string name)
        {
            return ProgramsForSale.Keys.FirstOrDefault(ByName(name));
        }

        internal override void OnDisconnect()
        {
            foreach (var id in ButtonIDs)
            {
                PFButton.ReturnID(id);
            }
        }

        private List<HollowProgram> GetUserPrograms()
        {
            Computer playerComp = OS.currentInstance.thisComputer;
            var binFolder = playerComp.getFolderFromPath("bin");
            List<HollowProgram> userPrograms = new List<HollowProgram>();

            foreach (var file in binFolder.files)
            {
                var programName = PortExploits.GetExeNameForData(file.name, file.data);
                if (programName == null) continue;

                HollowProgram program = new HollowProgram(programName.Split('.')[0], programName);
                userPrograms.Add(program);
            }

            return userPrograms;
        }
    }

    internal class HollowBundle
    {
        public HollowBundle(string id, int price, List<HollowProgram> programs)
        {
            ID = id;
            Price = price;
            Programs = programs;
        }

        public string ID;
        public int Price;
        public List<HollowProgram> Programs;
    }
}
