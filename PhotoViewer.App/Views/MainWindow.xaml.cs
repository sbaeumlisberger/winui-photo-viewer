using CommunityToolkit.Mvvm.Messaging;
using Essentials.NET.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PhotoViewer.App.Services;
using PhotoViewer.App.Utils;
using PhotoViewer.App.Views.Dialogs;
using PhotoViewer.Core;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using PhotoViewer.Core.Services;
using PhotoViewer.Core.Utils;
using PhotoViewer.Core.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using WinUIEx;

namespace PhotoViewer.App;

public sealed partial class MainWindow : Window
{
    public event TypedEventHandler<MainWindow, AppWindowClosingEventArgs>? Closing;

    private readonly ViewRegistrations viewRegistrations = ViewRegistrations.Instance;

    private MainWindowModel ViewModel => viewModel ?? throw new InvalidOperationException("ViewModel not set");

    private MainWindowModel? viewModel;

    private readonly DialogService dialogService;

    private readonly IMessenger messenger;

    public MainWindow(IMessenger messenger)
    {
        this.messenger = messenger;

        this.InitializeComponent();

        WindowManager.PersistenceStorage = new PersistentDictionary(Path.Combine(AppData.PrivateFolder, "window.dat"));
        WindowManager.Get(this).PersistenceId = "MainWindow";

        AppWindow.Closing += AppWindow_Closing;
        Closed += MainWindow_Closed;

        AppWindow.SetIcon("Assets/icon.ico");

        dialogService = new DialogService(this);

        messenger.Register<EnterFullscreenMessage>(this, _ => EnterFullscreen());
        messenger.Register<ExitFullscreenMessage>(this, _ => ExitFullscreen());
        messenger.Register<NavigateToPageMessage>(this, msg => NavigateToPage(msg.PageType, msg.Parameter));
        messenger.Register<NavigateBackMessage>(this, _ => NavigateBack());

        FocusManager.LosingFocus += FocusManager_LosingFocus;
    }

    public void SetViewModel(MainWindowModel viewModel)
    {
        this.viewModel = viewModel;

        viewModel.DialogRequested += ViewModel_DialogRequested;

        viewModel.Subscribe(this, nameof(viewModel.Title), () => Title = viewModel.Title, initialCallback: true);
        viewModel.Subscribe(this, nameof(viewModel.Theme), ApplyTheme, initialCallback: true);
    }

