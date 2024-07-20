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
    internal static class DefaultChoiceEvents
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
            }
        };
        #endregion default choices

        public static void AddInfection(int amount)
        {
            HollowZeroCore.InfectionLevel += amount;
            OS.currentInstance.terminal.writeLine($"<< WARNING! >> Infection level increased by {amount}!");
        }

        public static void RemoveInfection(int amount)
        {
            HollowZeroCore.InfectionLevel -= amount;
            OS.currentInstance.terminal.writeLine($"<< INFO >> Infection level decreased by {amount}");
        }

        public static void ClearInfection()
        {
            HollowZeroCore.InfectionLevel = 0;
            OS.currentInstance.terminal.writeLine($"<< INFO >> Infection level cleared");
        }

        public static void ForciblyAddMalware(Malware malwareToAdd = null)
        {
            HollowZeroCore.InfectionLevel = 0;
            OS.currentInstance.terminal.writeLine($"<< WARNING >> Malicious program detected. Please remove ASAP.");
        }
    }
}
