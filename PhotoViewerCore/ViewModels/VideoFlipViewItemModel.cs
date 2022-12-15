using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using PhotoViewerApp.Messages;
using PhotoViewerApp.Models;
using PhotoViewerApp.Utils;
using PhotoViewerApp.Utils.Logging;
using PhotoViewerCore.Utils;
using PhotoViewerCore.ViewModels;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

namespace PhotoViewerApp.ViewModels;

public partial class VideoFlipViewItemModel : ViewModelBase, IMediaFlipViewItemModel
{
    public IMediaFileInfo MediaItem { get; }

    public bool IsActive { get; set; }

    public Task PlaybackCompletedTask => playbackTaskCompletionSource.Task;

    public bool AutoPlay { get; set; }

    public MediaPlayer MediaPlayer { get; } = new MediaPlayer();

    public MediaFileContextMenuModel ContextMenuModel { get; }

    private MediaSource? mediaSource;

    private TaskCompletionSource<bool> playbackTaskCompletionSource = new TaskCompletionSource<bool>();

    public VideoFlipViewItemModel(IMediaFileInfo mediaFile, MediaFileContextMenuModel contextMenuModel, IMessenger messenger) : base(messenger)
    {
        MediaItem = mediaFile;
        ContextMenuModel = contextMenuModel;
        ContextMenuModel.Files = new[] { mediaFile };
    }

    public async Task InitializeAsync()
    {
        Messenger.Register<StartDiashowMessage>(this, OnReceive);
        Messenger.Register<ExitDiashowMessage>(this, OnReceive);        

        using var stream = await MediaItem.OpenAsync(FileAccessMode.Read);

        mediaSource = MediaSource.CreateFromStream(stream, MediaItem.ContentType); ;
        mediaSource.StateChanged += MediaSource_StateChanged;
        mediaSource.OpenOperationCompleted += MediaSource_OpenOperationCompleted;

        MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        MediaPlayer.Source = mediaSource;
    }

    public void Cleanup()
    {
        Messenger.UnregisterAll(this);

        if (mediaSource != null)
        {
            mediaSource.StateChanged -= MediaSource_StateChanged;
            mediaSource.OpenOperationCompleted -= MediaSource_OpenOperationCompleted;
            mediaSource.Dispose();
        }

        MediaPlayer.MediaEnded -= MediaPlayer_MediaEnded;
        MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
        MediaPlayer.Dispose();
    }

    partial void OnIsActiveChanged()
    {
        if (IsActive)
        {
            if (AutoPlay)
            {
                MediaPlayer.Play();
            }
        }
        else
        {
            MediaPlayer.Pause();
            MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
            playbackTaskCompletionSource = new TaskCompletionSource<bool>();
        }
    }


    private void OnReceive(StartDiashowMessage msg)
    { 
        if (IsActive)
        {
            MediaPlayer.Play();
      }
    }

    private void OnReceive(ExitDiashowMessage msg)
    {
        if (IsActive)
        {
            MediaPlayer.Pause();
        }
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

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        playbackTaskCompletionSource.SetResult(true);
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Log.Error($"An error occured playing \"{MediaItem.Name}\": {args.Error}: {args.ErrorMessage}", args.ExtendedErrorCode);
    }
}
