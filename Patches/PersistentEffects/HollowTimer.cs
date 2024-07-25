using Pathfinder.Event.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HollowZero
{
    internal class HollowTimer
    {
        internal static List<Tuple<string, float, Action>> timers = new List<Tuple<string, float, Action>>();

        internal static void DecreaseTimers(OSUpdateEvent updateEvent)
        {
            float seconds = (float)updateEvent.GameTime.ElapsedGameTime.TotalSeconds;

            foreach(var timer in timers)
            {
                int index = timers.IndexOf(timer);
                if(timer.Item2 - seconds <= 0)
                {
                    timer.Item3.Invoke();
                    timers.Remove(timer);
                    continue;
                } else
                {
                    Tuple<string, float, Action> replaceTuple = Tuple.Create(timer.Item1, timer.Item2 - seconds, timer.Item3);
                    timers[index] = replaceTuple;
                    continue;
                }
            }
        }
    }
}
