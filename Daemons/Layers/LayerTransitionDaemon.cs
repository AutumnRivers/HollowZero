using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.Daemon;
using Pathfinder.GUI;

using HollowZero.Managers;
using Hacknet.Gui;

namespace HollowZero.Daemons
{
    public class LayerTransitionDaemon : HollowDaemon
    {
        public LayerTransitionDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public static new bool Registerable => false;

        public int Layer => PlayerManager.CurrentLayer;
        public int TransButtonID { get; private set; }

        private ShiftingGridEffect gridEffect = new();

        public override void navigatedTo()
        {
            TransButtonID = PFButton.GetNextID();
            base.navigatedTo();
        }

        private Color bgColor1 = new(30, 59, 44, 0);
        private Color bgColor2 = new(89, 181, 183, 0);
        private Color bgColor3 = new(19, 51, 35, 0);

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            OS os = OS.currentInstance;

            UpdateGrid();
            gridEffect.RenderGrid(bounds, sb, bgColor1, bgColor2, bgColor3, true);
            RenderedRectangle.doRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height,
                Color.Black * 0.75f);

            int buttonWidth = bounds.Width / 2;
            int buttonHeight = 50;
            HollowButton transitionButton = new(TransButtonID,
                bounds.Center.X - (buttonWidth / 2), bounds.Center.Y - (buttonHeight / 2),
                buttonWidth, buttonHeight, "Next Layer ->", os.brightUnlockedColor);
            if(!comp.PlayerHasAdminPermissions())
            {
                transitionButton.Disabled = true;
                transitionButton.DisabledMessage = "<!> You need to gain admin access before continuing!";
                transitionButton.Text = "(LOCKED)";
            }
            if(PlayerManager.Transitioning)
            {
                transitionButton.Disabled = true;
                transitionButton.DisabledMessage = "<!> Pay attention!";
                transitionButton.Text = "(SPINNING UP...)";
            }
            transitionButton.OnPressed = delegate ()
            {
                // Change layer here...
                PlayerManager.MoveToNextLayer();
            };
            transitionButton.DoButton();
        }

        private void UpdateGrid()
        {
            float gameTime = (float)OS.currentInstance.lastGameTime.ElapsedGameTime.TotalSeconds;
            gridEffect.Update(gameTime);
        }

        internal override void OnDisconnect()
        {
            PFButton.ReturnID(TransButtonID);
            base.OnDisconnect();
        }
    }
}
