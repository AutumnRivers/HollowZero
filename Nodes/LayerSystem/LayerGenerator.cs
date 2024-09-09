using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using static HollowZero.Nodes.LayerSystem.LayerSolver;
using static HollowZero.HollowLogger;

using static HollowZero.Nodes.LayerSystem.LayerSolutionStep.SolutionType;

using BepInEx;

using Stuxnet_HN;
using Stuxnet_HN.Patches;
using Stuxnet_HN.Executables;
using HollowZero.Daemons.Shop;
using HollowZero.Managers;

namespace HollowZero.Nodes.LayerSystem
{
    public static class LayerGenerator
    {
        public const int MIN_LAYER_SIZE = 5;
        public const int MAX_LAYER_SIZE = 10;

        private static Random random = new();
        public static int LayerSize = MAX_LAYER_SIZE;

        internal static void RegenSeed()
        {
            random = new Random();
        }

        // Generate completely randomized layer - very likely unsolvable
        public static HollowLayer GenerateTrueRandomLayer()
        {
            HollowLayer layer = new();
            int layerSize = LayerSize;

            for(var i = 0; i < layerSize + 1; i++)
            {
                var genComp = NodeGenerator.GenerateComputer($"TestComp{i + 1}");
                if(Utils.flipCoin())
                {
                    genComp = NodeGenerator.GenerateEventComputer($"TestComp{i + 1}");
                }
                if(Utils.flipCoin() && layer.nodes.Count > 2)
                {
                    genComp.links.Add(random.Next(0, layer.nodes.Count));
                }
                layer.nodes.Add(genComp);
            }

            return layer;
        }

        // Generate (theoretically) solvable layer
        private static int retries = 0;
        private const int MAX_RETRIES = 5;
        private static readonly Array solutionTypesArr = Enum.GetValues(typeof(LayerSolutionStep.SolutionType));
        private static readonly List<LayerSolutionStep.SolutionType> solutionTypes =
            solutionTypesArr.Cast<LayerSolutionStep.SolutionType>().ToList();

        public static bool CanDecrypt { get; set; } = false;
        public static bool CanMemDump { get; set; } = false;
        public static bool CanWireshark { get; set; } = false;
        public static bool CanScanEOS { get; set; } = false;

        public static bool NeedsDecListed { get; set; } = true;
        public static bool NeedsMemListed { get; set; } = true;

        private static List<Computer> GenerateSolutionComp(Computer comp, Computer prevComp, ref List<Computer> layerNodes,
            ref bool needsLinking)
        {
            var solutionType = solutionTypes.GetRandom();
            needsLinking = false;

            List<Computer> comps = new()
            {
                comp, prevComp
            };
            prevComp.Memory = new();

            LogDebug($"Generating {solutionType} solution for {prevComp.name} (ID: {prevComp.idName})", true);
            switch(solutionType)
            {
                case GainAdminAccess:
                default:
                    needsLinking = true;
                    break;
                case DecryptFile:
                    if(!CanDecrypt)
                    {
                        needsLinking = true;
                        break;
                    }
                    var encFile = GenerateEncryptedFile(prevComp, comp, out string pass);
                    if(pass != "")
                    {
                        int chance = random.Next(1, 5);
                        if(!CanMemDump)
                        {
                            prevComp.getFolderFromPath("home").files.Add(new FileEntry(pass, "enc_pass.txt"));
                            break;
                        }
                        switch(chance)
                        {
                            case 1:
                                prevComp.getFolderFromPath("home").files.Add(new FileEntry(pass, "enc_pass.txt"));
                                break;
                            case 2:
                                prevComp.Memory.CommandsRun.Add(pass);
                                break;
                            case 3:
                                prevComp.Memory.DataBlocks.Add(pass);
                                break;
                            case 4:
                                var eos = GenerateEOSDevice("Test eOS", pass, prevComp);
                                layerNodes.Add(eos);
                                int index = layerNodes.IndexOf(eos);
                                prevComp.links.Add(index);
                                if(!prevComp.attatchedDeviceIDs.IsNullOrWhiteSpace())
                                {
                                    prevComp.attatchedDeviceIDs += ",";
                                }
                                prevComp.attatchedDeviceIDs += eos.idName;
                                break;
                        }
                    }
                    prevComp.getFolderFromPath("home").files.Add(encFile);
                    break;
                case MemoryDump:
                    if(!CanMemDump)
                    {
                        needsLinking = true;
                        break;
                    }
                    prevComp.Memory.DataBlocks.Add("--) " + comp.ip);
                    break;
                case WiresharkCapture:
                    if(!CanWireshark)
                    {
                        needsLinking = true;
                        break;
                    }
                    WiresharkContents wsContent = new();
                    WiresharkEntry wsEntry = new(1, prevComp.ip, comp.ip, $"--- {comp.ip} ---", false);
                    wsContent.entries.Add(wsEntry);
                    StuxnetCore.wiresharkComps.Add(prevComp.idName, wsContent);
                    break;
            }

            comps[0] = comp;
            comps[1] = prevComp;

            return comps;
        }

