using PhotoViewer.App.Resources;
using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;

namespace PhotoViewer.App.Views;

[ViewRegistration(typeof(ImportGpxTrackDialogModel))]
public sealed partial class ImportGpxTrackDialog : MultiViewDialogBase, IMVVMControl<ImportGpxTrackDialogModel>
{
    public ImportGpxTrackDialog()
    {
        this.InitializeComponentMVVM();
    }

    private string FormatErrorMessage(int updatedFilesCount)
    {
        return string.Format(Strings.ImportGpxTrackDialog_ErrorMessage, updatedFilesCount);
    }

    private string FormatSuccessMessage(int updatedFilesCount)
    {
        return string.Format(Strings.ImportGpxTrackDialog_SuccessMessage, updatedFilesCount);
    }

}
