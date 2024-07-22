using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.GUI;
using Pathfinder.Util;

namespace HollowZero.Daemons.Event
{
    internal class DialogueEventDaemon : EventDaemon
    {
        public DialogueEventDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        [XMLStorage]
        public string DEventTitle = "Dialogue Event Title";

        [XMLStorage]
        public string IsOneShot = "true";

        [XMLStorage(IsContent = true)]
        public string DEventContent = "Dialogue Event Content";

        private readonly Color ButtonColor = Color.CornflowerBlue;
        private readonly int ButtonID = PFButton.GetNextID();

        private const string CONTINUE_TEXT = "Continue >>>";

        public override string Identifier => "Dialogue Event";

        public override void initFiles()
        {
            base.initFiles();

            EventTitle = DEventTitle;
            EventContent = DEventContent;

            OneShot = bool.Parse(IsOneShot);
        }

        public override void draw(Rectangle bounds, SpriteBatch sb)
        {
            base.draw(bounds, sb);

            DrawEventTemplate(bounds);
            var continueButton = Button.doButton(ButtonID, bounds.X + 25, bounds.Y + bounds.Height - 75, 200, 50, CONTINUE_TEXT, ButtonColor);

            if(continueButton)
            {
                OS.currentInstance.display.command = "probe";
                if (!OneShot) return;
                RemoveDaemon();
            }
        }
    }
}
