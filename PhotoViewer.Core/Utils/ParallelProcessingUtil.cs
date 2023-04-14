using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoViewer.Core.Utils
{
    public enum ErrorMode
    {
        Ignore,
        Log,
        Abort,
    }

    public record class Failure<T>(T Element, Exception Exception);

    public class ProcessingResult<T>
    {
        public IReadOnlyCollection<T> ProcessedElements { get; }

        public IReadOnlyCollection<Failure<T>> Failures { get; }

        public bool IsSuccessful => !HasFailures;

        public bool HasFailures => Failures.Any();

        public ProcessingResult(IReadOnlyCollection<T> processedElements, IReadOnlyCollection<Failure<T>> failures)
        {
            ProcessedElements = processedElements;
            Failures = failures;
        }
    }

    public static class ParallelProcessingUtil
    {

        public static async Task<ProcessingResult<T>> ProcessParallelAsync<T>(
            IReadOnlyCollection<T> elements, 
            Func<T, Task> processElement, 
            IProgress<double>? progress = null, 
            CancellationToken? cancellationToken = null, 
            int numberOfThreads = 4, 
            ErrorMode errorMode = ErrorMode.Log,
            bool throwOnCancellation = false)
        {
            if (!elements.Any()) 
            {
                progress?.Report(1);
                return new ProcessingResult<T>(new List<T>(), new List<Failure<T>>());
            }

            bool aborted = false;

            var processedElements = new ConcurrentBag<T>();
            var failures = new ConcurrentBag<Failure<T>>();

            var tasks = new List<Task>(numberOfThreads);

            int chunkSize = (int)Math.Ceiling((double)elements.Count / numberOfThreads);

            elements.Chunk(chunkSize).ForEach(chunk =>
            {
                tasks.Add(Task.Run(async () =>
                {
                    foreach (var element in chunk)
                    {
                        if (aborted || cancellationToken?.IsCancellationRequested is true)
                        {
                            return;
                        }
                        try
                        {
                            await processElement(element).ConfigureAwait(false);
                            processedElements.Add(element);
                            progress?.Report((double)processedElements.Count / elements.Count);
                        }
                        catch (TaskCanceledException) 
                        {
                            aborted = true;

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
                                case ErrorMode.Ignore:
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
                cancellationToken?.ThrowIfCancellationRequested();
            }

            progress?.Report(1);

            return new ProcessingResult<T>(processedElements, failures);
        }

    }

}

