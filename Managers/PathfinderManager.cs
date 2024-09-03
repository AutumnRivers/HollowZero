using BepInEx.Hacknet;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero.Managers
{
    public class HollowPFManager
    {
        public static Assembly LoadAssemblyThroughPF(string path)
        {
            var renamedAssemblyResolver = typeof(HacknetChainloader).Assembly.GetType("BepInEx.Hacknet.RenamedAssemblyResolver", true);
            var chainloaderFix = typeof(HacknetChainloader).Assembly.GetType("BepInEx.Hacknet.ChainloaderFix", true);
            var chFixRemaps = chainloaderFix.GetPrivateStaticField<Dictionary<string, Assembly>>("Remaps");
            var chFixRemapDefs = chainloaderFix.GetPrivateStaticField<Dictionary<string, AssemblyDefinition>>("RemapDefinitions");

            byte[] asmBytes;
            string name;

            var asm = AssemblyDefinition.ReadAssembly(path, new ReaderParameters()
            {
                AssemblyResolver = (IAssemblyResolver)Activator.CreateInstance(renamedAssemblyResolver)
            });
            name = asm.Name.Name;
            asm.Name.Name = asm.Name.Name + "-" + DateTime.Now.Ticks;

            using (var ms = new MemoryStream())
            {
                asm.Write(ms);
                asmBytes = ms.ToArray();
            }

            var loaded = Assembly.Load(asmBytes);
            chFixRemaps[name] = loaded;
            chFixRemapDefs[name] = asm;

            chainloaderFix.SetPrivateStaticField("Remaps", chFixRemaps);
            chainloaderFix.SetPrivateStaticField("RemapDefinitions", chFixRemapDefs);

            HollowZeroCore.knownPackAsms.Add(loaded);

            return loaded;
        }
    }
}
