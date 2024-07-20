using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.Daemon;

namespace HollowZero.Daemons
{
    internal class HollowDaemon : BaseDaemon
    {
        internal HollowDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public const int HSEP_HEIGHT = 4;

        protected void DrawCenteredText(Rectangle bounds, string text, SpriteFont font, int startingHeight)
        {
            Vector2 textVector = font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                (float)(bounds.X + bounds.Width / 2) - textVector.X / 2f,
                (float)(startingHeight) - textVector.Y / 2f);

            GuiData.spriteBatch.DrawString(font, text, textPosition, Color.White);
        }

        protected void DrawHorizontalSeparator(Rectangle bounds, int startingHeight)
        {
            RenderedRectangle.doRectangle(bounds.X, startingHeight - (HSEP_HEIGHT / 2), bounds.Width, HSEP_HEIGHT, Color.White);
        }

        protected Vector2 GetStringSize(SpriteFont font, string content)
        {
            return font.MeasureString(content);
        }

        protected int GetStringHeight(SpriteFont font, string content)
        {
            return (int)Math.Ceiling(GetStringSize(font, content).Y);
        }
    }
}
