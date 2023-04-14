using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace PhotoViewer.Core.Models;

internal record DetectedFaceModel(BitmapBounds FaceBox);