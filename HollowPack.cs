using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero.Packs
{
    public abstract class HollowPack
    {
        public abstract void OnRegister();
    }

    public class HollowPackMetadata : Attribute
    {
        public HollowPackMetadata(string id, string author)
        {
            PackID = id;
            PackAuthor = author;
        }

        public string PackID { get; protected set; }
        public string PackAuthor { get; protected set; }
    }
}
