using Hacknet;
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

        public override string Identifier => "Gacha Shop";

        public const int DEFAULT_MOD_CHANCES = 3;
        public const int DEFAULT_UPGRADE_CHANCES = 2;

        public int RemainingModifications = DEFAULT_MOD_CHANCES;
        public int RemainingUpgrades = DEFAULT_UPGRADE_CHANCES;

        public int ModButtonID;
        public int UpgradeButtonID;

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

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            PatternDrawer.draw(bounds, 0.5f, Color.Black * 0.5f, OS.currentInstance.brightUnlockedColor * 0.12f, GuiData.spriteBatch,
                PatternDrawer.wipTile);

            int titleHeight = GetStringHeight(GuiData.titlefont, "GREAT GACHA!");

            DrawCenteredText(bounds, "GREAT GACHA!", GuiData.titlefont, titleHeight + 10);

            const int BUTTON_HEIGHT = 50;

            var modButton = new HollowButton(ModButtonID, bounds.Center.X - (bounds.Width / 4),
                bounds.Center.Y - (BUTTON_HEIGHT / 2), bounds.Width / 2, BUTTON_HEIGHT,
                "Get Modification", Color.White);
            if(RemainingModifications > 0)
            {
                modButton.Text = $"Get Random Modification!\n(Remaining Chances: {RemainingModifications}";
                modButton.Color = OS.currentInstance.brightUnlockedColor;
                modButton.OnPressed = delegate ()
                {
                    switch(GetModification(out var mod, out var cor))
                    {
                        case true:
                            HollowZeroCore.CollectedMods.Add(mod);
                            break;
                        case false:
                            HollowZeroCore.CollectedCorruptions.Add(cor);
                            break;
                    }
                };
            }
        }

        private const int CHANCE = 80;

        private bool GetModification(out Modification mod, out Corruption corruption)
        {
            mod = null;
            corruption = null;

            if(GetChanceResult(CHANCE))
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
                if (HollowZeroCore.CollectedMods.Contains(mod)) return GetMod();
                return mod;
            }

            Corruption GetCorruption()
            {
                var c = DefaultCorruptions.Corruptions.GetRandom();
                if (HollowZeroCore.CollectedCorruptions.Contains(c)) return GetCorruption();
                return c;
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
