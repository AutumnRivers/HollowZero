using Hacknet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero.Daemons.Event
{
    public class ChanceEventDaemon : UnavoidableEventDaemon
    {
        public ChanceEventDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        private int ExitButtonID = PFButton.GetNextID();

        public override void navigatedTo()
        {
            base.navigatedTo();
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            var exitButton = new HollowButton(ExitButtonID, bounds.X + 10, bounds.Y + 10, 200, 100, "Exit...", Color.Red);
            exitButton.OnPressed = delegate ()
            {
                UnlockModules();
                RemoveDaemon();
            };
            exitButton.DoButton();
        }
    }
}
