using Essentials.NET.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Printing;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;

namespace PhotoViewer.App.Utils;

internal class PrintRegistration
{
    private readonly DispatcherQueue dispatcherQueue;
    private readonly Func<IPrintJob> printJobFactory;

    public PrintRegistration(DispatcherQueue dispatcherQueue, Func<IPrintJob> printJobFactory)
    {
        this.dispatcherQueue = dispatcherQueue;
        this.printJobFactory = printJobFactory;
    }

    public async void OnPrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
    {
        var deferral = args.Request.GetDeferral();
        try
        {
            await dispatcherQueue.DispatchAsync(() =>
            {
                var printJob = printJobFactory();

                PrintTask printTask = null!;
                printTask = args.Request.CreatePrintTask(printJob.Title, sourceRequestedArgs =>
                {
                    CreatePrintSource(printTask, printJob, sourceRequestedArgs);
                });

                var printTaskOptionDetails = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);
                printJob.SetupPrintOptions(printTaskOptionDetails);
                printJob.UpdateDisplayedOptions(printTaskOptionDetails);
            });
        }
        catch (Exception ex)
        {
            Log.Error("Failed to create print task", ex);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private async void CreatePrintSource(PrintTask printTask, IPrintJob printJob, PrintTaskSourceRequestedArgs sourceRequestedArgs)
    {
        var deferral = sourceRequestedArgs.GetDeferral();
        try
        {
            await dispatcherQueue.DispatchAsync(() =>
            {
                var printSource = new PrintSource(printTask, printJob);
                sourceRequestedArgs.SetSource(printSource.PrintDocumentSource);
            });
        }
        catch (Exception ex)
        {
            Log.Error("Failed to create print source", ex);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private class PrintSource
    {
        public IPrintDocumentSource PrintDocumentSource => printDocument.DocumentSource;

        private readonly PrintTask printTask;
        private readonly IPrintJob printJob;
        public readonly PrintDocument printDocument = new PrintDocument();
        private readonly PrintTaskOptionDetails printTaskOptionDetails;
        private List<UIElement> previewPages = new List<UIElement>();

        public PrintSource(PrintTask printTask, IPrintJob printJob)
        {
            this.printTask = printTask;
            this.printJob = printJob;

            printDocument.Paginate += PrintDocument_Paginate;
            printDocument.GetPreviewPage += PrintDocument_GetPreviewPage;
            printDocument.AddPages += PrintDocument_AddPages;

            printTaskOptionDetails = PrintTaskOptionDetails.GetFromPrintTaskOptions(printTask.Options);
            printTaskOptionDetails.OptionChanged += PrintTaskOptionDetails_OptionChanged;

            printTask.Completed += PrintTask_Completed;
        }

        private void PrintDocument_Paginate(object sender, PaginateEventArgs e)
        {
            previewPages = printJob.CreatePages(e.PrintTaskOptions, true, printDocument.InvalidatePreview);
            printDocument.SetPreviewPageCount(previewPages.Count, PreviewPageCountType.Intermediate);
        }

        private void PrintDocument_AddPages(object sender, AddPagesEventArgs e)
        {
            foreach (var page in printJob.CreatePages(e.PrintTaskOptions, false, printDocument.InvalidatePreview))
            {
                printDocument.AddPage(page);
            }
            printDocument.AddPagesComplete();
        }

        private void PrintDocument_GetPreviewPage(object sender, GetPreviewPageEventArgs e)
        {
            if (previewPages.Any())
            {
                printDocument.SetPreviewPage(e.PageNumber, previewPages[e.PageNumber - 1]);
            }
        }

        private async void PrintTaskOptionDetails_OptionChanged(PrintTaskOptionDetails printTaskOptionDetails, PrintTaskOptionChangedEventArgs args)
        {
            if (args.OptionId != null)
            {
                printJob.UpdateDisplayedOptions(printTaskOptionDetails);
            }

            await printDocument.DispatcherQueue.DispatchAsync(() =>
            {
                try
                {
                    printDocument.InvalidatePreview();
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to invalidate print preview", ex);
                }
            });
        }

        private async void PrintTask_Completed(PrintTask sender, PrintTaskCompletedEventArgs args)
        {
            try
            {
                printTask.Completed -= PrintTask_Completed;

                await printDocument.DispatcherQueue.DispatchAsync(() =>
                {
                    printDocument.Paginate -= PrintDocument_Paginate;
                    printDocument.GetPreviewPage -= PrintDocument_GetPreviewPage;
                    printDocument.AddPages -= PrintDocument_AddPages;
                });

                printTaskOptionDetails.OptionChanged -= PrintTaskOptionDetails_OptionChanged;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to cleanup print source", ex);
            }
        }
    }
}
