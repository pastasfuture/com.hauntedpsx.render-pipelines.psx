using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/TonemapperVolume")]
    public class TonemapperVolume : VolumeComponent
    {
        public BoolParameter isEnabled = new BoolParameter(false);
        public ClampedFloatParameter contrast = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ClampedFloatParameter shoulder = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ClampedFloatParameter graypointIn = new ClampedFloatParameter(0.18f, 0.0f, 1.0f);
        public ClampedFloatParameter graypointOut = new ClampedFloatParameter(0.18f, 0.0f, 1.0f);
        public FloatParameter whitepoint = new FloatParameter(10.0f);
        public ClampedFloatParameter crossTalk = new ClampedFloatParameter(0.394f, 0.0f, 1.0f);
        public ClampedFloatParameter saturation = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        public ClampedFloatParameter crossTalkSaturation = new ClampedFloatParameter(0.039f, 1e-3f, 1.0f);

        static TonemapperVolume s_Default = null;
        public static TonemapperVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<TonemapperVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}