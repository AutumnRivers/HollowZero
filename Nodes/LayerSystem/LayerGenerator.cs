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

namespace HollowZero.Nodes.LayerSystem
{
    public static class LayerGenerator
    {
        public const int MIN_LAYER_SIZE = 5;
        public const int MAX_LAYER_SIZE = 10;

        private static Random random = new();
        public static int LayerSize => random.Next(MIN_LAYER_SIZE, MAX_LAYER_SIZE);

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

        public static bool canDecrypt = false;
        public static bool canMemDump = false;
        public static bool canWireshark = false;

        private static List<Computer> GenerateSolutionComp(Computer comp, Computer prevComp, int index, ref List<Computer> layerNodes,
            ref bool needsLinking)
        {
            var solutionType = solutionTypes.GetRandom();
            needsLinking = false;

            List<Computer> comps = new()
            {
                comp, prevComp
            };
            prevComp.Memory = new();

            switch(solutionType)
            {
                case GainAdminAccess:
                    linkComputer();
                    needsLinking = true;
                    break;
                case DecryptFile:
                    if(canDecrypt)
                    {
                        linkComputer();
                        needsLinking = true;
                        break;
                    }
                    var encFile = GenerateEncryptedFile(prevComp, comp, out string pass);
                    if(pass != "")
                    {
                        int chance = random.Next(1, 4);
                        switch(chance)
                        {
                            case 1:
                                prevComp.getFolderFromPath("home").files.Add(new FileEntry(pass, "enc_pass.txt"));
                                break;
                            case 2:
                                prevComp.Memory.CommandsRun.Add($"encypher next.txt {pass}");
                                break;
                            case 3:
                                prevComp.Memory.DataBlocks.Add($"pass: {pass} (delete later)");
                                break;
                            case 4:
                                var eos = GenerateEOSDevice("Test eOS", $"{encFile.name} pass: {pass}", prevComp);
                                layerNodes.Add(eos);
                                break;
                        }
                    }
                    prevComp.getFolderFromPath("home").files.Add(encFile);
                    break;
                case MemoryDump:
                    if(canMemDump)
                    {
                        linkComputer();
                        needsLinking = true;
                        break;
                    }
                    prevComp.Memory.DataBlocks.Add("--) " + comp.ip);
                    break;
                case WiresharkCapture:
                    if(canWireshark)
                    {
                        linkComputer();
                        needsLinking = true;
                        break;
                    }
                    WiresharkContents wsContent = new();
                    WiresharkEntry wsEntry = new(1, prevComp.ip, comp.ip, $"--- {comp.ip} ---", false);
                    wsContent.entries.Add(wsEntry);
                    StuxnetCore.wiresharkComps.Add(prevComp.idName, wsContent);
                    break;
            }

            void linkComputer()
            {
                //comp.links.Add(index + 1);
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
            os.netMap.nodes.Add(eosDevice);
            linkedComp.links.Add(os.netMap.nodes.IndexOf(eosDevice));
            linkedComp.attatchedDeviceIDs += eosID;
            return eosDevice;
        }

        public static HollowLayer GenerateSolvableLayer()
        {
            HollowLayer layer = new();
            int layerSize = LayerSize;

            Computer lastComp = null;

            for(var i = 0; i < layerSize + 1; i++)
            {
                bool lastNode = i == layerSize;
                bool link = false;
                var genComp = NodeGenerator.GenerateComputer($"TestComp{i + 1}");
                if (Utils.flipCoin())
                {
                    genComp = NodeGenerator.GenerateEventComputer($"TestComp{i + 1}");
                }
                List<Computer> solComps = new();
                if(i > 0)
                {
                    LogDebug(lastComp.idName);
                    solComps = GenerateSolutionComp(genComp, lastComp, i, ref layer.nodes, ref link);
                    genComp = solComps[0];
                }
                if(lastNode)
                {
                    // TODO: Layer transition node stuff...
                }
                layer.nodes.Add(genComp);
                int currentCompIndex = layer.nodes.IndexOf(genComp);
                int lastCompIndex = i > 0 ? layer.nodes.IndexOf(lastComp) : -1;
                if(link)
                {
                    solComps[1].links.Add(currentCompIndex);
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
                    solution.Add(new(node, LayerSolutionStep.SolutionType.GainAdminAccess));
                    continue;
                }
                if(node.links.Contains(idx + 1))
                {
                    solution.Add(new(node, LayerSolutionStep.SolutionType.GainAdminAccess, nextNode));
                    continue;
                } else
                {
                    canSolve = false;
                }
            }

            return canSolve;
        }
    }
}
