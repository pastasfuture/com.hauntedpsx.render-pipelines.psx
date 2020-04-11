using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/FogVolume")]
    public class FogVolume : VolumeComponent
    {
        public ColorParameter color = new ColorParameter(new Color(0.5f, 0.5f, 0.5f, 1.0f));
        public ClampedFloatParameter distanceMin = new ClampedFloatParameter(0.0f, 0.0f, 100.0f);
        public ClampedFloatParameter distanceMax = new ClampedFloatParameter(100.0f, 0.0f, 100.0f);
        static FogVolume s_Default = null;
        public static FogVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<FogVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}