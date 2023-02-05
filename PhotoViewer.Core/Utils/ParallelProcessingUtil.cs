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

        public static async Task<ProcessingResult<T>> ProcessParallelAsync<T>(IReadOnlyCollection<T> elements, Func<T, Task> processElement, int numberOfThreads = 4, Progress? progress = null, ErrorMode errorHandling = ErrorMode.Log)
        {
            progress?.Initialize(true, true, elements.Count);

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
                        if (aborted)
                        {
                            return;
                        }

                        try
                        {
                            await processElement(element).ConfigureAwait(false);
                            processedElements.Add(element);
                        }
                        catch (Exception exception)
                        {
                            failures.Add(new Failure<T>(element, exception));

                            switch (errorHandling)
                            {
                                case ErrorMode.Ignore:
                                    break;
                                case ErrorMode.Log:
                                    Log.Error($"Processing {element} failed.", exception);
                                    break;
                                case ErrorMode.Abort:
                                    aborted = true;
                                    progress?.Fail();
                                    return;
                            }
                        }

                        if (progress != null && !await progress.WaitIfPausedAndReport(1).ConfigureAwait(false))
                        {
                            return;
                        }
                    }
                }));
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            return new ProcessingResult<T>(processedElements, failures);
        }

    }

}

