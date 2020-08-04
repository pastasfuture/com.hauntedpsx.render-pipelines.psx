using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/PrecisionVolume")]
    public class PrecisionVolume : VolumeComponent
    {
        [Serializable]
        public enum DrawDistanceFalloffMode
        {
            Planar = 0,
            Cylindrical,
            Spherical
        };

        [Serializable]
        public sealed class DrawDistanceFalloffModeParameter : VolumeParameter<DrawDistanceFalloffMode>
        {
            public DrawDistanceFalloffModeParameter(DrawDistanceFalloffMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        public ClampedFloatParameter geometry = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter color = new ClampedFloatParameter(4.0f / 7.0f, 0.0f, 1.0f);
        public ClampedFloatParameter chroma = new ClampedFloatParameter(1.0f / 3.0f, 0.0f, 1.0f);
        public ClampedFloatParameter alpha = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter affineTextureWarping = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter framebufferDither = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public DrawDistanceFalloffModeParameter drawDistanceFalloffMode = new DrawDistanceFalloffModeParameter(DrawDistanceFalloffMode.Planar);
        public MinFloatParameter drawDistance = new MinFloatParameter(100.0f, 0.0f);
        static PrecisionVolume s_Default = null;
        public static PrecisionVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<PrecisionVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}