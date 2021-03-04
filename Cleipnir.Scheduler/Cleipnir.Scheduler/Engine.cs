using System;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;

namespace Cleipnir.ExecutionEngine
{
    public class Engine : IDisposable
    {
        internal IScheduler Scheduler { get; set; }
        internal static ThreadLocal<Engine> _current { get; } = new ThreadLocal<Engine>();
        public static Engine Current => _current.Value;

        public Task Schedule(Action toExecute)
        {
            var tcs = new TaskCompletionSource<bool>();

            Scheduler.FireAndForget(() =>
            {
                try
                {
                    toExecute();
                    Task.Run(() => tcs.SetResult(true));
                }
                catch (Exception e)
                {
                    Task.Run(() => tcs.SetException(e));
                }
            });

            return tcs.Task;
        }

        public Task<T> Schedule<T>(Func<T> toExecute)
        {
            var tcs = new TaskCompletionSource<T>();
            Schedule(() =>
            {
                try
                {
                    var result = toExecute();
                    Task.Run(() => tcs.SetResult(result));
                }
                catch (Exception e)
                {
                    Task.Run(() => tcs.SetException(e));
                }
            });

            return tcs.Task;
        }

        public Task ScheduleTask(Func<CTask> toExecute)
        {
            var tcs = new TaskCompletionSource();
            Schedule(
                () =>
                {
                    try
                    {
                        var task = toExecute();
                        var awaiter = task.Awaitable.GetEphemeralAwaiter();
                        awaiter.OnCompleted(
                            () =>
                            {
                                try
                                {
                                    awaiter.GetResult();
                                    Task.Run(() => tcs.SetResult());
                                }
                                catch (Exception e)
                                {
                                    Task.Run(() => tcs.SetException(e));
                                }
                            }
                        );
                    }
                    catch (Exception e)
                    {
                        Task.Run(() => tcs.SetException(e));
                    }
                });

            return tcs.Task;
        }

        public Task ScheduleTask(Func<CAwaitable> toExecute)
        {
            var tcs = new TaskCompletionSource();
            Schedule(
                () =>
                {
                    try
                    {
                        var awaitable = toExecute();
                        var awaiter = awaitable.GetEphemeralAwaiter();
                        awaiter.OnCompleted(
                            () =>
                            {
                                try
                                {
                                    awaiter.GetResult();
                                    Task.Run(() => tcs.SetResult());
                                }
                                catch (Exception e)
                                {
                                    Task.Run(() => tcs.SetException(e));
                                }
                            }
                        );
                    }
                    catch (Exception e)
                    {
                        Task.Run(() => tcs.SetException(e));
                    }
                });

            return tcs.Task;
        }

        public Task<T> ScheduleTask<T>(Func<CTask<T>> toExecute)
        {
            var tcs = new TaskCompletionSource<T>();

            Schedule(() =>
                {
                    try
                    {
                        var task = toExecute();
                        var awaiter = task.Awaitable.GetEphemeralAwaiter();
                        awaiter.OnCompleted(() =>
                        {
                            try
                            {
                                var result = awaiter.GetResult();
                                Task.Run(() => tcs.SetResult(result));
                            }
                            catch (Exception e)
                            {
                                Task.Run(() => tcs.SetException(e));
                            }
                        });
                    }
                    catch (Exception e)
                    {
                        Task.Run(() => tcs.SetException(e));
                    }
                }
            );

            return tcs.Task;
        }

        public Task Sync()
        {
            var tcs = new TaskCompletionSource();
            Scheduler.FireAndForget(
                () => Api.Sync.AfterNext(() => Task.Run(tcs.SetResult), false)
            );

            return tcs.Task;
        }

        public Task Entangle(object toEntangle)
        {
            var tcs = new TaskCompletionSource();
            Scheduler.FireAndForget(() =>
            {
                Roots.Entangle(toEntangle);
                Task.Run(tcs.SetResult);
            });

            return tcs.Task;
        }

        public void Dispose() => Scheduler.Dispose();
    }
}
