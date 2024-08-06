using Pathfinder.Event.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public static readonly Dictionary<string, float> RepeatTimers = new Dictionary<string, float>();

        internal static void DecreaseTimers(OSUpdateEvent updateEvent)
        {
            float seconds = (float)updateEvent.GameTime.ElapsedGameTime.TotalSeconds;

            foreach(var timer in timers)
            {
                int index = timers.IndexOf(timer);
                if(timer.Item2 - seconds <= 0)
                {
                    timer.Item3.Invoke();

                    if(RepeatTimers.ContainsKey(timer.Item1))
                    {
                        var newTimer = Tuple.Create(timer.Item1, RepeatTimers[timer.Item1], timer.Item3);
                        timerChangeQueue.Add(timer, newTimer);
                    } else
                    {
                        Console.WriteLine($"Removing timer {timer.Item1}...");
                        timerRemovalQueue.Add(timer);
                    }
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

        public static void AddTimer(string id, float timeInSeconds, Action action, bool repeat)
        {
            AddTimer(id, timeInSeconds, action);
            if (!repeat) return;
            if (RepeatTimers.ContainsKey(id)) return;
            RepeatTimers.Add(id, timeInSeconds);
        }

        public static void RemoveTimer(string id)
        {
            if(TryFindTimer(id, out var timer))
            {
                timerRemovalQueue.Add(timer);
                if(RepeatTimers.ContainsKey(id))
                {
                    RepeatTimers.Remove(id);
                }
            }
        }

        public static void ChangeTimer(string id, float timeInSeconds = 0.0f, Action action = null)
        {
            if(TryFindTimer(id, out var timer))
            {
                float time = timeInSeconds == 0.0f ? timer.Item2 : timeInSeconds;
                Action a = action ?? timer.Item3;

                var newTimer = Tuple.Create(id, time, a);
                timerChangeQueue.Add(timer, newTimer);
            }
        }

        private static bool TryFindTimer(string id, out Tuple<string, float, Action> timer)
        {
            timer = null;

            var fTimer = timers.FirstOrDefault(t => t.Item1 == id);
            if (fTimer == null) return false;

            timer = fTimer;
            return true;
        }
    }
}
