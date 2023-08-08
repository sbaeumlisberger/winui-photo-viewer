using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tocronx.SimpleAsync;

namespace PhotoViewer.Core.Utils
{
    public enum ErrorMode
    {
        None,
        Log,
        Abort,
    }

    public record class Failure<T>(T Element, Exception Exception);

    public class ProcessingResult<T>
    {
        public static ProcessingResult<T> Completed => new ProcessingResult<T>(Array.Empty<T>(), Array.Empty<Failure<T>>(), false);

        public IReadOnlyCollection<T> ProcessedElements { get; }

        public IReadOnlyCollection<Failure<T>> Failures { get; }

        public bool IsSuccessful => !HasFailures && !IsCanceld;

        public bool IsCanceld { get; }

        public bool HasFailures => Failures.Any();

        public ProcessingResult(IReadOnlyCollection<T> processedElements, IReadOnlyCollection<Failure<T>> failures, bool canceld)
        {
            ProcessedElements = processedElements;
            Failures = failures;
            IsCanceld = canceld;
        }
    }

    public static class ParallelizationUtil
    {
        public static async Task<TResults[]> MapParallelAsync<TElements, TResults>(
            this IEnumerable<TElements> elements,
            Func<TElements, Task<TResults>> mapFunction,
            CancellationToken cancellationToken = default,
            int maxParallelTasks = default)
        {
            if (maxParallelTasks == default)
            {
                maxParallelTasks = Environment.ProcessorCount;
            }
            else if (maxParallelTasks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxParallelTasks));
            }

            var tasks = new List<Task<TResults>>();
            foreach (var element in elements)
            {
                var runningTasks = tasks.Where(x => !x.IsCompleted).ToList();
                if (runningTasks.Count < maxParallelTasks)
                {
                    tasks.Add(mapFunction(element));
                }
                else
                {
                    await Task.WhenAny(runningTasks).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    tasks.Add(mapFunction(element));
                }
            }
            return await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public static async Task<ProcessingResult<T>> ProcessParallelAsync<T>(
            IReadOnlyCollection<T> elements,
            Func<T, Task> processElement,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default,
            int maxParallelTasks = default,
            ErrorMode errorMode = ErrorMode.Log,
            bool throwOnCancellation = false)
        {
            if (maxParallelTasks == default)
            {
                maxParallelTasks = Environment.ProcessorCount;
            }
            else if (maxParallelTasks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxParallelTasks));
            }

            if (!elements.Any())
            {
                progress?.Report(1);
                return new ProcessingResult<T>(new List<T>(), new List<Failure<T>>(), false);
            }

            bool aborted = false;

            var processedElements = new ConcurrentBag<T>();
            var failures = new ConcurrentBag<Failure<T>>();

            var tasks = new List<Task>(maxParallelTasks);

            int chunkSize = (int)Math.Ceiling((double)elements.Count / maxParallelTasks);

            elements.Chunk(chunkSize).ForEach(chunk =>
            {
                tasks.Add(Task.Run(async () =>
                {
                    foreach (var element in chunk)
                    {
                        if (aborted || cancellationToken.IsCancellationRequested is true)
                        {
                            return;
                        }
                        try
                        {
                            await processElement(element).ConfigureAwait(false);
                            processedElements.Add(element);
                            progress?.Report((double)processedElements.Count / elements.Count);
                        }
                        catch (OperationCanceledException) // can be thrown by processElement
                        {
                            if (throwOnCancellation)
                            {
                                throw;
                            }
                        }
                        catch (Exception exception)
                        {
                            failures.Add(new Failure<T>(element, exception));

                            switch (errorMode)
                            {
                                case ErrorMode.None:
                                    break;
                                case ErrorMode.Log:
                                    Log.Error($"Processing {element} failed.", exception);
                                    break;
                                case ErrorMode.Abort:
                                    aborted = true;
                                    return;
                            }
                        }
                    }
                }));
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            if (throwOnCancellation)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            progress?.Report(1);

            return new ProcessingResult<T>(processedElements, failures, cancellationToken.IsCancellationRequested);
        }

    }

}

