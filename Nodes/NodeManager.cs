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
        private static OS os => OS.currentInstance;

        private static int PlayerNodeIndex => os.netMap.nodes.IndexOf(os.thisComputer);

        public static int AddNode(Computer comp)
        {
            os.netMap.nodes.Add(comp);
            return os.netMap.nodes.IndexOf(comp);
        }

        public static void RemoveNode(Computer comp)
        {
            os.netMap.nodes.Remove(comp);
        }

        public static void ReplaceNode(string id, Computer newComp)
        {
            var index = FindIndexOfNode(id);
            os.netMap.nodes[index] = newComp;
        }

        public static void SetNodeVisibility(string id, bool visible)
        {
            var index = FindIndexOfNode(id);
            if(visible)
            {
                if (!os.netMap.visibleNodes.Contains(index)) os.netMap.visibleNodes.Add(index);
            } else
            {
                if (os.netMap.visibleNodes.Contains(index)) os.netMap.visibleNodes.Remove(index);
            }
        }

        public static int FindIndexOfNode(string id)
        {
            var node = os.netMap.nodes.FirstOrDefault(n => n.idName == id);
            if (node == null) return -1;
            return os.netMap.nodes.IndexOf(node);
        }

        public static void ClearNetMap()
        {
            os.netMap.visibleNodes.RemoveAll(c => c != PlayerNodeIndex);
            os.netMap.nodes.RemoveAll(c => c.idName != "playerComp" && c.idName != "jmail" && c.idName != "ispComp");
        }

        public static Computer GetRandomNode(string except = null)
        {
            Random random = Utils.random;
            string[] bannedIDs = { "playerComp", "jmail", "ispComp", except };

            var nodes = os.netMap.nodes.FindAll(c => !bannedIDs.Contains(c.idName));
            int index = random.Next(0, nodes.Count);
            int indexOfComp = os.netMap.nodes.IndexOf(nodes[index]);

            return os.netMap.nodes[indexOfComp];
        }
    }
}
