using Pathfinder.Event.Gameplay;
using System;
using System.Collections.Generic;

namespace HollowZero
{
    internal class HollowTimer
    {
        internal static List<Tuple<string, float, Action>> timers = new List<Tuple<string, float, Action>>();
        private static List<Tuple<string, float, Action>> timerRemovalQueue = new List<Tuple<string, float, Action>>();
        private static List<Tuple<string, float, Action>> timerAdditionQueue = new List<Tuple<string, float, Action>>();
        private static Dictionary<Tuple<string, float, Action>, Tuple<string, float, Action>> timerChangeQueue =
            new Dictionary<Tuple<string, float, Action>, Tuple<string, float, Action>>();

        public static readonly List<string> ActiveTimerIDs = new List<string>();

        internal static void DecreaseTimers(OSUpdateEvent updateEvent)
        {
            float seconds = (float)updateEvent.GameTime.ElapsedGameTime.TotalSeconds;

            foreach(var timer in timers)
            {
                int index = timers.IndexOf(timer);
                if(timer.Item2 - seconds <= 0)
                {
                    timer.Item3.Invoke();
                    Console.WriteLine($"Removing timer {timer.Item1}...");
                    timerRemovalQueue.Add(timer);
                    continue;
                } else
                {
                    Tuple<string, float, Action> replaceTuple = Tuple.Create(timer.Item1, timer.Item2 - seconds, timer.Item3);
                    timerChangeQueue.Add(timer, replaceTuple);
                    continue;
                }
            }

            List<Tuple<string, float, Action>> duplicateTimers = new List<Tuple<string, float, Action>>();
            foreach(var timer in timers)
            {
                var amountOfIdenticalTimers = timers.FindAll(t => t.Item1 == timer.Item1).Count;
                var amountOfIdenticalTimersFound = duplicateTimers.FindAll(t => t.Item1 == timer.Item1).Count;
                if(amountOfIdenticalTimers > amountOfIdenticalTimersFound + 1)
                {
                    duplicateTimers.Add(timer);
                }
            }

            foreach(var timer in duplicateTimers)
            {
                timerRemovalQueue.Add(timer);
            }

            foreach(var timer in timerRemovalQueue)
            {
                timers.Remove(timer);
                ActiveTimerIDs.Remove(timer.Item1);
            }

            foreach(var timer in timerAdditionQueue)
            {
                timers.Add(timer);
            }

            foreach(var change in timerChangeQueue)
            {
                int index = timers.IndexOf(change.Key);
                timers[index] = change.Value;
            }

            timerRemovalQueue.Clear();
            timerAdditionQueue.Clear();
            timerChangeQueue.Clear();
        }

        public static void AddTimer(string id, float timeInSeconds, Action action)
        {
            if (ActiveTimerIDs.Contains(id)) return;
            ActiveTimerIDs.Add(id);
            timerAdditionQueue.Add(Tuple.Create(id, timeInSeconds, action));
        }
    }
}
