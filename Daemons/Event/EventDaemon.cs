using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace HollowZero.Daemons.Event
{
    internal class EventDaemon : HollowDaemon
    {
        public EventDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public string EventTitle;
        public string EventContent;

        public readonly SpriteFont TitleFont = GuiData.font;
        public readonly SpriteFont ContentFont = GuiData.smallfont;

        public const int DEFAULT_OFFSET = 25;

        protected int DrawEventTemplate(Rectangle bounds)
        {
            int offset = 50;
            string content = EventContent.ToString();

            DrawCenteredText(bounds, EventTitle, TitleFont, bounds.Y + offset);
            offset += GetStringHeight(TitleFont, EventTitle) + 10;
            DrawHorizontalSeparator(bounds, bounds.Y + offset);

            string parsedContent = Utils.SuperSmartTwimForWidth(content, bounds.Width - DEFAULT_OFFSET, ContentFont);
            offset += DEFAULT_OFFSET;

            TextItem.doSmallLabel(new Vector2(bounds.X + DEFAULT_OFFSET, bounds.Y + offset), parsedContent, Color.White);

            return offset;
        }
    }
}
