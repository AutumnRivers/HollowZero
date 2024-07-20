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
    }
}
