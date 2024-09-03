using Hacknet;
using Hacknet.Effects;
using HollowZero.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathfinder.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero.Daemons
{
    public class RestStopDaemon : HollowDaemon
    {
        public RestStopDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os)
        {
            Backdrop.MaxVerticalLandingVariane = 0.06f;
            Backdrop.FallRate = 0.3f;
        }

        public static new bool Registerable => true;

        public static int ReduceInfectionBy { get; internal set; } = 25;

        private readonly RaindropsEffect Backdrop = new RaindropsEffect();
        private readonly int RestButtonID = PFButton.GetNextID();

        public override void navigatedTo()
        {
            base.navigatedTo();

            Backdrop.Init(OS.currentInstance.content);
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            var gt = (float)OS.currentInstance.lastGameTime.ElapsedGameTime.TotalSeconds;
            Backdrop.Update(gt, 10f);
            Backdrop.Render(bounds, GuiData.spriteBatch, Color.CornflowerBlue * 0.75f, 5f, 30f);

            DrawCenteredText(bounds, "Tranquility at last...", GuiData.font, (bounds.Height / 2) / 2, Color.White);
            var trimmedText = Utils.SmartTwimForWidth("The healing power of the digital raindrops soothe your soul, " +
                "lowering your current Infection. It might be nice to take a break here...", bounds.Width - 20, GuiData.smallfont);
            DrawCenteredText(bounds, trimmedText, GuiData.smallfont, bounds.Center.Y, Color.White);

            HollowButton RestButton = new HollowButton(RestButtonID, bounds.X + (bounds.Width / 4), bounds.Height - 70,
                bounds.Width / 2, 50, $"Take a rest (-{ReduceInfectionBy} Infection)", Color.Blue);
            RestButton.OnPressed = delegate ()
            {
                PlayerManager.DecreaseInfection(ReduceInfectionBy);
                PFButton.ReturnID(RestButtonID);
                RemoveDaemon();
            };
            RestButton.DoButton();
        }
    }
}