        public static Computer GenerateEOSDevice(string title, string content, Computer linkedComp)
        {
            OS os = OS.currentInstance;
            string eosName = title;
            string eosID = $"eos_{linkedComp.idName}";

            Computer eosDevice = new(eosName, NetworkMap.generateRandomIP(), os.netMap.getRandomPosition(), 0, 5, os);
            eosDevice.idName = eosID;
            eosDevice.icon = Utils.flipCoin() ? "ePhone" : "ePhone2";
            eosDevice.location = linkedComp.location + Corporation.getNearbyNodeOffset(linkedComp.location, Utils.random.Next(12),
                12, os.netMap);
            eosDevice.setAdminPassword("alpine");
            ComputerLoader.loadPortsIntoComputer("22,3659", eosDevice);
            eosDevice.portsNeededForCrack = 2;
            EOSComp.GenerateEOSFilesystem(eosDevice);

            Folder rootFolder = eosDevice.files.root.searchForFolder("eos");
            Folder notesFolder = rootFolder.searchForFolder("notes");

            FileEntry passNote = new(content, "ImportantFile.txt");
            notesFolder.files.Add(passNote);

            Folder appsFolder = rootFolder.searchForFolder("apps");
            if(appsFolder != null)
            {
                appsFolder.files.Clear();
                appsFolder.folders.Clear();
            }
            return eosDevice;
        }

        internal static string DetermineEventFromComputer(Computer comp)
        {
            if(comp.daemons.OfType<ProgramShopDaemon>().Any())
            {
                return "progshop";
            } else if(comp.daemons.OfType<Daemons.RestStopDaemon>().Any())
            {
                return "reststop";
            } else if(comp.daemons.OfType<GachaShopDaemon>().Any())
            {
                return "gachashop";
            } else if(comp.daemons.OfType<AntivirusShopDaemon>().Any())
            {
                return "avshop";
            } else
            {
                return null;
            }
        }

        public static HollowLayer GenerateSolvableLayer()
        {
            HollowLayer layer = new();
            int layerSize = Utils.random.Next(MIN_LAYER_SIZE, MAX_LAYER_SIZE);

            StuxnetCore.wiresharkComps.Clear();
            Computer lastComp = null;

            List<string> excludedEvents = new();

            for (var i = 0; i < layerSize + 1; i++)
            {
                bool lastNode = i == layerSize;
                bool firstNode = lastComp == null;
                bool link = false;
                var genComp = NodeGenerator.GenerateComputer($"TestComp{i + 1}");
                if (Utils.flipCoin())
                {
                    genComp = NodeGenerator.GenerateEventComputer($"TestComp{i + 1}", excludedEvents.ToArray());
                }
                if(PlayerManager.CurrentLayer % 5 == 0 && firstNode)
                {
                    genComp = NodeGenerator.GenerateProgramShopComp();
                }
                List<Computer> solComps = new();
                if(i > 0)
                {
                    solComps = GenerateSolutionComp(genComp, lastComp, ref layer.nodes, ref link);
                    genComp = solComps[0];
                }
                if(lastNode)
                {
                    genComp = NodeGenerator.GenerateTransitionNode();
                }
                var ev = DetermineEventFromComputer(genComp);
                if(!ev.IsNullOrWhiteSpace())
                {
                    layer.Events.Add(ev);
                    findExcludedEvents();
                }
                layer.nodes.Add(genComp);
                int currentCompIndex = layer.nodes.IndexOf(genComp);
                string currentCompID = genComp.idName;
                int lastCompIndex = i > 0 ? layer.nodes.IndexOf(lastComp) : -1;
                if(link)
                {
                    solComps[1].links.Add(currentCompIndex);
                    if (!solComps[1].attatchedDeviceIDs.IsNullOrWhiteSpace())
                    {
                        solComps[1].attatchedDeviceIDs += ",";
                    }
                    solComps[1].attatchedDeviceIDs += currentCompID;
                }
                if (i > 0 && lastCompIndex > -1)
                {
                    layer.nodes[lastCompIndex] = solComps[1];
                }
                lastComp = genComp;
            }
            if(AttemptSolveLayer(layer, out var sol))
            {
                layer.Solvable = true;
                layer.Solution = sol;
                retries = 0;
                return layer;
            } else
            {
                if(retries++ > MAX_RETRIES)
                {
                    LogError($"[LayerGenerator] Couldn't generate a solvable layer after {MAX_RETRIES} retries.");
                    return layer;
                }
                LogWarning($"Failed to generate solvable layer on attempt {retries}");
                StuxnetCore.wiresharkComps.Clear();
                return GenerateSolvableLayer();
            }

            void findExcludedEvents()
            {
                if(layer.Events.Count(e => e == "progshop") >= 2 && !excludedEvents.Contains("progshop"))
                {
                    excludedEvents.Add("progshop");
                }

                if(layer.Events.Count(e => e == "reststop") >= 2 && !excludedEvents.Contains("reststop"))
                {
                    excludedEvents.Add("reststop");
                }

                if (layer.Events.Contains("avshop") && !excludedEvents.Contains("avshop")) excludedEvents.Add("avshop");
                if (layer.Events.Contains("gachashop") && !excludedEvents.Contains("gachashop")) excludedEvents.Add("gachashop");
            }
        }

