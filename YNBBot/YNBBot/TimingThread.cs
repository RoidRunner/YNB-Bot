using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YNBBot
{
    /// <summary>
    /// Handles the separate thread used for calling scheduled methods 
    /// </summary>
    static class TimingThread
    {
        #region Fields, Properties

        /// <summary>
        /// The thread the timing happens in
        /// </summary>
        private static Thread timingThread;
        /// <summary>
        /// Timer used to measure time to allow accurate scheduling
        /// </summary>
        private static Stopwatch timer;
        /// <summary>
        /// Shortcut for accessing the timers milliseconds elapsed
        /// </summary>
        public static long Millis
        {
            get
            {
                return timer.ElapsedMilliseconds;
            }
        }

        /// <summary>
        /// contains ScheduledCallback structs that will be executed when their time has been reached.
        /// </summary>
        private static List<ScheduledCallback> scheduledCallbacks;
        public static IReadOnlyList<ScheduledCallback> ScheduledCallbacks
        {
            get
            {
                lock (scheduledCallbackListLock)
                {
                    return scheduledCallbacks.AsReadOnly();
                }
            }
        }

        /// <summary>
        /// Since new ScheduledCallbacks can not be added while the callbacks are checked for time they are added from this point
        /// </summary>
        private static List<ScheduledCallback> newScheduledCallbacks;
        /// <summary>
        /// The lock object used for newScheduledCallbacks
        /// </summary>
        private static readonly object newCallbacksLock = new object();
        private static readonly object scheduledCallbackListLock = new object();

        /// <summary>
        /// initiates variables and starts the timer thread
        /// </summary>
        static TimingThread()
        {
            scheduledCallbacks = new List<ScheduledCallback>();

            timer = new Stopwatch();
            timer.Start();
            timingThread = new Thread(new ThreadStart(Run));
            timingThread.Start();
        }

        #endregion
        #region Run

        /// <summary>
        /// The method running the timer thread
        /// </summary>
        public static async void Run()
        {
            while (Var.running)
            {
                List<ScheduledCallback> markedForRemoval = new List<ScheduledCallback>();
                foreach (ScheduledCallback schedule in scheduledCallbacks)
                {
                    if (Millis >= schedule.executeAt && schedule.callback != null)
                    {
                        await SettingsModel.SendDebugMessage("Firing Callback: " + schedule.callback.Method.ToString(), DebugCategories.timing);
                        await schedule.callback();
                        markedForRemoval.Add(schedule);
                    }
                }
                lock (scheduledCallbackListLock)
                {
                    if (markedForRemoval.Count > 0)
                    {
                        foreach (var complete in markedForRemoval)
                        {
                            scheduledCallbacks.Remove(complete);
                        }
                    }
                    lock (newCallbacksLock)
                    {
                        if (newScheduledCallbacks != null)
                        {
                            scheduledCallbacks.AddRange(newScheduledCallbacks);
                            newScheduledCallbacks = null;
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }

        #endregion
        #region Schedule Delegates

        /// <summary>
        /// Schedules a delegate to be fired after specified delay
        /// </summary>
        /// <param name="call">The delegate to call upon the delay is over</param>
        /// <param name="delay">The delay in milliseconds</param>
        public static void AddScheduleDelegate(SimpleDelegate call, long delay)
        {
            lock (newCallbacksLock)
            {
                if (newScheduledCallbacks == null)
                {
                    newScheduledCallbacks = new List<ScheduledCallback>();
                }
                newScheduledCallbacks.Add(new ScheduledCallback { callback = call, executeAt = Millis + delay});
            }
        }

        /// <summary>
        /// Updates the clients activity to the current UTC time and schedules a new update for when the next minute is reached.
        /// </summary>
        /// <returns></returns>
        public static async Task UpdateTimeActivity()
        {
            TimeActivity activity = new TimeActivity();
            DateTime now = DateTime.UtcNow;
            activity.Time = string.Format("UTC {0}-{1}-{2} {3}:{4}",
                now.Year, now.Month.ToString().PadLeft(2, '0'), now.Day.ToString().PadLeft(2, '0'),
                now.Hour.ToString().PadLeft(2, '0'), now.Minute.ToString().PadLeft(2, '0'));
            await Var.client.SetActivityAsync(activity);
            await SettingsModel.SendDebugMessage("Updated Time Activity to " + activity.Time + "!", DebugCategories.timing);
            
            AddScheduleDelegate(UpdateTimeActivity, (61 - now.Second) * 1000);
        }

        #endregion
    }

    /// <summary>
    /// Delegate pattern used for a scheduled Callback
    /// </summary>
    /// <returns></returns>
    public delegate Task SimpleDelegate();

    /// <summary>
    /// Stores a callback and timing information
    /// </summary>
    struct ScheduledCallback
    {
        public SimpleDelegate callback;
        public long executeAt;
    }

    /// <summary>
    /// Container for updating the discord bots activity
    /// </summary>
    class TimeActivity : IActivity
    {
        public string Time;
        public string Name {
            get
            {
                return Time;
            }
        }

        public ActivityType Type
        {
            get
            {
                return ActivityType.Playing;
            }
        }
    }
}
