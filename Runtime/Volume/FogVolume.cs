using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/FogVolume")]
    public class FogVolume : VolumeComponent
    {
        [Serializable]
        public enum FogFalloffMode
        {
            Planar = 0,
            Cylindrical,
            Spherical
        };

        [Serializable]
        public sealed class FogFalloffModeParameter : VolumeParameter<FogFalloffMode>
        {
            public FogFalloffModeParameter(FogFalloffMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

        public BoolParameter isEnabled = new BoolParameter(true);
        public FogFalloffModeParameter fogFalloffMode = new FogFalloffModeParameter(FogFalloffMode.Planar);
        public ColorParameter color = new ColorParameter(new Color(0.5f, 0.5f, 0.5f, 1.0f));
        public FloatParameter precisionAlpha = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public TextureParameter precisionAlphaDitherTexture = new TextureParameter(null);
        public ClampedFloatParameter precisionAlphaDither = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public MinFloatParameter distanceMin = new MinFloatParameter(0.0f, 0.0f);
        public MinFloatParameter distanceMax = new MinFloatParameter(100.0f, 1e-5f);
        public ClampedFloatParameter fogFalloffCurve = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        public BoolParameter heightFalloffEnabled = new BoolParameter(false);
        public FloatParameter heightMin = new FloatParameter(0.0f);
        public FloatParameter heightMax = new FloatParameter(10.0f);

        public BoolParameter isAdditionalLayerEnabled = new BoolParameter(false);
        public FogFalloffModeParameter fogFalloffModeLayer1 = new FogFalloffModeParameter(FogFalloffMode.Planar);
        public ColorParameter colorLayer1 = new ColorParameter(new Color(0.5f, 0.5f, 0.5f, 1.0f));
        public MinFloatParameter distanceMinLayer1 = new MinFloatParameter(0.0f, 0.0f);
        public MinFloatParameter distanceMaxLayer1 = new MinFloatParameter(100.0f, 1e-5f);
        public ClampedFloatParameter fogFalloffCurveLayer1 = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        public BoolParameter heightFalloffEnabledLayer1 = new BoolParameter(false);
        public FloatParameter heightMinLayer1 = new FloatParameter(0.0f);
        public FloatParameter heightMaxLayer1 = new FloatParameter(10.0f);

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