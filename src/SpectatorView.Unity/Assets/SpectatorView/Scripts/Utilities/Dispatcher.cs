// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.MixedReality.SpectatorView
{
    /// <summary>
    /// A helper class to Dispatch actions to the GameThread
    /// </summary>
    public static class Dispatcher
    {
        private static int? mainThreadId = null;
        private static TaskScheduler mainThreadScheduler;

        public static void Initialize(int mainThreadId, TaskScheduler mainThreadScheduler)
        {
            if (Dispatcher.mainThreadId.HasValue)
            {
                throw new InvalidOperationException("Dispatcher already initialized.");
            }

            Dispatcher.mainThreadId = mainThreadId;
            Dispatcher.mainThreadScheduler = mainThreadScheduler;
        }

        public static void Unintialize()
        {
            mainThreadId = null;
            mainThreadScheduler = null;
        }

        /// <summary>
        /// Returns the Id of the current managed thread.
        /// </summary>
        public static int CurrentThreadId
        {
            get
            {
#if WINDOWS_UWP
                return Environment.CurrentManagedThreadId;
#else
                return Thread.CurrentThread.ManagedThreadId;
#endif
            }
        }

        /// <summary>
        /// Returns whether or not the calling thread is in fact a game thread.
        /// </summary>
        public static bool IsGameThread { get { return CurrentThreadId == mainThreadId; } }

        /// <summary>
        /// A helper that throws if the current thread is not a game thread.
        /// </summary>
        public static void ThrowIfNotGameThread()
        {
            if (mainThreadId == null)
            {
                throw new InvalidOperationException("Dispatcher not initialized.");
            }

            if (!IsGameThread)
            {
                throw new InvalidOperationException("The current thread is not the game thread.");
            }
        }

        /// <summary>
        /// Schedules an action to be executed on the GameThread task scheduler.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the operation.</param>
        /// <param name="runImmediateIfOnGameThread">If true, will check if current thread is the game thread and execute the dispatched operation immediately.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task ScheduleAsync(Action action, CancellationToken cancellationToken, bool runImmediateIfOnGameThread = false)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (runImmediateIfOnGameThread && IsGameThread)
            {
                try
                {
                    action();
                    return Task.CompletedTask;
                }
                // Ensure same behaviour for exception handling on consuming side.
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            }
            else
            {
                Task task = new Task(action, cancellationToken);
                task.Start(mainThreadScheduler);
                return task;
            }
        }

        /// <summary>
        /// Schedules a function to be executed on the GameThread task scheduler.
        /// </summary>
        /// <typeparam name="TResult">The generic type of the return parameter.</typeparam>
        /// <param name="func">The function to execute.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the operation.</param>
        /// <param name="runImmediateIfOnGameThread">If true, will check if current thread is the game thread and execute the dispatched operation immediately.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task<TResult> ScheduleAsync<TResult>(Func<TResult> func, CancellationToken cancellationToken, bool runImmediateIfOnGameThread = false)
        {
            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (runImmediateIfOnGameThread && IsGameThread)
            {
                try
                {
                    return Task.FromResult(func());
                }
                // Ensure same behaviour for exception handling on consuming side.
                catch (Exception ex)
                {
                    return Task.FromException<TResult>(ex);
                }
            }
            else
            {
                Task<TResult> task = new Task<TResult>(func, cancellationToken);
                task.Start(mainThreadScheduler);
                return task;
            }
        }

        /// <summary>
        /// Schedules an asyncrhonous action to be executed on the GameThread task scheduler.
        /// </summary>
        /// <param name="asyncAction">The asyncrhonous action to execute.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the operation.</param>
        /// <param name="runImmediateIfOnGameThread">If true, will check if current thread is the game thread and execute the dispatched operation immediately.</param>
        /// <returns>The Task representing the unwrapped asynchronous operation.</returns>
        public static Task ScheduleAsync(Func<Task> asyncAction, CancellationToken cancellationToken, bool runImmediateIfOnGameThread = false)
        {
            return ScheduleAsync<Task>(asyncAction, cancellationToken, runImmediateIfOnGameThread).Unwrap();
        }

        /// <summary>
        /// Schedules an asycnrhonous function to be executed on the GameThread task scheduler.
        /// </summary>
        /// <typeparam name="TResult">The generic type of the return parameter.</typeparam>
        /// <param name="func">The asynchronous function to execute.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the operation.</param>
        /// <param name="runImmediateIfOnGameThread">If true, will check if current thread is the game thread and execute the dispatched operation immediately.</param>
        /// <returns>The Task representing the unwrapped asynchronous operation.</returns>
        public static Task<TResult> ScheduleAsync<TResult>(Func<Task<TResult>> asyncFunc, CancellationToken cancellationToken, bool runImmediateIfOnGameThread = false)
        {
            return ScheduleAsync<Task<TResult>>(asyncFunc, cancellationToken, runImmediateIfOnGameThread).Unwrap();
        }

        /// <summary>
        /// A helper to wait for next frame.
        /// </summary>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static async Task WaitForNextFrameAsync()
        {
            ThrowIfNotGameThread();

            await Task.Delay(1);
        }

        /// <summary>
        /// Conducts a polling wait on the game thread until a predicate is satisfied.
        /// </summary>
        /// <param name="predicate">The condition to wait for.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the operation.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task WhenAsync(Func<bool> predicate, CancellationToken cancellationToken)
        {
            return ScheduleAsync(async () =>
            {
                while (!predicate())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await WaitForNextFrameAsync();
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Conducts a polling wait on the game thread until an result returend by an evaluator is no longer null.
        /// </summary>
        /// <param name="evaluator">The function that returns the result to check for null.</param>
        /// <param name="cancellationToken">CancellationToken to cancel the operation.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        public static Task<T> WhenNotNullAsync<T>(Func<T> evaluator, CancellationToken cancellationToken) where T : class
        {
            return ScheduleAsync(async () =>
            {
                T item;
                while ((item = evaluator()) == null)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await WaitForNextFrameAsync();
                }

                return item;
            }, cancellationToken);
        }
    }
}
