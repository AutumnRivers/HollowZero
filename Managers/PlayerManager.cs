using System;
using System.Linq;

using Hacknet;
using Hacknet.Gui;

using HollowZero.Daemons;
using HollowZero.Daemons.Shop;
using HollowZero.Nodes;

using Microsoft.Xna.Framework;

using static HollowZero.Nodes.LayerSystem.LayerGenerator;
using static HollowZero.HollowLogger;
using HollowZero.Daemons.Event;

namespace HollowZero.Managers
{
    public static class PlayerManager
    {
        private static bool hasDecypher = false;
        private static bool hasDecHead = false;

        private static bool hasMemGen = false;
        private static bool hasMemForen = false;

        public static bool Transitioning { get; private set; } = false;

        public static int InfectionLevel { get; internal set; } = 0;
        public static uint PlayerCredits { get; internal set; } = 0;
        public static int CurrentLayer { get; internal set; } = 1;

        private static readonly string[] decData = new string[]
        {
            PortExploits.crackExeData[9],
            PortExploits.crackExeData[10]
        };

        private static readonly string[] memData = new string[]
        {
            PortExploits.crackExeData[34],
            PortExploits.crackExeData[33]
        };

        public static void AddProgramToPlayerPC(string programName, string programContent)
        {
            FileEntry programFile = new(programContent, $"{programName}.exe");
            Folder binFolder = OS.currentInstance.thisComputer.getFolderFromPath("bin");

            if (decData[0] == programContent)
            {
                hasDecypher = true;
                if (hasDecHead) {
                    CanDecrypt = true;
                    NeedsDecListed = false;
                }
            }
            else if (decData[1] == programContent)
            {
                hasDecHead = true;
                if (hasDecypher) {
                    CanDecrypt = true;
                    NeedsDecListed = false;
                }
            }

            if (memData[0] == programContent)
            {
                hasMemGen = true;
                if (hasMemForen) {
                    CanMemDump = true;
                    NeedsMemListed = false;
                }
            }
            else if (memData[1] == programContent)
            {
                hasMemForen = true;
                if (hasMemGen) {
                    CanMemDump = true;
                    NeedsMemListed = false;
                }
            }

            if (programContent == ComputerLoader.filter("#WIRESHARK_EXE#"))
            {
                CanWireshark = true;
            }

            if (binFolder.containsFileWithData(programContent)) return;

            binFolder.files.Add(programFile);
        }

        public static void IncreaseInfection(int amount, bool overflow = false)
        {
            foreach (var mod in HollowZeroCore.CollectedMods.Where(m => m.Trigger == Modification.ModTriggers.OnInfectionGain))
            {
                if (mod.IsBlocker && mod.ChanceEffect != null)
                {
                    if (mod.ChanceEffect(OS.currentInstance.thisComputer)) return;
                }
                else if (mod.IsBlocker)
                {
                    mod.LaunchEffect(OS.currentInstance.thisComputer, amount);
                    return;
                }
                else
                {
                    mod.LaunchEffect(OS.currentInstance.thisComputer, amount);
                }
            }

            if (InfectionLevel + amount >= 100)
            {
                HollowZeroCore.Overload(InfectionLevel + amount, overflow);
            }
            else
            {
                InfectionLevel += amount;
            }
        }

        public static void DecreaseInfection(int amount)
        {
            if (InfectionLevel - amount <= 0)
            {
                InfectionLevel = 0;
            }
            else
            {
                InfectionLevel -= amount;
            }
        }

        public static void ClearInfection()
        {
            InfectionLevel = 0;
        }

        public static void AddPlayerCredits(int amount)
        {
            if (PlayerCredits + amount > 9999)
            {
                PlayerCredits = 9999;
            }
            else
            {
                PlayerCredits += (uint)amount;
            }
        }

        public static bool RemovePlayerCredits(int amount)
        {
            if (PlayerCredits - amount < 0)
            {
                return false;
            }
            else
            {
                PlayerCredits -= (uint)amount;
                return true;
            }
        }

        internal static float layerTransitionProgress = 0.0f;
        private static bool nodesPrepared = false;
        private static bool warned = false;
        private static bool alerted = false;

        public const float TRANSITION_SPIN_UP_TIME = 5.0f;

        private static string layerSuffix
        {
            get
            {
                int layer = CurrentLayer;
                string suffix = "th";
                if (layer.ToString().EndsWith("1"))
                {
                    suffix = "st";
                }
                else if (layer.ToString().EndsWith("2"))
                {
                    suffix = "nd";
                }
                else if (layer.ToString().EndsWith("3"))
                {
                    suffix = "rd";
                }
                return suffix;
            }
        }

