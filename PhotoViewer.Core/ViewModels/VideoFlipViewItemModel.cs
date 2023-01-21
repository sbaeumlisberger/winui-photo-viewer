using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;
using Tocronx.SimpleAsync;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace PhotoViewerApp.ViewModels;

public partial class VideoFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    public bool IsSelected { get; set; }

    public bool IsDiashowActive { get; set; }

    public Task PlaybackCompletedTask => WaitForPlaybackToComplete();

    public MediaPlayer? MediaPlayer { get; private set; }

    public bool IsContextMenuEnabeld => !IsDiashowActive;

    public MediaFileContextMenuModel ContextMenuModel { get; }

    private MediaSource? mediaSource;

    private readonly CancelableTaskRunner initRunner = new CancelableTaskRunner();

    public VideoFlipViewItemModel(IMediaFileInfo mediaFile, MediaFileContextMenuModel contextMenuModel, IMessenger messenger) : base(messenger)
    {
        MediaItem = mediaFile;
        ContextMenuModel = contextMenuModel;
        ContextMenuModel.Files = new[] { mediaFile };
    }

    public async Task PrepareAsync()
    {
        await initRunner.RunAndCancelPrevious(async cancellationToken =>
        {
            using var stream = await MediaItem.OpenAsync(FileAccessMode.Read);

            cancellationToken.ThrowIfCancellationRequested();

            mediaSource = MediaSource.CreateFromStream(stream, MediaItem.ContentType);
            mediaSource.StateChanged += MediaSource_StateChanged;
            mediaSource.OpenOperationCompleted += MediaSource_OpenOperationCompleted;

            MediaPlayer = new MediaPlayer();
            MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            MediaPlayer.Source = mediaSource;
        });
    }

    public void Cleanup()
    {
        initRunner.Cancel();

        if (this.mediaSource is MediaSource mediaSource)
        {
            this.mediaSource = null;
            mediaSource.StateChanged -= MediaSource_StateChanged;
            mediaSource.OpenOperationCompleted -= MediaSource_OpenOperationCompleted;
            mediaSource.Dispose();
        }

        if (MediaPlayer is MediaPlayer mediaPlayer)
        {
            MediaPlayer = null;
            mediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
            mediaPlayer.Dispose();
        }
    }

    partial void OnIsSelectedChanged()
    {
        if (IsSelected)
        {
            if (IsDiashowActive)
            {
                MediaPlayer!.Play();
            }
        }
        else
        {
            MediaPlayer!.Pause();
            MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
        }
    }

    partial void OnIsDiashowActiveChanged()
    {
        if (IsSelected)
        {
            if (IsDiashowActive)
            {
                MediaPlayer!.Play();
            }
            else
            {
                MediaPlayer!.Pause();
            }
        }
    }

    private Task WaitForPlaybackToComplete()
    {
        if (MediaPlayer is MediaPlayer mediaPlayer)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            mediaPlayer.MediaEnded += mediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += mediaPlayer_MediaFailed;
            void mediaPlayer_MediaEnded(MediaPlayer sender, object args)
            {
                sender.MediaEnded -= mediaPlayer_MediaEnded;
                sender.MediaFailed -= mediaPlayer_MediaFailed;
                taskCompletionSource.SetResult(true);
            }
            void mediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
            {
                sender.MediaEnded -= mediaPlayer_MediaEnded;
                sender.MediaFailed -= mediaPlayer_MediaFailed;
            }
            return taskCompletionSource.Task;
        }
        return Task.CompletedTask;
    }


    private void MediaSource_StateChanged(MediaSource sender, MediaSourceStateChangedEventArgs args)
    {
        if (args.NewState == MediaSourceState.Failed)
        {
            Log.Error($"Could not open \"{MediaItem.Name}\"");
        }
    }

    private void MediaSource_OpenOperationCompleted(MediaSource sender, MediaSourceOpenOperationCompletedEventArgs args)
    {
        if (args.Error?.ExtendedError is not null)
        {
            Log.Error($"Could not open \"{MediaItem.Name}\"", args.Error.ExtendedError);
        }
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Log.Error($"An error occured playing \"{MediaItem.Name}\": {args.Error}: {args.ErrorMessage}", args.ExtendedErrorCode);
    }
}
