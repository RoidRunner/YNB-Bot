using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YNBBot.MultiThreading
{
    internal delegate Task SimpleDelegate();

    /// <summary>
    /// Gives access to workerthreads that allow completing large workloads split over multiple threads
    /// </summary>
    internal static class WorkerThreadService
    {
        /// <summary>
        /// The amount of worker threads. Is logical processor count
        /// </summary>
        internal readonly static int WORKERTHREADCOUNT;

        private static WorkerThreadContainer[] threads;
        private static Queue<WorkerTask> taskQueue;
        private static readonly object taskQueueLock;

        #region init & dispose

        /// <summary>
        /// Initializes Workerthreads & TaskQueue
        /// </summary>
        static WorkerThreadService()
        {
            WORKERTHREADCOUNT = Environment.ProcessorCount;
            if (WORKERTHREADCOUNT < 2)
            {
                WORKERTHREADCOUNT = 2;
            }
            threads = new WorkerThreadContainer[WORKERTHREADCOUNT];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new WorkerThreadContainer("WorkerThread #" + i);
            }
            taskQueueLock = new object();
            taskQueue = new Queue<WorkerTask>();
        }

        /// <summary>
        /// Can be used to nudge the static class to run its constructor
        /// </summary>
        internal static void Init()
        {

        }

        /// <summary>
        /// Clears the TaskQueue and stops all WorkerThreads
        /// </summary>
        internal static void Dispose()
        {
            lock (taskQueueLock)
            {
                taskQueue.Clear();
            }
            if (threads != null)
            {
                foreach (WorkerThreadContainer thread in threads)
                {
                    thread?.Abort();
                }
                threads = null;
            }
        }

        #endregion
        #region taskqueueing

        /// <summary>
        /// Retrieve the number of threads that are working on a task right now
        /// </summary>
        internal static int WorkingThreads
        {
            get
            {
                int workingThreads = 0;
                foreach (WorkerThreadContainer thread in threads)
                {
                    if (thread.State == WorkerThreadContainer.ThreadState.Working)
                    {
                        workingThreads++;
                    }
                }
                return workingThreads;
            }
        }

        /// <summary>
        /// Add a workertask to the worker queue. Make sure your workertask ist threadsafe!
        /// </summary>
        /// <param name="task">The Workertask object containing all information to perform your task</param>
        internal static void QueueTask(WorkerTask task)
        {
            lock (taskQueueLock)
            {
                taskQueue.Enqueue(task);
            }
        }

        /// <summary>
        /// Are any Tasks queued?
        /// </summary>
        internal static bool HasQueuedTask
        {
            get
            {
                return taskQueue.Count > 0;
            }
        }

        /// <summary>
        /// Removes one Task from the TaskQueue
        /// </summary>
        /// <returns>the Task removed from the TaskQueue</returns>
        internal static WorkerTask DeQueue()
        {
            lock (taskQueueLock)
            {
                if (HasQueuedTask)
                {
                    return taskQueue.Dequeue();
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Contains all variables & logic a single WorkerThread needs to function
    /// </summary>
    internal class WorkerThreadContainer
    {
        /// <summary>
        /// An Identifier string assigned to the Thread upon construction
        /// </summary>
        internal string ThreadIdentifier { get; private set; }
        /// <summary>
        /// The current threads state
        /// </summary>
        internal ThreadState State { get; private set; }

        private Thread thread;
        private WorkerTask Task;
        private bool closeThreadAfterWork = false;

        /// <summary>
        /// Creates a new WorkerThreadContainer (includes own Thread)
        /// </summary>
        /// <param name="identifier">String to represent the thread in debug logging</param>
        internal WorkerThreadContainer(string identifier)
        {
            thread = new Thread(new ThreadStart(Run));
            thread.Start();
            ThreadIdentifier = identifier;
            State = ThreadState.Idle;
        }

        private async void Run()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            while (!closeThreadAfterWork)
            {
                if (Task != null)
                {
                    if (Task.TaskState == WorkerTaskState.Ready)
                    {
                        State = ThreadState.Working;
                        try
                        {
                            await Task.Run();
                        }
                        catch (Exception e)
                        {
                            await GuildChannelHelper.SendExceptionNotification(e, "A WorkerTask has crashed");
                            Task = null;
                        }
                        State = ThreadState.Idle;
                    } else
                    {
                        Task = null;
                    }
                } else if (WorkerThreadService.HasQueuedTask)
                {
                    Task = WorkerThreadService.DeQueue();
                } else
                {
                    Thread.Sleep(1);
                }
            }
        }

        /// <summary>
        /// Aborts the current task and closes the thread
        /// </summary>
        internal void Abort()
        {
            Task?.Abort();
            closeThreadAfterWork = true;
        }

        /// <summary>
        /// Completes the current task and closes the thread
        /// </summary>
        internal void Finalise()
        {
            closeThreadAfterWork = true;
        }

        internal enum ThreadState
        {
            /// <summary>
            /// When Idle the WorkerThread waits for a new WorkerTask to be queued to the WorkerThreadService
            /// </summary>
            Idle,
            /// <summary>
            /// When working the WorkerThread is actively working on a WorkerTask
            /// </summary>
            Working
        }
    }

    /// <summary>
    /// Represents a single Task to be performed by a WorkerThread
    /// </summary>
    internal class WorkerTask
    {
        /// <summary>
        /// Represents the State the current Task is in.
        /// </summary>
        internal WorkerTaskState TaskState { get { return taskState; } }
        /// <summary>
        /// Represents the Progress as a float value set by the Task delegate
        /// </summary>
        internal float Progress { get { return progress; } }

        private WorkerTaskState taskState;
        private float progress;
        private readonly SimpleDelegate OnTaskBegin;
        private readonly SimpleDelegate Task;
        private readonly SimpleDelegate OnTaskEnd;
        private readonly SimpleDelegate OnTaskAbort;

        private bool abort = false;

        #region run

        /// <summary>
        /// Aborts the current Task by flipping the "abort" bool given to the Task delegate
        /// </summary>
        internal void Abort()
        {
            if (taskState == WorkerTaskState.InProgress)
            {
                taskState = WorkerTaskState.Aborted;
                abort = true;
            }
        }

        /// <summary>
        /// Handles execution of all set delegates
        /// </summary>
        internal async Task Run()
        {
            if (taskState == WorkerTaskState.Ready)
            {
                await begin();
            }

            if (taskState == WorkerTaskState.InProgress)
            {
                await doTask();
            }

            if (taskState == WorkerTaskState.Aborted)
            {
                await doAbort();
            } else
            {
                await end();
            }
        }

        private Task begin()
        {
            taskState = WorkerTaskState.InProgress;
            if (OnTaskBegin != null)
            {
                return OnTaskBegin.Invoke();
            }
            else
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }

        private Task doTask()
        {
            if (Task != null)
            {
                return Task.Invoke();
            }
            else
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }

        private Task end()
        {
            if (OnTaskEnd != null)
            {
                return OnTaskEnd.Invoke();
            }
            else
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }

        private Task doAbort()
        {
            if (OnTaskAbort != null)
            {
                return OnTaskAbort.Invoke();
            }
            else
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }
        }

        #endregion
        #region constructors

        /// <summary>
        /// Constructs a new WorkerTask
        /// </summary>
        /// <param name="task">The AbortableTask delegate that contains the tasks workload. 
        /// Make sure it is Threadsafe and implements abort in some way so stopping the process somewhere through the workload is possible. 
        /// Also be sure to update the taskState when your work is done</param>
        /// <param name="onTaskBegin">Delegate called once before execution of task begins</param>
        /// <param name="onTaskEnd">Delegate called once after and if the workload completes nominally</param>
        /// <param name="onTaskAbort">Delegate called once if execution of the task was aborted</param>
        internal WorkerTask(SimpleDelegate task, SimpleDelegate onTaskBegin = null, SimpleDelegate onTaskEnd = null, SimpleDelegate onTaskAbort = null)
        {
            taskState = WorkerTaskState.Ready;
            progress = 0;
            Task = task;
            OnTaskBegin = onTaskBegin;
            OnTaskEnd = onTaskEnd;
            OnTaskAbort = onTaskAbort;
        }

        #endregion
    }

    internal delegate Task AbortableTask(ref bool abort, ref float progress, ref WorkerTaskState taskState);

    enum WorkerTaskState
    {
        /// <summary>
        /// The task has not been worked on yet and is ready to be executed
        /// </summary>
        Ready,
        /// <summary>
        /// The task is right now performing the main workload
        /// </summary>
        InProgress,
        /// <summary>
        /// The task has been aborted
        /// </summary>
        Aborted,
        /// <summary>
        /// The task has completed nominally
        /// </summary>
        Done
    }
}