        public static void MoveToNextLayer()
        {
            CurrentLayer += 1;
            Action changeStage = delegate ()
            {
                CustomEffects.CurrentStage++;
            };
            UnavoidableEventDaemon.LockUpModules();
            OS.currentInstance.display.inputLocked = true;
            Transitioning = true;
            Action layerTransitionFX = delegate ()
            {
                int stage = CustomEffects.CurrentStage;
                OS os = OS.currentInstance;
                Rectangle screen = Utils.GetFullscreen();
                if (layerTransitionProgress < 1.0f)
                {
                    addRadialLine();
                    if (layerTransitionProgress > 0.5f)
                    {
                        addRadialLine();
                    }
                    if (layerTransitionProgress > 0.75f)
                    {
                        addRadialLine();
                    }
                    Utils.FillEverywhereExcept(os.netMap.bounds, screen, GuiData.spriteBatch, Color.Black * 0.5f);
                    PostProcessor.EndingSequenceFlashOutActive = true;
                    PostProcessor.EndingSequenceFlashOutPercentageComplete = 1f - layerTransitionProgress;
                }
                else
                {
                    PostProcessor.EndingSequenceFlashOutActive = false;
                    PostProcessor.EndingSequenceFlashOutPercentageComplete = 0f;
                    switch (stage)
                    {
                        case 0:
                            RenderedRectangle.doRectangle(screen.X, screen.Y, screen.Width, screen.Height, Color.White);
                            if (!warned)
                            {
                                warned = true;
                                os.IncConnectionOverlay.sound1.Play();
                            }
                            HollowTimer.AddTimer("layer_transition_stage1", 2.0f, changeStage, false);
                            break;
                        case 1:
                            RenderedRectangle.doRectangle(screen.X, screen.Y, screen.Width, screen.Height, Color.Black);
                            HollowDaemon.DrawTrueCenteredText(screen, "-= CONNECTING =-", GuiData.titlefont, Color.White);
                            HollowTimer.AddTimer("layer_transition_stage2", 3.0f, changeStage, false);
                            if (!nodesPrepared)
                            {
                                nodesPrepared = true;
                                NodeManager.ClearNetMap();
                                Programs.disconnect(new string[] { }, os);
                                OS.currentInstance.display.command = "dc";
                                // Add starting node here...
                            }
                            break;
                        case 2:
                            if (!alerted)
                            {
                                alerted = true;
                                os.hubServerAlertsIcon.alertSound.Play();
                            }
                            CustomEffects.ResetEffect();
                            os.delayer.Post(ActionDelayer.NextTick(), delegate ()
                            {
                                layerTransitionProgress = 0.0f;
                                alerted = false;
                                warned = false;
                                nodesPrepared = false;
                                sendTransitionMessages(
                                    "-----------------------------------------",
                                    $"You've reached the {CurrentLayer}{layerSuffix} layer",
                                    "-----------------------------------------"
                                    );
                                UnavoidableEventDaemon.UnlockModules();
                                OS.currentInstance.display.inputLocked = false;
                                Transitioning = false;
                                GameplayManager.GenerateAndLoadInLayer();
                            });
                            break;
                    }
                }
                var gameTime = OS.currentInstance.lastGameTime.ElapsedGameTime.TotalSeconds;
                layerTransitionProgress += (float)gameTime / TRANSITION_SPIN_UP_TIME;
            };
            CustomEffects.ChangeEffect(layerTransitionFX, true);
            if (CurrentLayer % 5 == 0)
            {
                if (!CanDecrypt)
                {
                    CanDecrypt = true;
                }
                else if (!CanMemDump)
                {
                    CanMemDump = true;
                }
                else if (!CanWireshark)
                {
                    CanWireshark = true;
                }
            }

            static void addRadialLine()
            {
                Vector2 playerPos = OS.currentInstance.thisComputer.location;
                Vector2 netMapPos = new(OS.currentInstance.netMap.bounds.X, OS.currentInstance.netMap.bounds.Y);
                Vector2 netMapDim = new(OS.currentInstance.netMap.bounds.Width, OS.currentInstance.netMap.bounds.Height);
                Vector2 radPosition = new()
                {
                    X = netMapPos.X + (netMapDim.X * playerPos.X),
                    Y = netMapPos.Y + (netMapDim.Y * playerPos.Y)
                };
                LogDebug(radPosition.ToString());
                SFX.AddRadialLine(radPosition,
                        Utils.randm(180f), 600f + Utils.randm(300f), 800f, 500f, 200f + Utils.randm(400f),
                        0.35f, Color.Lerp(Utils.makeColor(100, 0, 0, byte.MaxValue), Utils.AddativeRed, Utils.randm(1f)),
                        2f);
            }

            static void sendTransitionMessages(params string[] messages)
            {
                float delay = 0.1f;
                OS.currentInstance.terminal.reset();
                foreach (var msg in messages)
                {
                    OS.currentInstance.delayer.Post(ActionDelayer.Wait(delay), delegate ()
                    {
                        OS.currentInstance.write(msg);
                    });
                    delay += 0.1f;
                }
            }
        }
    }
}
