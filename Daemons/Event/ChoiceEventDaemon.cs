using System;
using System.Collections.Generic;

using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.GUI;
using Pathfinder.Util;

using HollowZero.Choices;

namespace HollowZero.Daemons.Event
{
    internal class ChoiceEventDaemon : EventDaemon
    {
        public ChoiceEventDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        [XMLStorage]
        public string CEventTitle = "Choice Event";

        [XMLStorage(IsContent = true)]
        public string CEventContent = "Choice Event Content";

        public ChoiceEvent choiceEvent;
        public const int BUTTON_MARGIN = 10;

        public override void initFiles()
        {
            base.initFiles();

            choiceEvent = DefaultChoiceEvents.choiceEvents[0];

            EventTitle = choiceEvent.Title;
            EventContent = choiceEvent.Content;
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            DrawEventTemplate(bounds);

            int buttonOffset = 75;
            for (var i = choiceEvent.Choices.Count - 1; i > -1; i--)
            {
                var choice = choiceEvent.Choices[i];
                string bText = $"{choice.Title}\n{choice.Subtext}";
                var b = Button.doButton(choice.ButtonID, bounds.X + 25,
                    bounds.Y + bounds.Height - buttonOffset, bounds.Width - 50, 50, bText, choice.Color);
                if (b)
                {
                    choice.OnPressed.Invoke();
                    OS.currentInstance.display.command = "probe";
                    comp.daemons.Remove(this);
                    comp.initDaemons();
                }
                buttonOffset += 60;
            }
        }
    }

    internal class Choice
    {
        public string Title;
        public string Subtext;
        public Action OnPressed;
        public Color Color;
        public int ButtonID = PFButton.GetNextID();
    }

    internal class ChoiceEvent
    {
        public string Title;
        public string Content;
        public List<Choice> Choices = new List<Choice>();
    }
}
