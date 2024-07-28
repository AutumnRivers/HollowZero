using System;

using Hacknet;
using Hacknet.Gui;

using HarmonyLib;

using HollowZero.Daemons;

using Microsoft.Xna.Framework;

namespace HollowZero
{
    [HarmonyPatch]
    public class CustomEffects
    {
        internal static float RectOpacity = 100.0f;
        internal static float UserSoundVolume = 100.0f;

        public static bool EffectsActive = false;
        public static Action CurrentEffect;
        public static int CurrentStage = 0;

        public static bool EffectFinished = false;

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(OS),nameof(OS.drawScanlines))]
        internal static void EffectsPatch()
        {
            if (!EffectsActive) return;

            CurrentEffect.Invoke();
        }

        public static void ResetEffect()
        {
            EffectsActive = false;
            CurrentEffect = null;
            CurrentStage = 0;
            RectOpacity = 100.0f;
            EffectFinished = true;
        }
    }

    [HarmonyPatch]
    public class MalwareOverlay
    {
        public static Malware CurrentMalware;

        private const string HEADER_TEXT = "GAINED MALWARE";
        private const float TARGET_BACKGROUND_OPACITY = 50.0f;

        private const float BACKGROUND_FADE_TIME = 2.0f;
        private const float TEXT_FADE_TIME = 3.0f;

        private static float BackgroundOpacity = 0.0f;
        private static float PopupRectWidth = 0.0f;
        private static float TextOpacity = 0.0f;
        private static Rectangle PlayerBounds => GuiData.spriteBatch.GraphicsDevice.Viewport.Bounds;
        private static int PopupHeight => PlayerBounds.Height / 2;

        private static Rectangle PopupBounds;
        private static bool BeginFade = false;
        private static Color TextColor = new Color(247, 132, 124);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(OS),nameof(OS.drawScanlines))]
        internal static void MalwareOverlayPatch(OS __instance)
        {
            if (CurrentMalware == null) return;
            var gt = __instance.lastGameTime.ElapsedGameTime.TotalSeconds;

            GuiData.blockingInput = true;

            PopupBounds = new Rectangle(PlayerBounds.X, PlayerBounds.Center.Y - (PlayerBounds.Center.Y / 2),
                (int)PopupRectWidth, PopupHeight);

            DrawBackground();
            DrawPopupModule();
            DrawPopupText(HEADER_TEXT, CurrentMalware.DisplayName, CurrentMalware.Description);

            // Stage 1 - Darken Background
            if(BackgroundOpacity < TARGET_BACKGROUND_OPACITY && !BeginFade)
            {
                BackgroundOpacity += (float)gt * (TARGET_BACKGROUND_OPACITY / BACKGROUND_FADE_TIME);
                return;
            }

            // Stage 2 - Bring in Rectangle
            if(PopupRectWidth < (PlayerBounds.Width / 2f) && !BeginFade)
            {
                PopupRectWidth += (float)gt * (PlayerBounds.Width / 2f);
                return;
            } else if(PopupRectWidth < PlayerBounds.Width && !BeginFade)
            {
                PopupRectWidth += (float)gt * (PlayerBounds.Width / 2f);
            }

            // Stage 3 - Fade in text
            if(TextOpacity < 100.0f && !BeginFade)
            {
                TextOpacity += (float)gt * (100.0f / TEXT_FADE_TIME);
                return;
            }

            // Final Stage - Fade away
            Action fadeAction = delegate ()
            {
                if (CurrentMalware == null) return;
                BeginFade = true;   
            };
            HollowTimer.AddTimer("malware_popup_fade", 3.5f, fadeAction);
            if (!BeginFade) return;

            if (TextOpacity > 0f)
            {
                TextOpacity -= (float)gt * (100.0f / TEXT_FADE_TIME);
            }
            if (TextOpacity > 65f) return; 

            if (PopupRectWidth > 0f)
            {
                PopupRectWidth -= (float)gt * (PlayerBounds.Width / 2f);
            }

            if (BackgroundOpacity > 0f)
            {
                BackgroundOpacity -= (float)gt * (TARGET_BACKGROUND_OPACITY / BACKGROUND_FADE_TIME);
                return;
            }

            GuiData.blockingInput = false;
            ResetPopup();
        }

        private static void ResetPopup()
        {
            CurrentMalware = null;
            BackgroundOpacity = 0.0f;
            TextOpacity = 0.0f;
            PopupRectWidth = 0.0f;
            BeginFade = false;
        }

        private static void DrawBackground()
        {
            RenderedRectangle.doRectangle(PlayerBounds.X, PlayerBounds.Y, PlayerBounds.Width, PlayerBounds.Height,
                Color.Black * (BackgroundOpacity / 100f));
        }

        private static void DrawPopupModule()
        {
            RenderedRectangle.doRectangle(PopupBounds.X, PopupBounds.Y, PopupBounds.Width, PopupBounds.Height, Color.Black);
            RenderedRectangle.doRectangleOutline(PlayerBounds.X - 5, PopupBounds.Y, PlayerBounds.Width * 2, PopupBounds.Height,
                1, Color.Red * ((float)PopupBounds.Width / (float)PlayerBounds.Width));
        }

        private static void DrawPopupText(string header, string subheader, string context)
        {
            if (TextOpacity <= 1f) return;
            float yOffset = (PlayerBounds.Center.Y - (PopupBounds.Height / 2f)) + (PopupBounds.Height * (1f / 4f));
            float opacity = TextOpacity / 100f;
            HollowDaemon.DrawCenteredScaleText(PlayerBounds, header, GuiData.font, (int)yOffset, TextColor * opacity, 2.5f);

            RenderedRectangle.doRectangle(PlayerBounds.X + 30, PopupBounds.Center.Y - 1, PlayerBounds.Width - 60, 3, Color.Red * opacity);

            yOffset = PlayerBounds.Center.Y + (PopupBounds.Height * (1f / 6f));
            HollowDaemon.DrawCenteredScaleText(PlayerBounds, subheader, GuiData.font, (int)yOffset, TextColor * opacity, 1.5f);
            yOffset = PlayerBounds.Center.Y + (PopupBounds.Height * (2f / 6f));
            context = Utils.SuperSmartTwimForWidth(context, (int)(PlayerBounds.Width / 1.2f), GuiData.smallfont);
            HollowDaemon.DrawCenteredScaleText(PlayerBounds, context, GuiData.smallfont, (int)yOffset, TextColor * opacity, 1.2f);
        }
    }
}