        public static FileEntry GenerateEncryptedFile(Computer sourceComp, Computer targetComp, out string pass,
            bool forcePassword = false, string password = null)
        {
            string ip = targetComp.ip;
            pass = "";

            if(forcePassword && !password.IsNullOrWhiteSpace())
            {
                pass = password;
            }

            if(Utils.flipCoin())
            {
                pass = PortExploits.getRandomPassword();
            }

            string content = FileEncrypter.EncryptString($"--- {ip} ---", sourceComp.ip, sourceComp.ip, pass);
            return new FileEntry(content, "next.dec");
        }
    }

    public static class LayerSolver
    {
        public static bool AttemptSolveLayer(HollowLayer layer, out List<LayerSolutionStep> solution)
        {
            bool canSolve = true;
            solution = new List<LayerSolutionStep>();

            foreach(var node in layer.nodes)
            {
                int idx = layer.nodes.IndexOf(node);
                Computer nextNode = null;
                if (idx + 1 != layer.nodes.Count) nextNode = layer.nodes[idx + 1];

                if(nextNode == null)
                {
                    solution.Add(new(node, GainAdminAccess));
                    continue;
                }
                if(node.idName.StartsWith("eos"))
                {
                    continue;
                }

                if (nextNode.idName.StartsWith("eos"))
                {
                    nextNode = layer.nodes[idx + 2];
                }
                if (node.links.Contains(layer.nodes.IndexOf(nextNode)))
                {
                    solution.Add(new(node, GainAdminAccess, nextNode));
                    continue;
                } else if(findMemoryWithContent(node, nextNode.ip))
                {
                    solution.Add(new(node, MemoryDump, nextNode));
                    continue;
                } else if(StuxnetCore.wiresharkComps.ContainsKey(node.idName))
                {
                    solution.Add(new(node, WiresharkCapture, nextNode));
                    continue;
                } else if(findDecryptedFileWithContent(node, nextNode.ip))
                {
                    solution.Add(new(node, DecryptFile, nextNode));
                    continue;
                } else
                {
                    canSolve = false;
                }
            }

            return canSolve;

            bool tryDecryptFile(string fileContent, string pass)
            {
                int result = FileEncrypter.FileIsEncrypted(fileContent, pass);
                return result != 0 && result != 2;
            }

            bool findDecryptedFileWithContent(Computer comp, string content)
            {
                bool canFind = false;
                List<FileEntry> encryptedFiles = new();
                string decryptedFile;

                foreach(var folder in comp.files.root.folders)
                {
                    if(folder.files.Any(f => f.name.EndsWith(".dec")))
                    {
                        encryptedFiles.Add(folder.files.First(f => f.name.EndsWith(".dec")));
                    }
                }
                if (!encryptedFiles.Any()) return false;
                string possiblePass = "";
                string[] attachedIDs = new string[0];
                if(!comp.attatchedDeviceIDs.IsNullOrWhiteSpace())
                {
                    if(comp.attatchedDeviceIDs.Contains(","))
                    {
                        attachedIDs = comp.attatchedDeviceIDs.Split(',');
                    } else
                    {
                        attachedIDs = new string[1] { comp.attatchedDeviceIDs };
                    }
                }
                foreach(var encFile in encryptedFiles)
                {
                    if(tryDecryptFile(encFile.data, ""))
                    {
                        string encContent = FileEncrypter.DecryptString(encFile.data, "")[2];
                        if(encContent.Contains(content))
                        {
                            decryptedFile = encContent;
                            canFind = true;
                        }
                    }
                    if (canFind) continue;
                    foreach(var folder in comp.files.root.folders)
                    {
                        if (folder.files.Any(f => tryDecryptFile(encFile.data, f.data))) canFind = true;
                    }
                    if(comp.Memory != null)
                    {
                        if (comp.Memory.CommandsRun.Any(c => tryDecryptFile(encFile.data, c))) canFind = true;
                        if (comp.Memory.DataBlocks.Any(d => tryDecryptFile(encFile.data, d))) canFind = true;
                    }
                    if(attachedIDs.Any(c => c.StartsWith("eos_")))
                    {
                        var eosID = attachedIDs.First(c => c.StartsWith("eos_"));
                        var eosDevice = layer.nodes.First(c => c.idName == eosID);
                        var notesFolder = eosDevice.files.root.searchForFolder("eos").searchForFolder("notes");
                        if(notesFolder.files.Any())
                        {
                            possiblePass = notesFolder.files[0].data;
                            if (tryDecryptFile(encFile.data, possiblePass)) canFind = true;
                        }
                    }
                }

                return canFind;
            }

            bool findMemoryWithContent(Computer comp, string content)
            {
                bool canFind = false;
                if (comp.Memory == null) return false;
                canFind = comp.Memory.DataBlocks.Any(b => b.Contains(content)) || comp.Memory.CommandsRun.Any(c => c.Contains(content));
                return canFind;
            }
        }
    }
}
