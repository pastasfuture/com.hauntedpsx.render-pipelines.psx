using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/LightingVolume")]
    public class LightingVolume : VolumeComponent
    {
        public BoolParameter lightingIsEnabled = new BoolParameter(false);
        public FloatParameter bakedLightingMultiplier = new FloatParameter(1.0f);
        public FloatParameter vertexColorLightingMultiplier = new FloatParameter(0.0f);
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