using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;

namespace HollowZero.Nodes
{
    internal class NodeManager
    {
        public static void AddNode(Computer comp)
        {
            OS.currentInstance.netMap.nodes.Add(comp);
        }

        public static void RemoveNode(Computer comp)
        {
            OS.currentInstance.netMap.nodes.Remove(comp);
        }

        public static void ClearNetMap()
        {
            OS.currentInstance.netMap.nodes.RemoveAll(c => c.idName != "playerComp" && c.idName != "jmail" && c.idName != "ispComp");
        }

        public static Computer GetRandomNode(string except = null)
        {
            Random random = new Random();
            string[] bannedIDs = { "playerComp", "jmail", "ispComp", except };

            var nodes = OS.currentInstance.netMap.nodes.FindAll(c => !bannedIDs.Contains(c.idName));
            int index = random.Next(0, nodes.Count);
            int indexOfComp = OS.currentInstance.netMap.nodes.IndexOf(nodes[index]);

            return OS.currentInstance.netMap.nodes[indexOfComp];
        }
    }
}
