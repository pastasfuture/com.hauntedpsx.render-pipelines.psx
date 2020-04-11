using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/PrecisionVolume")]
    public class PrecisionVolume : VolumeComponent
    {
        public ClampedFloatParameter geometry = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter color = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public BoolParameter framebufferDitherIsEnabled = new BoolParameter(true);
        public ClampedFloatParameter drawDistance = new ClampedFloatParameter(100.0f, 0.0f, 10000.0f);
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