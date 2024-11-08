﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Hacknet;
using Hacknet.Gui;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Pathfinder.Daemon;
using Pathfinder.GUI;

namespace HollowZero.Daemons
{
    public class HollowDaemon : BaseDaemon
    {
        public HollowDaemon(Computer computer, string serviceName, OS os) : base(computer, serviceName, os) { }

        public const int HSEP_HEIGHT = 4;

        public static bool Registerable => false;

        internal static void DrawCenteredText(Rectangle bounds, string text, SpriteFont font, int startingHeight, Color textColor = default)
        {
            textColor = textColor == default ? Color.White : textColor;
            Vector2 textVector = font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                (float)(bounds.X + bounds.Width / 2) - textVector.X / 2f,
                (float)(startingHeight) - textVector.Y / 2f);

            GuiData.spriteBatch.DrawString(font, text, textPosition, textColor);
        }

        internal static void DrawCenteredFlickeringText(Rectangle bounds, string text, SpriteFont font, int startingHeight, Color textColor = default)
        {
            textColor = textColor == default ? Color.White : textColor;
            Vector2 textVector = font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                (float)(bounds.X + bounds.Width / 2) - textVector.X / 2f,
                (float)(startingHeight) - textVector.Y / 2f);

            GuiData.spriteBatch.DrawString(font, Utils.FlipRandomChars(text, 0.017999999225139618), textPosition, textColor);
            GuiData.spriteBatch.DrawString(font, Utils.FlipRandomChars(text, 0.0040000001899898052), textPosition, textColor * 0.5f);
        }

        internal static void DrawCenteredScaleText(Rectangle bounds, string text, SpriteFont font, int startingHeight, Color textColor = default,
            float scale = 1.0f)
        {
            textColor = textColor == default ? Color.White : textColor;
            Vector2 textVector = font.MeasureString(text) * scale;
            Vector2 textPosition = new Vector2(
                (float)(bounds.X + bounds.Width / 2) - textVector.X / 2f,
                (float)(startingHeight) - textVector.Y / 2f);

            GuiData.spriteBatch.DrawString(font, text, textPosition, textColor, 0f, Vector2.Zero, scale,
                SpriteEffects.None, 0.1f);
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

        protected void DrawHorizontalSeparator(Rectangle bounds, int startingHeight)
        {
            RenderedRectangle.doRectangle(bounds.X, startingHeight - (HSEP_HEIGHT / 2), bounds.Width, HSEP_HEIGHT, Color.White);
        }

        internal static Vector2 GetStringSize(SpriteFont font, string content)
        {
            return font.MeasureString(content);
        }

        internal static int GetStringHeight(SpriteFont font, string content)
        {
            return (int)Math.Ceiling(GetStringSize(font, content).Y);
        }

        protected void RemoveDaemon()
        {
            var sysFolder = comp.getFolderPath("sys");
            comp.deleteFile("SYSTEM", "DefaultBootModule.txt", sysFolder);
            comp.getFolderFromPath("log").files.Clear();

            comp.daemons.Remove(this);
            comp.initDaemons();

            OS.currentInstance.display.command = "probe";
        }

        public static void DoMalwarePowerAction()
        {
            foreach (var malware in HollowZeroCore.CollectedMalware.Where(m => m.Trigger == Malware.MalwareTrigger.EveryAction))
            {
                malware.PowerAction.Invoke(malware.PowerLevel);
            }
        }

        internal virtual void OnDisconnect()
        {
            return;
        }
    }

    public class HollowButton
    {
        public HollowButton(int id, int x, int y, int width, int height, string text, Color color)
        {
            ButtonID = id;
            X = x; Y = y; Width = width; Height = height;
            Text = text; Color = color;
        }

        public int X; public int Y; public int Width; public int Height;
        public string Text; public Color Color;

        public bool Disabled = false;
        public string DisabledMessage = "<!> You don't have enough resources for that!";
        public int ButtonID;

        public Action OnPressed;

        public bool IsAction = false;

        public void DoButton()
        {
            if (Disabled) Color = Utils.SlightlyDarkGray;

            if(Button.doButton(ButtonID, X, Y, Width, Height, Text, Color))
            {
                if(Disabled)
                {
                    OS.currentInstance.warningFlash();
                    OS.currentInstance.terminal.writeLine(DisabledMessage);
                    return;
                }

                if(IsAction)
                {
                    HollowDaemon.DoMalwarePowerAction();
                }
                OnPressed.Invoke();
            }
        }
    }
}