    public async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        dialog.XamlRoot = frame.XamlRoot;
        dialog.RequestedTheme = frame.RequestedTheme;
        return await dialog.ShowAsync();
    }

    public async Task<T> ShowDialogAsync<T>(FrameworkElement dialog, Func<Task<T>> getResultFunction)
    {
        dialog.HorizontalAlignment = HorizontalAlignment.Center;
        dialog.VerticalAlignment = VerticalAlignment.Center;

        var overlayContent = new Border();
        overlayContent.Background = new SolidColorBrush(Color.FromArgb(150, 0, 0, 0));
        overlayContent.Width = Content.ActualSize.X;
        overlayContent.Height = Content.ActualSize.Y;
        overlayContent.Child = dialog;
        SizeChanged += Window_SizeChanged;

        void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            overlayContent.Width = Content.ActualSize.X;
            overlayContent.Height = Content.ActualSize.Y;
        }

        var overlayPopup = new Popup();
        overlayPopup.XamlRoot = Content.XamlRoot;
        overlayPopup.Child = overlayContent;
        overlayPopup.IsOpen = true;

        DispatcherQueue.TryEnqueue(() => dialog.Focus(FocusState.Programmatic));

        var result = await getResultFunction();

        overlayPopup.IsOpen = false;
        SizeChanged -= Window_SizeChanged;

        return result;
    }

    private void ViewModel_DialogRequested(object? sender, DialogRequestedEventArgs e)
    {
        e.AddTask(dialogService.ShowDialogAsync(e.DialogModel));
    }

    private void FocusManager_LosingFocus(object? sender, LosingFocusEventArgs e)
    {
        // prevent losing focus to the RootScrollViewer (ScrollViewer with parent of type DependencyObject)
        if (e.NewFocusedElement is ScrollViewer sv && sv.Parent.GetType() == typeof(DependencyObject))
        {
            e.TryCancel();
        }
    }

    private void ApplyTheme()
    {
        var elementTheme = (ElementTheme)ViewModel.Theme;

        if (ViewModel.Theme == AppTheme.System)
        {
            // force update of theme
            bool isDark = App.Current.RequestedTheme == ApplicationTheme.Dark;
            frame.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        }

        frame.RequestedTheme = elementTheme;
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        Closing?.Invoke(this, args);

        if (args.Cancel)
        {
            args.Cancel = true;
            return;
        }

        try
        {
            args.Cancel = true;
            await ViewModel.OnClosingAsync();
        }
        finally
        {
            Close();
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        messenger.UnregisterAll(this);
        Log.Info("Application closed");
        Log.Logger.Dispose();
    }

    private void EnterFullscreen()
    {
        AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
    }

    private void ExitFullscreen()
    {
        AppWindow.SetPresenter(AppWindowPresenterKind.Default);
    }

    private void NavigateToPage(Type pageModelType, object? parameter)
    {
        frame.Navigate(viewRegistrations.GetViewTypeForViewModelType(pageModelType), parameter);
    }

    private void NavigateBack()
    {
        frame.GoBack();
    }

    private void Frame_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.F11)
        {
            if (AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
            {
                ExitFullscreen();
            }
            else
            {
                EnterFullscreen();
            }
        }
    }

    private void Frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        Log.Error("Navigation failed", e.Exception);
        e.Handled = true;
    }

    private void Frame_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Task.Run(ReportCrashAsync);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to check or report application crash", ex);
        }
    }

    private async Task ReportCrashAsync()
    {
        var errorReportService = new ErrorReportService(Package.Current.Id.Version, new EventLogService());

        if (await errorReportService.CreateCrashReportAsync() is string crashReport)
        {
            await DispatcherQueue.DispatchAsync(async () =>
            {
                var dialog = new CrashReportDialog(crashReport);

                if (await ShowDialogAsync(dialog) == ContentDialogResult.Primary)
                {
                    await errorReportService.SendCrashReportAsync(crashReport);
                }
            });
        }
    }

    private class PersistentDictionary : IDictionary<string, object>
    {
        private readonly string filePath;
        private readonly Dictionary<string, object> dictionary;

        public PersistentDictionary(string filePath)
        {
            this.filePath = filePath;

            if (File.Exists(this.filePath))
            {
                dictionary = LoadFromFile();
            }
            else
            {
                dictionary = new Dictionary<string, object>();
            }
        }

        private Dictionary<string, object> LoadFromFile()
        {
            var dictionary = new Dictionary<string, object>();
            using var stream = new FileStream(filePath, FileMode.Open);
            using var reader = new BinaryReader(stream);
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadString();
                object value = ReadObject(reader);
                dictionary[key] = value;
            }
            return dictionary;
        }

        private void SaveToFile()
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            writer.Write(dictionary.Count);
            foreach (var kvp in dictionary)
            {
                writer.Write(kvp.Key);
                WriteObject(writer, kvp.Value);
            }
        }

        private object ReadObject(BinaryReader reader)
        {
            byte typeMarker = reader.ReadByte();
            switch (typeMarker)
            {
                case 0: return reader.ReadString();
                default: throw new InvalidOperationException("Unsupported type: " + typeMarker);
            }
        }

        private void WriteObject(BinaryWriter writer, object obj)
        {
            switch (obj)
            {
                case string s:
                    writer.Write((byte)0);
                    writer.Write(s);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported type: " + obj.GetType());
            }
        }

        public object this[string key]
        {
            get => dictionary[key];
            set
            {
                dictionary[key] = value;
                SaveToFile();
            }
        }

        public ICollection<string> Keys => dictionary.Keys;

        public ICollection<object> Values => dictionary.Values;

        public int Count => dictionary.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            dictionary.Add(key, value);
            SaveToFile();
        }

        public bool ContainsKey(string key) => dictionary.ContainsKey(key);

        public bool Remove(string key)
        {
            var removed = dictionary.Remove(key);
            if (removed)
            {
                SaveToFile();
            }
            return removed;
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => dictionary.TryGetValue(key, out value);

        public void Add(KeyValuePair<string, object> item)
        {
            dictionary.Add(item.Key, item.Value);
            SaveToFile();
        }

        public void Clear()
        {
            dictionary.Clear();
            SaveToFile();
        }

        public bool Contains(KeyValuePair<string, object> item) => dictionary.Contains(item);

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((IDictionary<string, object>)dictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            var removed = dictionary.Remove(item.Key);
            if (removed)
            {
                SaveToFile();
            }
            return removed;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => dictionary.GetEnumerator();
    }

}
