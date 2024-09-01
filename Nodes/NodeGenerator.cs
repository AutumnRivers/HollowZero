using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using HollowZero.Daemons;
using HollowZero.Daemons.Event;
using HollowZero.Daemons.Shop;

using Pathfinder.Daemon;

namespace HollowZero.Nodes
{
    internal class NodeGenerator
    {
        public static Computer GenerateComputer(string title, List<BaseDaemon> daemons, List<Computer> connectedComps = null, string ip = null)
        {
            ip ??= GetNewIP();
            OS os = OS.currentInstance;
            Computer newNode = new(title, ip, os.netMap.getRandomPosition(), 0, 4, os);
            newNode.idName = GenerateID();

            if (daemons == null) return newNode;

            foreach(var daemon in daemons)
            {
                newNode.daemons.Add(daemon);
            }
            newNode.initDaemons();

            foreach(var node in connectedComps)
            {
                var onNetmap = os.netMap.nodes.FirstOrDefault(c => c == node);
                if (onNetmap == default) continue;

                newNode.links.Add(os.netMap.nodes.IndexOf(node));
            }

            return newNode;
        }

        public static Computer GenerateComputer(string title)
        {
            return GenerateComputer(title, null);
        }

        private static readonly Random random = new();

        private static string GenerateID()
        {
            var id = random.Next(0, 1000).ToString();
            OS os = OS.currentInstance;

            if (os.netMap.nodes.Exists(c => c.idName == id))
            {
                return GenerateID();
            }
            return id;
        }

        private static readonly List<string> possibleEvents = new()
        {
            "choice", "dialogue", "avshop", "gachashop", "progshop", "reststop",
            "none"
        };

        public static Computer GenerateEventComputer(string title)
        {
            int idx = random.Next(0, possibleEvents.Count);
            string ev = possibleEvents[idx];
            HollowDaemon eventDaemon;
            var comp = GenerateComputer(title);
            OS os = OS.currentInstance;

            switch(ev)
            {
                case "choice":
                    eventDaemon = new ChoiceEventDaemon(comp, "Choice Event", os);
                    break;
                case "avshop":
                    eventDaemon = new AntivirusShopDaemon(comp, "Antivirus Shop", os);
                    break;
                case "gachashop":
                    eventDaemon = new GachaShopDaemon(comp, "Gacha Shop", os);
                    break;
                case "progshop":
                    eventDaemon = new ProgramShopDaemon(comp, "Program Shop", os);
                    break;
                case "reststop":
                    eventDaemon = new RestStopDaemon(comp, "Rest Stop", os);
                    break;
                case "none":
                default:
                    eventDaemon = null;
                    break;
            }

            if (eventDaemon == null) return comp;
            comp.daemons.Add(eventDaemon);
            comp.initDaemons();
            return comp;
        }

        public static Computer GenerateProgramShopComp()
        {
            var comp = GenerateComputer("Program Shop");
            ProgramShopDaemon programShop = new(comp, "Program Shop", OS.currentInstance);
            comp.daemons.Add(programShop);
            comp.initDaemons();
            return comp;
        }

        public static Computer GenerateTransitionNode()
        {
            var comp = GenerateComputer("Exit Point " + random.Next(1, 10) + random.Next(1, 10) + random.Next(1, 10));
            LayerTransitionDaemon transitionDaemon = new(comp, "Transition Point", OS.currentInstance);
            comp.daemons.Add(transitionDaemon);
            comp.initDaemons();
            return comp;
        }

        public static Computer GenerateAndAddComputer(string title, List<BaseDaemon> daemons, string ip = null)
        {
            var node = GenerateComputer(title, daemons, ip: ip);
            NodeManager.AddNode(node);
            return node;
        }

        public static Computer GenerateAndAddComputer(string title)
        {
            var node = GenerateComputer(title, null);
            NodeManager.AddNode(node);
            return node;
        }

        public static void AddNewLinkToComp(Computer sourceComputer, Computer targetComputer)
        {
            sourceComputer.links.Add(OS.currentInstance.netMap.nodes.IndexOf(targetComputer));
        }

        public static void AddRandomLinkToComp(Computer sourceComputer)
        {
            var targetComp = NodeManager.GetRandomNode(sourceComputer.idName);
            AddNewLinkToComp(sourceComputer, targetComp);
        }

        public static string GetNewIP()
        {
            return NetworkMap.generateRandomIP();
        }

        public static string GetNewURL()
        {
            string[] subdomains = { "www", "web", "ftp", "login", "admin", "git" };
            string[] words = { "entech", "hack", "cool", "mud", "grass", "zone", "one", "trust", "life", "resist", "time", "moon", "lunar",
            "secure", "lock", "love", "hub", "only", "flans", "source", "code", "sharp", "bad", "good", "mid", "hand" };
            string[] tlds = { "com", "net", "site", "org", "gov", "mail", "strudel", "dev", "tech" };

            StringBuilder websiteURL = new StringBuilder();
            GenURL();

            return websiteURL.ToString();

            void GenURL()
            {
                for (var i = 0; i < 3; i++)
                {
                    Random random = new Random();

                    switch (i)
                    {
                        case 0:
                            int subIndex = random.Next(0, subdomains.Length - 1);
                            websiteURL.Append(subdomains[subIndex] + ".");
                            break;
                        case 1:
                            int wordIndex = random.Next(0, words.Length - 1);
                            websiteURL.Append(words[wordIndex]);
                            wordIndex = random.Next(0, words.Length - 1);
                            websiteURL.Append(words[wordIndex] + ".");
                            break;
                        case 2:
                            int tldIndex = random.Next(0, tlds.Length - 1);
                            websiteURL.Append(tlds[tldIndex]);
                            break;
                    }
                }

                if(OS.currentInstance.netMap.nodes.Exists(c => c.ip == websiteURL.ToString()))
                {
                    websiteURL = new StringBuilder();
                    GenURL();
                }
            }
        }
    }
}
