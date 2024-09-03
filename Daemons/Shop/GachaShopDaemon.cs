using Hacknet;
using HollowZero.Managers;
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
    public class GachaShopDaemon : ShopDaemon
    {
        public GachaShopDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public static new bool Registerable => true;

        public override string Identifier => "Gacha Shop";

        public const int DEFAULT_MOD_CHANCES = 3;
        public const int DEFAULT_UPGRADE_CHANCES = 1;

        public int RemainingModifications = DEFAULT_MOD_CHANCES;
        public int RemainingUpgrades = DEFAULT_UPGRADE_CHANCES;

        public int ModButtonID;
        public int UpgradeButtonID;

        public int Cost = 500;

        public override void initFiles()
        {
            base.initFiles();

            if (CheckForChanceFiles()) return;
            Folder sysFolder = comp.getFolderFromPath("sys");

            // This should only fire if the file was found but the values were invalid.
            if (sysFolder.TryFindFile("GachaValues", out var file))
            {
                sysFolder.files.Remove(file);
            }

            FileEntry chanceFile = new FileEntry
            {
                name = "GachaValues",
                data = $"{RemainingModifications},{RemainingUpgrades}"
            };
            sysFolder.files.Add(chanceFile);

            GetButtonIDs();
        }

        public override void navigatedTo()
        {
            GetButtonIDs();
            base.navigatedTo();
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            PatternDrawer.draw(bounds, 0.5f, Color.Black * 0.5f, OS.currentInstance.unlockedColor * 0.12f, GuiData.spriteBatch,
                PatternDrawer.wipTile);

            int titleHeight = GetStringHeight(GuiData.titlefont, "GREAT GACHA!");

            DrawCenteredText(bounds, "GREAT GACHA!", GuiData.titlefont, titleHeight + 10);

            const int BUTTON_HEIGHT = 50;

            int modPrice = (int)Math.Ceiling((Cost * ((DEFAULT_MOD_CHANCES - RemainingModifications) + 1)) * PriceMultiplier);

            var modButton = new HollowButton(ModButtonID, bounds.Center.X - (bounds.Width / 4),
                bounds.Center.Y - (BUTTON_HEIGHT / 2), bounds.Width / 2, BUTTON_HEIGHT,
                "Get Modification", Color.White);
            if(RemainingModifications > 0)
            {
                modButton.Text = $"Get Random Modification! (${modPrice})\n(Remaining Chances: {RemainingModifications}";
                modButton.Color = OS.currentInstance.brightUnlockedColor;
                if(PlayerManager.PlayerCredits < modPrice)
                {
                    modButton.Disabled = true;
                    modButton.DisabledMessage = "<!> You don't have enough credits for that!";
                }
                modButton.OnPressed = delegate ()
                {
                    switch(GetModification(out var mod, out var cor))
                    {
                        case true:
                            InventoryManager.AddModification(mod);
                            OS.currentInstance.terminal.writeLine("< :) > GREAT LUCK! " +
                                $"You got the Modifcation: {mod.DisplayName}");
                            if(OS.DEBUG_COMMANDS)
                            {
                                OS.currentInstance.terminal.writeLine("MOD DEBUG: " +
                                    $"{mod.ID} | U:{mod.Upgraded} | {mod.Description}");
                            }
                            Chance -= 15;
                            break;
                        case false:
                            InventoryManager.AddCorruption(cor);
                            OS.currentInstance.terminal.writeLine("< :( > BAD LUCK! " +
                                $"You got the Corruption: {cor.DisplayName}");
                            if (OS.DEBUG_COMMANDS)
                            {
                                OS.currentInstance.terminal.writeLine("CORRUPTION DEBUG: " +
                                    $"{cor.ID} | U:{cor.Upgraded} | Steps:{cor.StepsLeft} | {cor.Description}");
                            }
                            break;
                    }
                    Cost += (int)Math.Floor(Cost / 4f);
                    RemainingModifications--;
                };
            } else
            {
                modButton.Text = "(OUT OF MODIFICATIONS)";
                modButton.Disabled = true;
                modButton.DisabledMessage = "<...> Out of modifications!";
            }
            modButton.DoButton();

            int upgradeCost = (int)Math.Ceiling((Cost * 1.5f) * ((DEFAULT_UPGRADE_CHANCES - RemainingUpgrades) + 1) * PriceMultiplier);

            var upgradeButton = new HollowButton(UpgradeButtonID, bounds.Center.X - (bounds.Width / 4),
                bounds.Center.Y + (BUTTON_HEIGHT / 2) + 25, bounds.Width / 2, BUTTON_HEIGHT,
                $"Get Mod. Upgrade (${upgradeCost})", Color.White);
            if(RemainingUpgrades > 0)
            {
                if (PlayerManager.PlayerCredits < upgradeCost)
                {
                    upgradeButton.Disabled = true;
                    upgradeButton.DisabledMessage = "<!> You don't have enough credits for that!";
                }
                if (HollowZeroCore.CollectedMods.Count(m => !m.Upgraded) <= 0)
                {
                    upgradeButton.Disabled = true;
                    upgradeButton.DisabledMessage = "<...> There's no more mods for you to upgrade!";
                }

                upgradeButton.Text = $"Upgrade Random Modification!\n(Remaining Chances: {RemainingUpgrades})";
                upgradeButton.Color = OS.currentInstance.unlockedColor;
                upgradeButton.OnPressed = delegate ()
                {
                    UpgradeModification();
                    RemainingUpgrades--;
                };
            } else
            {
                upgradeButton.Text = "(OUT OF UPGRADES)";
                upgradeButton.Disabled = true;
                upgradeButton.DisabledMessage = "<...> Out of upgrades!";
            }
            upgradeButton.DoButton();
        }

        private const int CHANCE = 80;
        private int Chance = CHANCE;

        private bool GetModification(out Modification mod, out Corruption corruption)
        {
            mod = null;
            corruption = null;

            if(GetChanceResult(Chance))
            {
                mod = GetMod();
                return true;
            } else
            {
                corruption = GetCorruption();
                return false;
            }

            Modification GetMod()
            {
                var mod = DefaultModifications.Mods.GetRandom();
                if (HollowZeroCore.CollectedMods.Any(m => m.ID == mod.ID)) return GetMod();
                return mod;
            }

            Corruption GetCorruption()
            {
                var c = DefaultCorruptions.Corruptions.GetRandom();
                if (HollowZeroCore.CollectedCorruptions.Any(cor => cor.ID == c.ID)) return GetCorruption();
                return c;
            }
        }

        private void UpgradeModification()
        {
            if(GetChanceResult(CHANCE))
            {
                var m = HollowZeroCore.CollectedMods.Where(mod => !mod.Upgraded).GetRandom();
                if(OS.DEBUG_COMMANDS) {
                    OS.currentInstance.terminal.writeLine($"Upgrading {m.DisplayName} | {m.Description}");
                }
                InventoryManager.UpgradeModification(m);
                if (OS.DEBUG_COMMANDS)
                {
                    OS.currentInstance.terminal.writeLine($"Upgraded {m.DisplayName} | {m.Description}");
                }
            } else if(HollowZeroCore.CollectedCorruptions.Any(c => !c.Upgraded))
            {
                var c = HollowZeroCore.CollectedCorruptions.Where(c => !c.Upgraded).GetRandom();
                InventoryManager.UpgradeCorruption(c);
            }
        }

        private void GetButtonIDs()
        {
            ModButtonID = PFButton.GetNextID();
            UpgradeButtonID = PFButton.GetNextID();
        }

        internal override void OnDisconnect()
        {
            PFButton.ReturnID(ModButtonID);
            PFButton.ReturnID(UpgradeButtonID);

            ModButtonID = default;
            UpgradeButtonID = default;
        }

        public void RecreateChanceFilesIfMissing()
        {
            Folder sysFolder = comp.getFolderFromPath("sys");

            if(sysFolder.TryFindFile("GachaValues", out var gachaFile))
            {
                var values = gachaFile.data.Split(',');
                if (values.Length != 2)
                {
                    RecreateFiles(gachaFile);
                    return;
                }

                if (int.TryParse(values[0], out int mods) && int.TryParse(values[1], out int upgrades))
                {
                    if (mods != RemainingModifications || upgrades != RemainingUpgrades)
                    {
                        RecreateFiles();
                        return;
                    }
                }
            } else
            {
                RecreateFiles();
                return;
            }

            void RecreateFiles(FileEntry gachaFile = null)
            {
                FileEntry chanceFile = new FileEntry($"{RemainingModifications},{RemainingUpgrades}", "GachaValues");
                if (gachaFile != null) sysFolder.files.Remove(gachaFile);
                sysFolder.files.Add(chanceFile);
            };
        }

        public bool CheckForChanceFiles()
        {
            Folder sysFolder = comp.getFolderFromPath("sys");
            
            if(sysFolder.TryFindFile("GachaValues", out var gachaFile))
            {
                var values = gachaFile.data.Split(',');
                if (values.Length < 2) return false;

                if (int.TryParse(values[0], out int mods) && int.TryParse(values[1], out int upgrades))
                {
                    RemainingModifications = mods;
                    RemainingUpgrades = upgrades;
                    GetButtonIDs();
                }
            }

            return false;
        }

        public bool GetChanceResult(int target)
        {
            Random random = new Random();
            int chance = random.Next(0, 100);

            return chance <= target;
        }
    }
}
