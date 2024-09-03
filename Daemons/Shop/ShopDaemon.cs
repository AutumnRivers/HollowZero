using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using HollowZero.Managers;

namespace HollowZero.Daemons.Shop
{
    public class ShopDaemon : HollowDaemon
    {
        public ShopDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public Dictionary<Modification, int> ModsForSale = new Dictionary<Modification, int>();
        public Dictionary<Corruption, int> CorrsForSale = new Dictionary<Corruption, int>();
        public Dictionary<HollowProgram, int> ProgramsForSale = new Dictionary<HollowProgram, int>();

        public Dictionary<string, string> BaseGameExeWildcards = new Dictionary<string, string>();
        public static List<HollowProgram> BaseGamePrograms = new List<HollowProgram>();

        public static List<HollowProgram> CustomPrograms = new();

        public static float PriceMultiplier { get; internal set; } = 1.0f;

        protected enum StoreScreen
        {
            Main, Shop, EmptyShop,
            ModShop, CorrShop, ProgShop,
            ShopResult, ViewCart
        }

        protected StoreScreen CurrentScreen = StoreScreen.Main;
        protected string LastBoughtItem = "ITEM";

        protected string CurrentItemInCart;
        protected int CartItemCost;
        protected bool CurrentItemIsBundle = false;

        private readonly int[] IgnorePorts = { 3724, 1, 4, 8, 15, 17, 31, 9418, 40, 3659 };

        public static new bool Registerable => false;

        public bool hasBeenRobbed = false;

        protected int GetFinalPrice(int basePrice)
        {
            return (int)Math.Ceiling(basePrice * PriceMultiplier);
        }

        public virtual void RobStore(string itemName)
        {
            if(hasBeenRobbed)
            {
                OS.currentInstance.write("<!> This store is on high alert! You can't rob it again.");
                return;
            }
            hasBeenRobbed = true;
        }

        public override void initFiles()
        {
            base.initFiles();

            foreach(var prog in ProgramLookup.ProgramIDs)
            {
                if (IgnorePorts.Contains(prog.Value)) continue;
                BaseGameExeWildcards.Add(PortExploits.cracks[prog.Value], PortExploits.crackExeData[prog.Value]);

                HollowProgram baseGameProgram = new HollowProgram(PortExploits.cracks[prog.Value].Split('.')[0])
                {
                    ProgramID = prog.Value,
                    FileContent = PortExploits.crackExeData[prog.Value]
                };
                BaseGamePrograms.Add(baseGameProgram);
            }

            var wiresharkProgram = new HollowProgram("Wireshark")
            {
                ProgramID = 11111,
                FileContent = ComputerLoader.filter("#WIRESHARK_EXE#")
            };
            var radioV3Program = new HollowProgram("RadioV3")
            {
                ProgramID = 33333,
                FileContent = ComputerLoader.filter("#RADIO_V3#")
            };

            CustomPrograms.Add(wiresharkProgram);
            CustomPrograms.Add(radioV3Program);
        }

        protected Func<HollowProgram, bool> ByName(string name)
        {
            return p => p.DisplayName == name;
        }

        public static string GetExeDataByName(string name)
        {
            var exe = BaseGamePrograms.First(p => p.DisplayName == name);
            return exe.FileContent;
        }

