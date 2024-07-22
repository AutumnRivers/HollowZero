using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero.Packs
{
    public abstract class HollowPack
    {
        public abstract string PackID { get; set; }
        public virtual string PackAuthor => "UNKNOWN";
        public abstract void OnRegister();
    }
}
