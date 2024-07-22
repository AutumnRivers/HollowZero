using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hacknet;
using HollowZero.Daemons.Event;

using Microsoft.Xna.Framework;

namespace HollowZero.Choices
{
    internal static class TestChoiceEvents
    {
        #region default choices
        public static List<ChoiceEvent> choiceEvents = new List<ChoiceEvent>()
        {
            new ChoiceEvent()
            {
                Title = "Wellness Check-Up",
                Content = "what's up. how ya feeling",
                Choices = new List<Choice>()
                {
                    new Choice()
                    {
                        Title = "Awesome",
                        Subtext = "Decrease infection level by 15",
                        Color = Color.Green,
                        OnPressed = delegate(){ RemoveInfection(15); }
                    },
                    new Choice()
                    {
                        Title = "Terrible",
                        Subtext = "Increase infection level by 15",
                        Color = Color.Red,
                        OnPressed = delegate(){ AddInfection(15); }
                    },
                    new Choice()
                    {
                        Title = "oh god oh fuck",
                        Subtext = "Add 1 random Malware",
                        Color = Color.Black,
                        OnPressed = delegate(){ ForciblyAddMalware(); }
                    }
                }
            },
            new ChoiceEvent()
            {
                Title = "Fuck You",
                Content = "die.",
                Choices = new List<Choice>()
                {
                    new Choice()
                    {
                        Title = "Can I not die actually?",
                        Subtext = "50% chance to clear infection, or +50 infection",
                        Color = Color.Goldenrod,
                        OnPressed = delegate()
                        {
                            Random rndm = new Random();
                            int n = rndm.Next(0, 100);
                            if(n >= 50)
                            {
                                ClearInfection();
                                OS.currentInstance.terminal.writeLine("okay :)");
                            } else
                            {
                                AddInfection(50);
                                OS.currentInstance.terminal.writeLine("grr you have made me ANGRIER");
                            }
                        }
                    },
                    new Choice()
                    {
                        Title = "Eh okay",
                        Subtext = "+25 infection",
                        Color = Color.Red,
                        OnPressed = delegate(){ AddInfection(25); }
                    }
                }
            }
        };
        #endregion default choices

        public static void AddInfection(int amount)
        {
            HollowZeroCore.IncreaseInfection(amount);
            OS.currentInstance.terminal.writeLine($"<< WARNING! >> Infection level increased by {amount}!");
        }

        public static void RemoveInfection(int amount)
        {
            HollowZeroCore.DecreaseInfection(amount);
            OS.currentInstance.terminal.writeLine($"<< INFO >> Infection level decreased by {amount}");
        }

        public static void ClearInfection()
        {
            HollowZeroCore.ClearInfection();
            OS.currentInstance.terminal.writeLine($"<< INFO >> Infection level cleared");
        }

        public static void ForciblyAddMalware(Malware malwareToAdd = null)
        {
            // TODO: Method for adding malware
            HollowZeroCore.InfectionLevel = 0;
            OS.currentInstance.terminal.writeLine($"<< WARNING >> Malicious program detected. Please remove ASAP.");
        }
    }
}
