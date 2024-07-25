using System;

using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;

using Pathfinder.Executable;

using HollowZero.Daemons;

namespace HollowZero.Executables
{
    public class InfecTracker : GameExecutable
    {
        public const int MAX_CORRUPTIONS = 4;

        public readonly Color LowColor = Color.Green;
        public readonly Color MedColor = Color.Goldenrod;
        public readonly Color HighColor = Color.Red;

        public const int RAM_COST = 100;

        public InfecTracker() : base()
        {
            this.baseRamCost = RAM_COST;
            this.ramCost = RAM_COST;
            this.IdentifierName = "InfecTracker";
            this.name = "InfecTracker";
            this.needsProxyAccess = false;
            this.CanBeKilled = false;
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
        }

        public override void Update(float t)
        {
            base.Update(t);
        }

        public override void Draw(float t)
        {
            base.Draw(t);
            drawTarget();
            drawOutline();

            int infection = HollowZeroCore.InfectionLevel;
            Color meterColor = infection < 50 ? Color.Lerp(LowColor, MedColor, (float)infection / 50) :
                Color.Lerp(MedColor, HighColor, ((float)infection - 50) / 50);

            RenderedRectangle.doRectangle(bounds.X, bounds.Y, (int)(bounds.Width * ((float)infection / 100)), 35, meterColor);

            int xOffset = 3;
            for(var i = 0; i < MAX_CORRUPTIONS; i++)
            {
                bool isMalware = HollowZeroCore.CollectedMalware.Count >= i + 1;

                float squareWidth = (float)Math.Floor((float)(bounds.Width / MAX_CORRUPTIONS));

                if(i == MAX_CORRUPTIONS - 1)
                {
                    squareWidth -= 2;
                }

                Rectangle rect = new Rectangle()
                {
                    X = xOffset, Y = bounds.Y + 35,
                    Width = (int)squareWidth, Height = bounds.Height - 36
                };

                RenderedRectangle.doRectangleOutline(xOffset, bounds.Y + 35, (int)squareWidth, bounds.Height - 36, 1,
                    (isMalware ? Color.Red : Color.LightGray) * 0.5f);
                HollowDaemon.DrawTrueCenteredText(rect, isMalware ? "<!>" : "n/a", GuiData.tinyfont,
                    isMalware ? Color.DarkRed : Color.Gray);

                if(rect.Contains(GuiData.getMousePoint()) && !GuiData.blockingInput)
                {
                    float opacity = 0.25f;
                    if(GuiData.isMouseLeftDown())
                    {
                        opacity = 0.15f;
                    }
                    RenderedRectangle.doRectangle(rect.X, rect.Y, rect.Width, rect.Height,
                        (isMalware ? Color.Red : Color.White) * opacity);
                }

                xOffset += (int)squareWidth;
            }
        }
    }
}
