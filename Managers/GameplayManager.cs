using Hacknet.Effects;
using Hacknet.Extensions;
using Hacknet;
using HollowZero.Daemons.Event;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using static HollowZero.Managers.HollowGlobalManager;

namespace HollowZero.Managers
{
    public static class GameplayManager
    {
        public static void AddChoiceEvent(ChoiceEvent ev)
        {
            ChoiceEventDaemon.PossibleEvents.Add(ev);
        }

        public static void AddChoiceEvent(IEnumerable<ChoiceEvent> evs)
        {
            ChoiceEventDaemon.PossibleEvents.AddRange(evs);
        }

        public static void ForkbombComputer(Computer target)
        {
            Multiplayer.parseInputMessage($"eForkBomb {target.ip}", OS.currentInstance);
        }

        internal static void DrawTrueCenteredText(Rectangle bounds, string text, SpriteFont font, Color textColor = default)
        {
            textColor = textColor == default ? Color.White : textColor;
            Vector2 textVector = font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                (float)(bounds.X + bounds.Width / 2) - textVector.X / 2f,
                (float)(bounds.Y + bounds.Height / 2) - textVector.Y / 2f);

            GuiData.spriteBatch.DrawString(font, text, textPosition, textColor);
        }

        internal static void DrawFlickeringCenteredText(Rectangle bounds, string text, SpriteFont font, Color textColor = default)
        {
            textColor = textColor == default ? Color.White : textColor;
            Vector2 textVector = font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                (float)(bounds.X + bounds.Width / 2) - textVector.X / 2f,
                (float)(bounds.Y + bounds.Height / 2) - textVector.Y / 2f);

            Rectangle container = new Rectangle()
            {
                X = (int)textPosition.X,
                Y = (int)textPosition.Y,
                Width = (int)textVector.X,
                Height = (int)textVector.Y
            };

            FlickeringTextEffect.DrawFlickeringText(container, text, 5f, 0.35f, font, OS.currentInstance, textColor);
        }
    }
}
