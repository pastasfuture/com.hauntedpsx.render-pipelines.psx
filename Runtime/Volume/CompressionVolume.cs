using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/CompressionVolume")]
    public class CompressionVolume : VolumeComponent
    {
        [Serializable]
        public enum CompressionMode
        {
            Accurate = 0,
            Fast
        };

        [Serializable]
        public sealed class CompressionModeParameter : VolumeParameter<CompressionMode>
        {
            public CompressionModeParameter(CompressionMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        [Serializable]
        public enum CompressionColorspace
        {
            YUV = 0,
            YCOCG,
            FCCYIQ,
            YCBCR,
            YCBCRJPEG,
            SRGB
        };

        [Serializable]
        public sealed class CompressionColorspaceParameter : VolumeParameter<CompressionColorspace>
        {
            public CompressionColorspaceParameter(CompressionColorspace value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        public BoolParameter isEnabled = new BoolParameter(false);
        public ClampedFloatParameter weight = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter accuracy = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        public CompressionModeParameter mode = new CompressionModeParameter(CompressionMode.Accurate);
        public CompressionColorspaceParameter colorspace = new CompressionColorspaceParameter(CompressionColorspace.YUV);

        static CompressionVolume s_Default = null;
        public static CompressionVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<CompressionVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}