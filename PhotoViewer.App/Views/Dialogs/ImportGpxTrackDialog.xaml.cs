using PhotoViewer.App.Resources;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core.ViewModels.Dialogs;

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
