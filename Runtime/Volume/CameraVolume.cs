using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/CameraVolume")]
    public class CameraVolume : VolumeComponent
    {
        [Serializable]
        public enum CameraAspectMode
        {
            FreeStretch = 0,
            FreeFitPixelPerfect,
            FreeCropPixelPerfect,
            FreeBleedPixelPerfect,
            LockedFitPixelPerfect,
            LockedFit,
            Native
        };

        [Serializable]
        public sealed class CameraAspectModeParameter : VolumeParameter<CameraAspectMode>
        {
            public CameraAspectModeParameter(CameraAspectMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        public BoolParameter isFrameLimitEnabled = new BoolParameter(false);
        public MinIntParameter frameLimit = new MinIntParameter(24, 1);
        public CameraAspectModeParameter aspectMode = new CameraAspectModeParameter(CameraAspectMode.FreeBleedPixelPerfect); 
        public ClampedIntParameter targetRasterizationResolutionWidth = new ClampedIntParameter(256, 1, 4096);
        public ClampedIntParameter targetRasterizationResolutionHeight = new ClampedIntParameter(224, 1, 4096);
        public BoolParameter isDepthBufferEnabled = new BoolParameter(true);
        public BoolParameter isClearDepthAfterBackgroundEnabled = new BoolParameter(true);
        public BoolParameter isClearDepthBeforeUIEnabled = new BoolParameter(true);

        static CameraVolume s_Default = null;
        public static CameraVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<CameraVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}