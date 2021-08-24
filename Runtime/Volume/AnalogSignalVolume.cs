using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/AnalogSignalVolume")]
    public class AnalogSignalVolume : VolumeComponent
    {
        public BoolParameter analogSignalEnabled = new BoolParameter(false);
        public ClampedIntParameter analogSignalBlurStrength = new ClampedIntParameter(5, 1, 5);
        public ClampedFloatParameter analogSignalKernelWidth = new ClampedFloatParameter(0.002f, 0.001f, 0.01f);
        public ClampedFloatParameter analogSignalSharpenPercent = new ClampedFloatParameter(0.8f, 0, 1);
        public ClampedFloatParameter analogSignalHorizontalCarrierFrequency = new ClampedFloatParameter(150, 50, 1000);
        public ClampedFloatParameter analogSignalLinePhaseShift = new ClampedFloatParameter(3.14f, 0, 6.28f);

        static AnalogSignalVolume s_Default = null;
        public static AnalogSignalVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<AnalogSignalVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}
