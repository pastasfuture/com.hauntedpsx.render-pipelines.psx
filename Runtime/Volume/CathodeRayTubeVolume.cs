using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/Cathode Ray Tube")]
    public class CathodeRayTubeVolume : VolumeComponent
    {
        [Serializable]
        public enum CRTGrateMaskMode
        {
            CompressedTV = 0,
            ApertureGrill,
            VGA,
            VGAStretched,
            Texture,
            Disabled
        };

        [Serializable]
        public sealed class CRTGrateMaskModeParameter : VolumeParameter<CRTGrateMaskMode>
        {
            public CRTGrateMaskModeParameter(CRTGrateMaskMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        public BoolParameter isEnabled = new BoolParameter(true);
        public ClampedFloatParameter bloom = new ClampedFloatParameter(1.0f / 16.0f, 0.0f, 1.0f);
        public CRTGrateMaskModeParameter grateMaskMode = new CRTGrateMaskModeParameter(CRTGrateMaskMode.CompressedTV);
        public TextureParameter grateMaskTexture = new TextureParameter(null);
        public FloatParameter grateMaskScale = new FloatParameter(1.0f);
        public ClampedFloatParameter scanlineSharpness = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ClampedFloatParameter imageSharpness = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ClampedFloatParameter bloomSharpnessX = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        public ClampedFloatParameter bloomSharpnessY = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        public ClampedFloatParameter noiseIntensity = new ClampedFloatParameter(1.0f / 10.0f, 0.0f, 1.0f);
        public ClampedFloatParameter noiseSaturation = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter grateMaskIntensityMin = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);
        public ClampedFloatParameter grateMaskIntensityMax = new ClampedFloatParameter(0.75f, 0.0f, 1.0f);
        public ClampedFloatParameter barrelDistortionX = new ClampedFloatParameter(8.0f / 64.0f, 0.0f, 1.0f);
        public ClampedFloatParameter barrelDistortionY = new ClampedFloatParameter(8.0f / 24.0f, 0.0f, 1.0f);
        public ClampedFloatParameter vignette = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        static CathodeRayTubeVolume s_Default = null;
        public static CathodeRayTubeVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<CathodeRayTubeVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}