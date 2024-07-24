using System;
using System.Reflection;
using System.Text;

using Hacknet;
using Hacknet.Gui;
using Hacknet.UIUtils;

using HarmonyLib;

using MonoMod.Cil;
using Mono.Cecil.Cil;

using Pathfinder.GUI;

using Microsoft.Xna.Framework;

using HollowZero.Daemons;

namespace HollowZero.Patches
{
    [HarmonyPatch]
    public class InformAboutPacks
    {
        /*[HarmonyILManipulator]
        [HarmonyPatch(typeof(SavefileLoginScreen),"Draw")]
        public static void ShowWarningForHollowPacks(ILContext il, SavefileLoginScreen __instance, MethodBase original)
        {
            ILCursor c = new ILCursor(il);

            Action action = () =>
            {
                
            };

            c.GotoNext(MoveType.After,
                abc => abc.MatchNop(),
                abc => abc.MatchLdarg(0),
                abc => abc.MatchLdfld(out var _),
                abc => abc.MatchLdcI4(0),
                abc => abc.MatchCallvirt(out var _),
                abc => abc.MatchStloc(18),
                abc => abc.MatchLdarg(0),
                abc => abc.MatchLdfld(out var _),
                abc => abc.MatchLdcI4(1),
                abc => abc.MatchCallvirt(out var _),
                abc => abc.MatchStloc(19),
                abc => abc.MatchLdcI4(0),
                abc => abc.MatchStsfld(out var _),
                abc => abc.MatchLdarg(0),
                abc => abc.MatchLdfld(out var _)
                );
            c.GotoPrev().GotoPrev().Remove();
            c.Emit(OpCodes.Ldloc_S, 18);
            c.Emit(OpCodes.Ldloc_S, 19);
            c.EmitDelegate(action);
        }*/

        private static Action<string,string> PatchedStartNewGameAction;
        private static bool needsConfirmation = false;

        private static int ConfirmID = PFButton.GetNextID();
        private static int CancelID = PFButton.GetNextID();

        private static readonly Rectangle screenBounds = new Rectangle()
        {
            X = GuiData.spriteBatch.GraphicsDevice.Viewport.X,
            Y = GuiData.spriteBatch.GraphicsDevice.Viewport.Y,
            Width = GuiData.spriteBatch.GraphicsDevice.Viewport.Width,
            Height = GuiData.spriteBatch.GraphicsDevice.Viewport.Height
        };

        private enum InformStates
        {
            Informing, Success, PackError, Denied
        }

        private static InformStates CurrentState = InformStates.Informing;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SavefileLoginScreen),"Draw")]
        public static bool InformAboutHollowPacksPatch(SavefileLoginScreen __instance)
        {
            if(PatchedStartNewGameAction == null)
            {
                SetNewGameAction(__instance.StartNewGameForUsernameAndPass);
                __instance.StartNewGameForUsernameAndPass = PatchedStartNewGameAction;
            }

            if(needsConfirmation)
            {
                if(CurrentState == InformStates.Informing)
                {
                    int yOffset = 275;
                    TextItem.doLabel(new Vector2(
                        screenBounds.X + (screenBounds.Width / 10),
                        screenBounds.Y + yOffset), "Hollow Packs - Warning",
                        new Color(252, 144, 144));
                    yOffset += HollowDaemon.GetStringHeight(GuiData.font, "Hollow Packs - Warning");
                    yOffset += 10;

                    StringBuilder packsList = new StringBuilder();
                    foreach (var pack in HollowZeroCore.knownPacks)
                    {
                        packsList.Append($"* \"{pack.Key}\" by {pack.Value}\n");
                    }
                    string warningContent = "Hollow Packs can run arbitrary code on your system. You are attempting to use the following packs:\n" +
                        packsList.ToString() +
                        "\nIf you trust the authors of these packs, then you can safely ignore this message. Are you sure you want to load these packs?";
                    TextItem.doSmallLabel(new Vector2(
                        screenBounds.X + (screenBounds.Width / 10),
                        screenBounds.Y + yOffset),
                        warningContent,
                        Color.White);
                    yOffset += HollowDaemon.GetStringHeight(GuiData.smallfont, warningContent);
                    yOffset += 50;

                    if (Button.doButton(ConfirmID, screenBounds.X + (screenBounds.Width / 10), screenBounds.Y + yOffset,
                        screenBounds.Width / 5, 50, "I trust these packs.", Color.Green))
                    {
                        if(HollowZeroCore.RegisterHollowPacks())
                        {
                            CurrentState = InformStates.Success;
                        } else
                        {
                            CurrentState = InformStates.PackError;
                        }
                    }
                    yOffset += 60;
                    if(Button.doButton(CancelID, screenBounds.X + (screenBounds.Width / 10), screenBounds.Y + yOffset,
                        screenBounds.Width / 5, 35, "I don't trust them!", Color.Red))
                    {
                        __instance.RequestGoBack();
                    }
                } else if(CurrentState == InformStates.Success)
                {
                    needsConfirmation = false;
                    __instance.StartNewGameForUsernameAndPass = HollowGlobalManager.StartNewGameAction;
                    __instance.StartNewGameForUsernameAndPass(__instance.Answers[0], __instance.Answers[1]);
                } else if(CurrentState == InformStates.PackError)
                {
                    int yOffset = 275;
                    TextItem.doLabel(new Vector2(
                        screenBounds.X + (screenBounds.Width / 10),
                        screenBounds.Y + yOffset), "Hollow Packs - Error",
                        new Color(252, 144, 144));
                    yOffset += HollowDaemon.GetStringHeight(GuiData.font, "Hollow Packs - Error");
                    yOffset += 10;

                    string errorContent = "Unfortunately, one or more packs could not be loaded.\n\n" +
                        "You can continue on without the unloaded packs, or go back to the main menu.";
                    TextItem.doSmallLabel(new Vector2(
                        screenBounds.X + (screenBounds.Width / 10),
                        screenBounds.Y + yOffset),
                        errorContent,
                        Color.White);
                    yOffset += HollowDaemon.GetStringHeight(GuiData.smallfont, errorContent);
                    yOffset += 50;

                    if (Button.doButton(ConfirmID, screenBounds.X + (screenBounds.Width / 10), screenBounds.Y + yOffset,
                        screenBounds.Width / 5, 50, "Start the extension", Color.Green))
                    {
                        CurrentState = InformStates.Success;
                    }
                    yOffset += 60;
                    if (Button.doButton(CancelID, screenBounds.X + (screenBounds.Width / 10), screenBounds.Y + yOffset,
                        screenBounds.Width / 5, 35, "Go back", Color.Red))
                    {
                        __instance.RequestGoBack();
                    }
                }

                return false;
            }

            return true;
        }

        private static void SetNewGameAction(Action<string,string> originalAction)
        {
            HollowGlobalManager.StartNewGameAction = originalAction;
            PatchedStartNewGameAction = delegate (string a, string b)
            {
                needsConfirmation = true;
            };
        }
    }
}
