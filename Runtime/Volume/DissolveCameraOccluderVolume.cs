using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/DissolveCameraOccluderVolume")]
    public class DissolveCameraOccluderVolume : VolumeComponent
    {
        [Serializable]
        public enum DissolveCameraOccluderMode
        {
            Disabled = 0,
            DistanceFade,
            RadialFade
        };

        [Serializable]
        public sealed class DissolveCameraOccluderModeParameter : VolumeParameter<DissolveCameraOccluderMode>
        {
            public DissolveCameraOccluderModeParameter(DissolveCameraOccluderMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        public DissolveCameraOccluderModeParameter mode = new DissolveCameraOccluderModeParameter(DissolveCameraOccluderMode.Disabled);
        public ClampedFloatParameter fadeAlpha = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);
        public MinFloatParameter distanceMin = new MinFloatParameter(2.0f, 0.0f);
        public MinFloatParameter distanceMax = new MinFloatParameter(3.0f, 0.0f);
        public MinFloatParameter radiusMin = new MinFloatParameter(0.5f, 0.0f);
        public MinFloatParameter radiusMax = new MinFloatParameter(0.75f, 0.0f);

        static DissolveCameraOccluderVolume s_Default = null;
        public static DissolveCameraOccluderVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<DissolveCameraOccluderVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}