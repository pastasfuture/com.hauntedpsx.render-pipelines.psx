using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/LightingVolume")]
    public class LightingVolume : VolumeComponent
    {
        public BoolParameter lightingIsEnabled = new BoolParameter(false);
        public MinFloatParameter bakedLightingMultiplier = new MinFloatParameter(1.0f, 0.0f);
        public MinFloatParameter vertexColorLightingMultiplier = new MinFloatParameter(0.0f, 0.0f);
        public MinFloatParameter dynamicLightingMultiplier = new MinFloatParameter(1.0f, 0.0f);
        public ClampedIntParameter dynamicLightsMaxCount = new ClampedIntParameter(256, 1, 256);
        public ClampedIntParameter dynamicLightsMaxPerObjectCount = new ClampedIntParameter(4, 1, 8);

        static LightingVolume s_Default = null;
        public static LightingVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<LightingVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}