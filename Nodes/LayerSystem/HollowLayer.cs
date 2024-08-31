using Hacknet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static HollowZero.HollowLogger;

namespace HollowZero.Nodes.LayerSystem
{
    public class HollowLayer
    {
        public List<Computer> nodes = new();
        // ID1, ID2
        public List<KeyValuePair<string, string>> nodeConnections = new();

        public bool Solvable { get; internal set; } = false;
        public List<LayerSolutionStep> Solution { get; internal set; } = new();

        public bool Active { get; set; } = false;

        public override string ToString()
        {
            StringBuilder response = new("HollowLayer:\n");
            foreach(var node in nodes)
            {
                response.Append($"Node:{node.name}|{node.idName}|{node.daemons.Any()}");
                if(node.daemons.Any())
                {
                    response.Append("|Daemons:");
                    foreach (var daemon in node.daemons)
                    {
                        response.Append($"{daemon.name}");
                    }
                }
                if(node.links.Any())
                {
                    response.Append("|Connections:");
                    foreach(var link in node.links)
                    {
                        response.Append($"Node_{nodes[link].idName}");
                    }
                }
                response.Append("\n");
            }
            response.Append($"Solvable:{Solvable}\n");
            if(Solution.Any())
            {
                response.Append("Solution:\n");
                for(var i = 0; i < Solution.Count; i++)
                {
                    var sol = Solution[i];
                    response.Append($"{i+1}|Node_{sol.Comp.idName}|{sol.Solution.ToString()}");
                    if(sol.NextComp != null) { response.Append($"|GoTo:Node_{sol.NextComp.idName}"); };
                    response.Append("\n");
                }
            } else
            {
                response.Append("Solution:N/A\n");
            }
            response.Append("SolutionEnd\n");
            response.Append($"Active:{Active}");
            return response.ToString();
        }
    }

    public class LayerSolutionStep
    {
        public enum SolutionType
        {
            GainAdminAccess,
            DecryptFile,
            MemoryDump,
            WiresharkCapture
        }

        public Computer Comp;
        public SolutionType Solution;
        public Computer NextComp;

        public LayerSolutionStep(Computer source, SolutionType type)
        {
            Comp = source;
            Solution = type;
        }

        public LayerSolutionStep(Computer source, SolutionType type, Computer linkedComp)
        {
            Comp = source;
            Solution = type;
            NextComp = linkedComp;
        }
    }
}
