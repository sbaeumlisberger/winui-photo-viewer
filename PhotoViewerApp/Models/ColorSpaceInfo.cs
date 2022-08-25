﻿namespace PhotoViewerCoreModule.Model
{
    public class ColorSpaceInfo
    {
        public ColorSpaceType Type { get; }

        public byte[]? Profile { get; }

        public ColorSpaceInfo(ColorSpaceType type, byte[]? profile = null)
        {
            Type = type;
            Profile = profile;
        }
    }
}
