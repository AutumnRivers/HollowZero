using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

using HollowZero.Daemons;
using HollowZero.Daemons.Event;

using Pathfinder.Daemon;

namespace HollowZero.Nodes
{
    internal class NodeGenerator
    {
        public static Computer GenerateComputer(string title, List<BaseDaemon> daemons, List<Computer> connectedComps = null, string ip = null)
        {
            ip ??= GetNewIP();
            OS os = OS.currentInstance;
            Computer newNode = new Computer(title, ip, os.netMap.getRandomPosition(), 0, 4, os);

            foreach(var daemon in daemons)
            {
                newNode.daemons.Add(daemon);
            }
            newNode.initDaemons();

            return newNode;
        }

        public static void GenerateAndAddComputer(string title, List<BaseDaemon> daemons, string ip = null)
        {
            var node = GenerateComputer(title, daemons, ip: ip);
            NodeManager.AddNode(node);
        }

        public static void AddNewLinkToComp(Computer sourceComputer, Computer targetComputer)
        {
            sourceComputer.links.Add(OS.currentInstance.netMap.nodes.IndexOf(targetComputer));
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

            for(var i = 0; i < 3; i++)
            {
                Random random = new Random();

                switch(i)
                {
                    case 0:
                        int subIndex = random.Next(0, subdomains.Length - 1);
                        websiteURL.Append(subdomains[subIndex]);
                        break;
                    case 1:
                        int wordIndex = random.Next(0, words.Length - 1);
                        websiteURL.Append(words[wordIndex]);
                        wordIndex = random.Next(0, words.Length - 1);
                        websiteURL.Append(words[wordIndex]);
                        wordIndex = random.Next(0, words.Length - 1);
                        websiteURL.Append(words[wordIndex]);
                        break;
                    case 2:
                        int tldIndex = random.Next(0, tlds.Length - 1);
                        websiteURL.Append(tlds[tldIndex]);
                        break;
                }
            }

            return websiteURL.ToString();
        }
    }
}
