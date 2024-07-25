using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Gui;
using Hacknet.UIUtils;

using Microsoft.Xna.Framework;

using Pathfinder.Executable;

using HollowZero.Daemons;

namespace HollowZero.Executables
{
    public class Cryptominer : GameExecutable
    {
        public Cryptominer()
        {
            this.ramCost = 100;
            this.baseRamCost = 100;
            this.IdentifierName = "Cryptominer";
            this.name = "Cryptominer";
            this.needsProxyAccess = false;
            this.CanBeKilled = false;
        }

        public override void Draw(float t)
        {
            base.Draw(t);
            drawTarget();
            drawOutline();

            string message = "$ Mining... $";
            if(isExiting)
            {
                message = "I'll be back~";
            }

            PatternDrawer.draw(bounds, 1f, OS.currentInstance.moduleColorBacking, OS.currentInstance.moduleColorSolid, GuiData.spriteBatch);
            HollowDaemon.DrawTrueCenteredText(bounds, message, GuiData.font);
        }
    }
}
