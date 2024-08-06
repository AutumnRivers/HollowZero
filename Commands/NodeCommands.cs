using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

namespace HollowZero.Commands
{
    public class NodeCommands
    {
        public static void ListAvailableNodes(OS os, string[] args)
        {
            List<Computer> visisbleNodes = new List<Computer>();

            foreach(var nodeIndex in os.netMap.visibleNodes)
            {
                visisbleNodes.Add(os.netMap.nodes[nodeIndex]);
            }

            os.terminal.writeLine("<...> Finding available nodes...");
            foreach(var node in visisbleNodes)
            {
                os.terminal.writeLine($"[{node.name}] :: {node.ip}");
            }
            os.terminal.writeLine("<!> END OF AVAILABLE NODE LIST");
        }
    }
}
