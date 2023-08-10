using CommunityToolkit.Mvvm.Messaging;
using PhotoViewer.App.Models;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System.Threading.Tasks;
using Tocronx.SimpleAsync;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.App.ViewModels;

public partial class VideoFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    public bool IsSelected { get; set; }

    public bool IsDiashowActive { get; set; }

    public Task PlaybackCompletedTask => playbackCompletionSource.Task;

    public MediaPlayer? MediaPlayer { get; private set; }

    public bool IsContextMenuEnabeld => !IsDiashowActive;

    public IMediaFileContextMenuModel ContextMenuModel { get; }
    
    private IRandomAccessStream? mediaStream;

    private MediaSource? mediaSource;

    private readonly CancelableTaskRunner initRunner = new CancelableTaskRunner();

    private readonly TaskCompletionSource playbackCompletionSource = new TaskCompletionSource();

    public VideoFlipViewItemModel(IMediaFileInfo mediaFile, IViewModelFactory viewModelFactory, IMessenger messenger) : base(messenger)
    {
        MediaItem = mediaFile;
        ContextMenuModel = viewModelFactory.CreateMediaFileContextMenuModel();
        ContextMenuModel.Files = new[] { mediaFile };
    }

    public async Task InitializeAsync()
    {
        await initRunner.RunAndCancelPrevious(async cancellationToken =>
        {
            mediaStream = await MediaItem.OpenAsRandomAccessStreamAsync(FileAccessMode.Read);

            cancellationToken.ThrowIfCancellationRequested();

            mediaSource = MediaSource.CreateFromStream(mediaStream, MediaItem.ContentType);
            mediaSource.StateChanged += MediaSource_StateChanged;
            mediaSource.OpenOperationCompleted += MediaSource_OpenOperationCompleted;

            MediaPlayer = new MediaPlayer();
            MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            MediaPlayer.Source = mediaSource;

            if (IsSelected && IsDiashowActive)
            {
                MediaPlayer.Play();
            }
        });
    }

    protected override void OnCleanup()
    {
        initRunner.Cancel();

        playbackCompletionSource.TrySetResult();

        if (MediaPlayer is { } mediaPlayer)
        {
            MediaPlayer = null;
            mediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
            mediaPlayer.Dispose();
        }

        if (this.mediaSource is { } mediaSource)
        {
            this.mediaSource = null;            
            mediaSource.StateChanged -= MediaSource_StateChanged;
            mediaSource.OpenOperationCompleted -= MediaSource_OpenOperationCompleted;
            mediaSource.Dispose();
        }

        if (this.mediaStream is { } mediaStream)
        {
            this.mediaStream = null;
            mediaStream.Dispose();
        }

        ContextMenuModel.Cleanup();
    }

    partial void OnIsSelectedChanged()
    {
        if (MediaPlayer is null)
        {
            return;
        }

        if (!IsSelected)
        {
            MediaPlayer.Pause();
            MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
        }

        if (IsSelected && IsDiashowActive)
        {
            MediaPlayer.Play();
        }
    }

    partial void OnIsDiashowActiveChanged()
    {
        if (!IsSelected || MediaPlayer is null)
        {
            return;
        }

        if (IsDiashowActive)
        {
            MediaPlayer.Play();
        }
        else
        {
            MediaPlayer.Pause();
        }
    }

    private void MediaSource_StateChanged(MediaSource sender, MediaSourceStateChangedEventArgs args)
    {
        if (args.NewState == MediaSourceState.Failed)
        {
            Log.Error($"Could not open \"{MediaItem.DisplayName}\"");
        }
    }

    private void MediaSource_OpenOperationCompleted(MediaSource sender, MediaSourceOpenOperationCompletedEventArgs args)
    {
        if (args.Error?.ExtendedError is not null)
        {
            Log.Error($"Could not open \"{MediaItem.DisplayName}\"", args.Error.ExtendedError);
        }
    }

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        playbackCompletionSource.SetResult();
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Log.Error($"An error occured playing \"{MediaItem.DisplayName}\": {args.Error}: {args.ErrorMessage}", args.ExtendedErrorCode);
        playbackCompletionSource.SetResult();
    }
}
