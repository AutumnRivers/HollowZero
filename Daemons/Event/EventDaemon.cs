using System.Collections.Generic;
using System.Linq;
using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.Util;

namespace HollowZero.Daemons.Event
{
    public class EventDaemon : HollowDaemon
    {
        public EventDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        [XMLStorage]
        public string IsOneShot = "false";

        public bool OneShot => bool.Parse(IsOneShot);

        public string EventTitle;
        public string EventFirstContactContent;
        public string EventContent;

        public List<string> ContentPages = new List<string>();
        public int CurrentPage { get; private set; } = 0;

        public bool HasFirstContact { get { return EventFirstContactContent != null; } }

        public readonly SpriteFont TitleFont = GuiData.font;
        public readonly SpriteFont ContentFont = GuiData.smallfont;

        public const int DEFAULT_OFFSET = 25;

        protected int DrawEventTemplate(Rectangle bounds)
        {
            int offset = 50;
            string content = EventContent.ToString();

            if(HasFirstContact && !HollowZeroCore.SeenEvents.Contains(EventTitle))
            {
                content = EventFirstContactContent;
            }
            if(ContentPages.Any())
            {
                ContentPages[0] = content;
                content = ContentPages[CurrentPage];
            }

            DrawCenteredText(bounds, EventTitle, TitleFont, bounds.Y + offset);
            offset += GetStringHeight(TitleFont, EventTitle) + 10;
            DrawHorizontalSeparator(bounds, bounds.Y + offset);

            string parsedContent = Utils.SuperSmartTwimForWidth(content, bounds.Width - DEFAULT_OFFSET, ContentFont);
            offset += DEFAULT_OFFSET;

            TextItem.doSmallLabel(new Vector2(bounds.X + DEFAULT_OFFSET, bounds.Y + offset), parsedContent, Color.White);

            return offset;
        }

        public void ChangePage(int page)
        {
            if (!ContentPages.Any()) return;
            if (page < 0)
            {
                CurrentPage = 0;
                return;
            }

            if(ContentPages.Count > page)
            {
                CurrentPage = page;
            } else
            {
                CurrentPage = ContentPages.Count - 1;
            }
        }
    }

    public class CustomEvent
    {
        public string Title;
        public string FirstContactContent;
        public string Content;
        public bool Unavoidable = false;

        public List<string> ContentPages = new List<string>();

        public bool HasFirstContact
        {
            get { return FirstContactContent != null; }
        }
    }

    internal class EventManager
    {
        private static readonly EventDetails DefaultDetails = new EventDetails()
        {
            Title = "Event Title",
            Content = "Event Content"
        };

        public static ChoiceEventDaemon CreateChoiceEventDaemon(ChoiceEvent choiceEvent, EventDetails details = null)
        {
            details ??= DefaultDetails;
            var cDaemon = new ChoiceEventDaemon(OS.currentInstance.thisComputer, "Choice Event", OS.currentInstance)
            {
                choiceEvent = choiceEvent,
                CEventTitle = details.Title,
                CEventContent = details.Content
            };
            return cDaemon;
        }

        public static void AddChoiceEventToNode(Computer comp, ChoiceEvent choiceEvent, EventDetails details)
        {
            var c = CreateChoiceEventDaemon(choiceEvent, details);
            comp.daemons.Add(c);
            comp.initDaemons();
        }
    }

    internal class EventDetails
    {
        public string Title;
        public string Content;
        public bool OneShot = true;
    }
}