        protected void PropagateProgramsForSale()
        {
            // These should be starting programs...
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("SSHcrack")), 50);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("FTPBounce")), 50);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("FTPSprint")), 500); // Apart from this, of course. :3

            ProgramsForSale.Add(BaseGamePrograms.First(ByName("SMTPoverflow")), 100);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("WebServerWorm")), 100);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("SQL_MemCorrupt")), 200);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("KBT_PortTest")), 350);

            // These are sold in a bundle
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("Decypher")), 300);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("DECHead")), 300);

            ProgramsForSale.Add(BaseGamePrograms.First(ByName("SignalScramble")), 1100);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("eosDeviceScan")), 250);

            ProgramsForSale.Add(BaseGamePrograms.First(ByName("TorrentStreamInjector")), 300);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("SSLTrojan")), 350);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("PacificPortcrusher")), 300);

            // These are sold in a bundle
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("MemForensics")), 500);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("MemDumpGenerator")), 500);

            // Ideally, these should be sold in a bundle, too. QoL Bundle or something
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("TraceKill")), 1000);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("themechanger")), 750);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("NetmapOrganizer")), 750);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("DNotes")), 750);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("Tuneswap")), 750);

            ProgramsForSale.Add(BaseGamePrograms.First(ByName("RTSPCrack")), 500);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("OpShell")), 500);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("ComShell")), 750);

            // These are sold in a bundle
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("Clock")), 1000);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("HexClock")), 1000);
            ProgramsForSale.Add(BaseGamePrograms.First(ByName("ClockV2")), 2000);

            // Custom
            ProgramsForSale.Add(CustomPrograms[0], 650);
            ProgramsForSale.Add(CustomPrograms[1], 9999);
        }

        protected bool CanPurchaseItem(int cost)
        {
            return cost <= PlayerManager.PlayerCredits;
        }

        protected bool AttemptPurchaseItem(int cost)
        {
            return PlayerManager.RemovePlayerCredits(cost);
        }

        protected void RemoveProgram(string programName)
        {
            if (!BaseGamePrograms.Any(ByName(programName))) return;
            if (!ProgramsForSale.ContainsKey(BaseGamePrograms.First(ByName(programName)))) return;
            ProgramsForSale.Remove(BaseGamePrograms.First(ByName(programName)));
        }

        protected void RemoveProgram(params string[] programNames)
        {
            foreach(var name in programNames)
            {
                RemoveProgram(name);
            }
        }
    }

    public class HollowProgram
    {
        public HollowProgram(string display, string item)
        {
            if(ProgramLookup.ProgramIDs.ContainsKey(item.ToLower())) {
                ProgramID = ProgramLookup.ProgramIDs[item];
                FileContent = PortExploits.crackExeData[ProgramID];
            } else if(ProgramLookup.CustomProgramWildcards.ContainsKey(item))
            {
                ProgramID = 0;
                FileContent = ComputerLoader.filter(ProgramLookup.CustomProgramWildcards[item]);
            }
            DisplayName = display;
        }

        public HollowProgram(string display)
        {
            DisplayName = display;
        }

        public string DisplayName;
        public string ItemName;
        public int ProgramID = -1;
        public string FileContent;

        public override string ToString()
        {
            return $"Hollow Program : {DisplayName} (ID: {ProgramID})";
        }
    }

    public static class ProgramLookup
    {
        public static readonly Dictionary<string, int> ProgramIDs = new Dictionary<string, int>()
        {
            { "sshcrack", 22 },
            { "ftpbounce", 21 },
            { "smtpoverflow", 25 },
            { "sql_memcorrupt", 1433 },
            { "webserverworm", 80 },
            { "kbtporttest", 104 },
            { "decypher", 9 },
            { "dechead", 10 },
            { "eosdevicescan", 13 },
            { "opshell", 41 },
            { "tracekill", 12 },
            { "themechanger", 14 },
            { "clock", 11 },
            { "hexclock", 16 },
            { "securitytracer", 4 },
            { "hacknetexe", 15 },
            { "torrentstreaminjector", 6881 },
            { "ssltrojan", 443 },
            { "ftpsprint", 211 },
            { "memdumpgenerator", 34 },
            { "memforensics", 33 },
            { "signalscrambler", 32 },
            { "pacificportcrusher", 192 },
            { "comshell", 36 },
            { "netmaporganizer", 35 },
            { "dnotes", 37 },
            { "tuneswap", 39 },
            { "clockv2", 38 },
            { "rtspcrack", 554 }
        };

        public static Dictionary<string, string> CustomProgramWildcards = new Dictionary<string, string>()
        {
            { "wireshark", "#WIRESHARK_EXE#" }
        };
    }
}
