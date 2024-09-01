using Pathfinder.Event.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;

using static HollowZero.HollowLogger;

namespace HollowZero
{
    public class HollowTimerBase
    {
        public HollowTimerBase(string id, float seconds, Action actionToRun)
        {
            ID = id;
            BaseSeconds = seconds;
            SecondsLeft = seconds;
            RunOnTimeOut = actionToRun;
        }

        public HollowTimerBase(string id, float seconds, Action actionToRun, bool repeating)
        {
            ID = id;
            BaseSeconds = seconds;
            SecondsLeft = seconds;
            RunOnTimeOut = actionToRun;
            IsRepeating = repeating;
        }

        public string ID { get; set; }
        public float SecondsLeft { get; set; }
        public Action RunOnTimeOut { get; set; }
        public bool IsRepeating { get; set; } = false;

        public bool IsActive { get; set; } = true;
        private float BaseSeconds { get; set; }

        public void Restart()
        {
            SecondsLeft = BaseSeconds;
        }

        public void ChangeSeconds(float newSeconds)
        {
            SecondsLeft = newSeconds;
            BaseSeconds = newSeconds;
        }

        public void ChangeSeconds(float newSeconds, bool alsoChangeBase)
        {
            SecondsLeft = newSeconds;
            if (alsoChangeBase) BaseSeconds = newSeconds;
        }
    }

    public class HollowTimerChangeOrder
    {
        public HollowTimerChangeOrder(string id)
        {
            TimerID = id;
        }

        public HollowTimerChangeOrder(string id, float newSeconds)
        {
            TimerID = id;
            NewSeconds = newSeconds;
        }

        public HollowTimerChangeOrder(string id, float newSeconds, bool alsoChangeBase)
        {
            TimerID = id;
            NewSeconds = newSeconds;
            ChangeBaseSeconds = alsoChangeBase;
        }

        public HollowTimerChangeOrder(string id, Action newAction)
        {
            TimerID = id;
            NewAction = newAction;
        }

        public HollowTimerChangeOrder(string id, float newSeconds, Action newAction)
        {
            TimerID = id;
            NewSeconds = newSeconds;
            NewAction = newAction;
        }

        public HollowTimerChangeOrder(string id, bool needsRemoval)
        {
            TimerID = id;
            NeedsRemoval = needsRemoval;
        }

        public HollowTimerChangeOrder(int index, bool needsRemoval)
        {
            TimerIndex = index;
            NeedsRemoval = needsRemoval;
        }

        public string TimerID;
        public int TimerIndex = -1;
        public float NewSeconds;
        public Action NewAction;
        public bool NeedsRemoval = false;
        public bool ChangeBaseSeconds = false;
    }

    public class HollowTimer
    {
        //public static readonly List<HollowTimerBase> timers = new();
        public static readonly UniqueTimersCollection timers = new();
        public static List<HollowTimerChangeOrder> changeOrders = new();
        public static readonly List<HollowTimerBase> timersQueue = new();

        private static readonly HashSet<string> knownIDs = new();

        internal static void DecreaseTimers(OSUpdateEvent updateEvent)
        {
            float seconds = (float)updateEvent.GameTime.ElapsedGameTime.TotalSeconds;

            foreach(var timer in timersQueue)
            {
                timers.AddTimer(timer);
            }
            timersQueue.Clear();

            foreach(var timer in timers)
            {
                if (!timer.IsActive) continue;
                if(timer.SecondsLeft - seconds <= 0)
                {
                    timer.RunOnTimeOut();
                    timer.IsActive = false;
                    if (!timer.IsRepeating)
                    {
                        var order = new HollowTimerChangeOrder(timer.ID, true);
                        changeOrders.Add(order);
                        continue;
                    } else
                    {
                        timer.Restart();
                        timer.IsActive = true;
                    }
                } else
                {
                    var order = new HollowTimerChangeOrder(timer.ID, timer.SecondsLeft - seconds);
                    changeOrders.Add(order);
                }
            }

            foreach(var order in changeOrders)
            {
                if(!timers.TryFind(t => t.ID == order.TimerID, out var timer))
                {
                    LogWarning($"[Timer Change Order] Couldn't find timer with ID of {order.TimerID} -- skipping.");
                    continue;
                }
                int index = timers.IndexOf(timer);

                if (order.NeedsRemoval)
                {
                    timers.Remove(timer);
                    knownIDs.Remove(timer.ID);
                    continue;
                }

                if(order.NewSeconds != default)
                {
                    timers[index].ChangeSeconds(order.NewSeconds, false);
                }

                if(order.NewAction != null)
                {
                    timers[index].RunOnTimeOut = order.NewAction;
                }
            }
            changeOrders.Clear();
        }

        public static void AddTimer(string id, float timeInSeconds, Action action)
        {
            if (!knownIDs.Add(id)) return;
            if (timers.Exists(t => t.ID == id) || timersQueue.Exists(t => t.ID == id)) return;
            var timer = new HollowTimerBase(id, timeInSeconds, action);
            timersQueue.Add(timer);
            knownIDs.Remove(id);
        }

        public static void AddTimer(string id, float timeInSeconds, Action action, bool repeat)
        {
            if (!knownIDs.Add(id)) return;
            if (timers.Exists(t => t.ID == id) || timersQueue.Exists(t => t.ID == id)) return;
            var timer = new HollowTimerBase(id, timeInSeconds, action, repeat);
            timersQueue.Add(timer);
            knownIDs.Remove(id);
        }

        public static void RemoveTimer(string id)
        {
            var order = new HollowTimerChangeOrder(id, true);
            changeOrders.Add(order);
        }

        public static void ChangeTimer(string id, float timeInSeconds = 0.0f, Action action = null)
        {
            var order = new HollowTimerChangeOrder(id, timeInSeconds, action);
            changeOrders.Add(order);
        }

        private static bool TryFindTimer(string id, out HollowTimerBase timer)
        {
            timer = null;
            if(!timers.TryFind(t => t.ID == id, out var fTimer))
            {
                return false;
            }
            timer = fTimer;
            return true;
        }

        internal static void ClearTimers()
        {
            timers.Clear();
            changeOrders.Clear();
            timersQueue.Clear();
        }
    }

    public class UniqueTimersCollection : List<HollowTimerBase>
    {
        private bool Debug => Hacknet.OS.DEBUG_COMMANDS;

        public List<HollowTimerBase> SimpleList;

        public UniqueTimersCollection()
        {
            SimpleList = this;
        }

        public void AddTimer(HollowTimerBase timer)
        {
            if(Debug)
            {
                LogDebug($"[Hollow Timer] Adding timer with ID of {timer.ID}...");
            }
            
            Add(timer);
        }

        public HollowTimerBase GetTimer(string id)
        {
            if (!this.Any(t => t.ID == id)) return null;
            return this.First(t => t.ID == id);
        }

        public void RemoveTimer(string id)
        {
            if (!this.Any(t => t.ID == id)) return;
            var timer = this.First(t => t.ID == id);
            this.Remove(timer);
        }
    }
}
