﻿
using CatLib.API;
using CatLib.API.Thread;
using System.Collections.Generic;
using System.Threading;

namespace CatLib.Thread
{

    /// <summary>
    /// 多线程运行器
    /// </summary>
    public class ThreadRuner : Component , IThread , IUpdate
    {

        private List<TaskRunner> taskRunner = new List<TaskRunner>();
        private ReaderWriterLockSlim taskRunnerLocker = new ReaderWriterLockSlim();

        public ITask Task(System.Action task)
        {
            var taskRunner = new TaskRunner(this)
            {
                Task = task,
                ReturnResult = false,
            };
            return taskRunner;
        }

        public ITask Task(System.Func<object> task)
        {

            var taskRunner = new TaskRunner(this)
            {
                TaskWithResult = task,
                ReturnResult = true,
            };

            return taskRunner;

        }

        public void AddTask(TaskRunner taskRunner)
        {
            taskRunner.StartTime = App.Time.Time;
            if (taskRunner.DelayTime > 0)
            {
                taskRunnerLocker.EnterWriteLock();
                try
                {
                    this.taskRunner.Add(taskRunner);
                }
                finally
                {
                    taskRunnerLocker.ExitWriteLock();
                }
            }
            else
            {
                ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadExecuter), taskRunner);
            }
        }

        public void Update()
        {
            taskRunnerLocker.EnterReadLock();
            var handlersToRemove = new bool[taskRunner.Count];

            try
            {
                for (var i = 0; i < taskRunner.Count; ++i)
                {
                    var runner = taskRunner[i];
                    if ((runner.StartTime + runner.DelayTime) <= App.Time.Time)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadExecuter), runner);
                        handlersToRemove[i] = true;
                    }
                }
            }
            finally { taskRunnerLocker.ExitReadLock(); }

            taskRunnerLocker.EnterWriteLock();
            try
            {
                for (var i = handlersToRemove.Length - 1; i > -1; --i)
                {
                    if (handlersToRemove[i])
                    {
                        taskRunner.RemoveAt(i);
                    }
                }
            }
            finally { taskRunnerLocker.ExitWriteLock(); }
        }

        private void ThreadExecuter(object state)
        {
            try
            {
                if (typeof(TaskRunner) == state.GetType()) { RunTaskThread((TaskRunner)state); }
                else
                {
                    App.Trigger(ThreadEvents.ON_THREAD_EXECURE_ERROR, 
                                                new ErrorEventArgs(
                                                    new System.Exception(string.Format("type '{0}' not supported!", state.GetType())
                                                )));
                }
            }
            catch (System.Exception exception)
            {
                App.Trigger(ThreadEvents.ON_THREAD_EXECURE_ERROR, new ErrorEventArgs(exception));
            }
        }

        private void RunTaskThread(TaskRunner taskRunner)
        {
            try
            {
                object result = null;
                if (taskRunner.ReturnResult)
                {
                    result = taskRunner.TaskWithResult();
                }
                else
                {
                    taskRunner.Task();
                }

                if (taskRunner.Complete != null)
                {
                    App.MainThread(() =>
                    {
                        taskRunner.Complete();
                    });
                }

                if (taskRunner.CompleteWithResult != null)
                {
                    App.MainThread(() =>
                    {
                        taskRunner.CompleteWithResult(result);
                    });
                }

            }
            catch (System.Exception exception)
            {
                if (taskRunner.Error != null)
                {
                    App.MainThread(() =>
                    {
                        taskRunner.Error(exception);
                    });
                }
            }
        }

    }


}