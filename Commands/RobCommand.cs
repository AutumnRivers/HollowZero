using Hacknet;
using HollowZero.Daemons.Shop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero.Commands
{
    public class RobCommand
    {
        public static void RobStore(OS os, string[] args)
        {
            if(args.Length < 2)
            {
                os.write("<!> You need to enter the item name!");
                os.validCommand = false;
                return;
            }
            if(!os.connectedComp.daemons.Any(d => d.GetType().IsSubclassOf(typeof(ShopDaemon))))
            {
                os.write("<!> You can't rob a non-existent shop!");
                os.validCommand = false;
                return;
            }
            string item = string.Join(" ", args.Skip(1));
            var shop = (ShopDaemon)os.connectedComp.daemons.First(d => d.GetType().IsSubclassOf(typeof(ShopDaemon)));
            shop.RobStore(item);
        }
    }
}
