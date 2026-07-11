using PhotoViewer.Core.Commands;
using PhotoViewer.Core.Utils;

namespace PhotoViewer.Core.ViewModels.Shared;

public partial class ToolsMenuModel : ViewModelBase
{
    public IKeyboardAcceleratedCommand MoveRawFilesToSubfolderCommand { get; }
    public IKeyboardAcceleratedCommand DeleteSingleRawFilesCommand { get; }
    public IKeyboardAcceleratedCommand ShiftDateTakenCommand { get; }
    public IKeyboardAcceleratedCommand ImportGpxTrackCommand { get; }
    public IKeyboardAcceleratedCommand PrefixFilesByDateCommand { get; }

    internal ToolsMenuModel(
        IMoveRawFilesToSubfolderCommand moveRawFilesToSubfolderCommand,
        IDeleteSingleRawFilesCommand deleteSingleRawFilesCommand,
        IShiftDateTakenCommand shiftDateTakenCommand,
        IImportGpxTrackCommand importGpxTrackCommand,
        IPrefixFilesByDateCommand prefixFilesByDateCommand)
    {
        MoveRawFilesToSubfolderCommand = moveRawFilesToSubfolderCommand;
        DeleteSingleRawFilesCommand = deleteSingleRawFilesCommand;
        ShiftDateTakenCommand = shiftDateTakenCommand;
        ImportGpxTrackCommand = importGpxTrackCommand;
        PrefixFilesByDateCommand = prefixFilesByDateCommand;
    }

}
