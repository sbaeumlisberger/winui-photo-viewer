using CommunityToolkit.Mvvm.Messaging;
using OpenCvSharp;
using OpenCvSharp.Face;
using PhotoViewer.App.Utils.Logging;
using PhotoViewer.Core.Messages;
using PhotoViewer.Core.Models;
using System.Text.Json;
using Windows.Graphics.Imaging;

namespace PhotoViewer.Core.Services;

// TODO handle multi instances/proceses
internal class FaceRecognitionService
{
    private const string ModelFileName = "face_recognizer_model.xml";
    private const string LabelsFileName = "face_recognizer_labels.json";

    private readonly string modelFilePath = Path.Combine(AppData.PublicFolder, ModelFileName);
    private readonly string labelsFilePath = Path.Combine(AppData.PublicFolder, LabelsFileName);

    private readonly LBPHFaceRecognizer faceRecognizer = LBPHFaceRecognizer.Create();

    private Dictionary<int, string> nameByLabel = [];

    private Dictionary<string, int> labelByName = [];

    public FaceRecognitionService(IMessenger messenger)
    {
        Task.Run(() =>
        {
            Load();
            messenger.Register<AppClosingMessage>(this, (_, _) => Save());
        });
    }

    public void Train(IBitmapFileInfo file, BitmapBounds faceRect, string personName)
    {
        using Mat image = Cv2.ImRead(file.FilePath, ImreadModes.Grayscale);

        Rect rect = new Rect((int)faceRect.X, (int)faceRect.Y, (int)faceRect.Width, (int)faceRect.Height);

        using var faceImg = new Mat(image, rect);

        if (!labelByName.TryGetValue(personName, out int label))
        {
            label = labelByName.Count;
            labelByName.Add(personName, label);
            nameByLabel.Add(label, personName);
        }

        faceRecognizer.Update([faceImg], [label]);
    }

    public string? Predict(IBitmapFileInfo file, BitmapBounds faceRect)
    {
        if (faceRecognizer.Empty)
        {
            return null;
        }

        using Mat image = Cv2.ImRead(file.FilePath, ImreadModes.Grayscale);

        Rect rect = new Rect((int)faceRect.X, (int)faceRect.Y, (int)faceRect.Width, (int)faceRect.Height);

        using var faceImg = new Mat(image, rect);

        faceRecognizer.Predict(faceImg, out int label, out double confidence);

        if (nameByLabel.TryGetValue(label, out string? name))
        {
            Log.Debug($"Predicted name: {name}, confidence: {confidence}");
            return name;
        }

        return null;
    }

    private void Load()
    {
        if (File.Exists(modelFilePath) && File.Exists(labelsFilePath))
        {
            faceRecognizer.Read(modelFilePath);
            nameByLabel = JsonSerializer.Deserialize<Dictionary<int, string>>(File.ReadAllText(labelsFilePath))!;
            labelByName = nameByLabel.ToDictionary(x => x.Value, x => x.Key);
        }
    }

    private void Save()
    {
        faceRecognizer.Write(modelFilePath);
        File.WriteAllText(Path.Combine(labelsFilePath), JsonSerializer.Serialize(nameByLabel));
    }
}
