using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels.Shared;

public partial class ToolsMenuModel : ViewModelBase
{
    public IAcceleratedCommand MoveRawFilesToSubfolderCommand { get; }
    public IAcceleratedCommand DeleteSingleRawFilesCommand { get; }
    public IAcceleratedCommand ShiftDatenTakenCommand { get; }
    public IAcceleratedCommand ImportGpxTrackCommand { get; }
    public IAcceleratedCommand PrefixFilesByDateCommand { get; }

    internal ToolsMenuModel(
        IMoveRawFilesToSubfolderCommand moveRawFilesToSubfolderCommand,
        IDeleteSingleRawFilesCommand deleteSingleRawFilesCommand,
        IShiftDatenTakenCommand shiftDatenTakenCommand,
        IImportGpxTrackCommand importGpxTrackCommand,
        IPrefixFilesByDateCommand prefixFilesByDateCommand)
    {
        MoveRawFilesToSubfolderCommand = moveRawFilesToSubfolderCommand;
        DeleteSingleRawFilesCommand = deleteSingleRawFilesCommand;
        ShiftDatenTakenCommand = shiftDatenTakenCommand;
        ImportGpxTrackCommand = importGpxTrackCommand;
        PrefixFilesByDateCommand = prefixFilesByDateCommand;
    }

}
