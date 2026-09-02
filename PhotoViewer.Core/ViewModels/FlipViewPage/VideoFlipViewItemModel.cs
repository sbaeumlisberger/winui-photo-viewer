using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET;
using Essentials.NET.Logging;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Utils;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace PhotoViewer.Core.ViewModels;

public partial class VideoFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaFile { get; }

    public partial bool IsSelected { get; set; }

    public partial bool IsDiashowActive { get; set; }

    public Task PlaybackCompletedTask => playbackCompletionSource.Task;

    public partial MediaPlayer? MediaPlayer { get; private set; }

    public bool IsContextMenuEnabled => IsSelected && !IsDiashowActive;

    public IMediaFileContextMenuModel ContextMenuModel { get; }

    private IRandomAccessStream? mediaStream;

    private MediaSource? mediaSource;

    private readonly CancelableTaskRunner initRunner = new();

    private TaskCompletionSource playbackCompletionSource = new();

    public VideoFlipViewItemModel(IMediaFileInfo mediaFile, IViewModelFactory viewModelFactory, IMessenger messenger) : base(messenger)
    {
        MediaFile = mediaFile;
        ContextMenuModel = viewModelFactory.CreateMediaFileContextMenuModel();
        ContextMenuModel.Files = new[] { mediaFile };
    }

    partial void OnIsContextMenuEnabledChanged()
    {
        ContextMenuModel.IsEnabled = IsContextMenuEnabled;
    }

    public async Task InitializeAsync()
    {
        await initRunner.RunAndCancelPrevious(async cancellationToken =>
        {
            mediaStream = await MediaFile.OpenAsRandomAccessStreamAsync(FileAccessMode.Read);

            cancellationToken.ThrowIfCancellationRequested();

            mediaSource = MediaSource.CreateFromStream(mediaStream, MediaFile.ContentType);
            mediaSource.StateChanged += MediaSource_StateChanged;
            mediaSource.OpenOperationCompleted += MediaSource_OpenOperationCompleted;

            MediaPlayer = new MediaPlayer();
            MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
            MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
            MediaPlayer.Source = mediaSource;

            if (IsSelected && IsDiashowActive)
            {
                PlayVideo();
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
        if (!IsSelected)
        {
            PauseVideo();
            ResetPlaybackPosition();
        }

        if (IsSelected && IsDiashowActive)
        {
            PlayVideo();
        }
    }

    partial void OnIsDiashowActiveChanged()
    {
        if (!IsSelected)
        {
            return;
        }

        if (IsDiashowActive)
        {
            PlayVideo();
        }
        else
        {
            PauseVideo();
        }
    }

    private void PlayVideo()
    {
        if (playbackCompletionSource.Task.IsCompleted)
        {
            playbackCompletionSource = new();
            ResetPlaybackPosition();
        }

        MediaPlayer?.Play();
    }

    private void PauseVideo()
    {
        MediaPlayer?.Pause();
    }

    private void ResetPlaybackPosition()
    {
        MediaPlayer?.PlaybackSession.Position = TimeSpan.Zero;
    }

    private void MediaSource_StateChanged(MediaSource sender, MediaSourceStateChangedEventArgs args)
    {
        if (args.NewState == MediaSourceState.Failed)
        {
            Log.Error($"Could not open \"{MediaFile.DisplayName}\"");
        }
    }

    private void MediaSource_OpenOperationCompleted(MediaSource sender, MediaSourceOpenOperationCompletedEventArgs args)
    {
        if (args.Error?.ExtendedError is not null)
        {
            Log.Error($"Could not open \"{MediaFile.DisplayName}\"", args.Error.ExtendedError);
        }
    }

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        playbackCompletionSource.TrySetResult();

        if (IsSelected && IsDiashowActive)
        {
            PlayVideo();
        }
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Log.Error($"An error occurred playing \"{MediaFile.DisplayName}\": {args.Error}: {args.ErrorMessage}", args.ExtendedErrorCode);
        playbackCompletionSource.TrySetResult();
    }
}
