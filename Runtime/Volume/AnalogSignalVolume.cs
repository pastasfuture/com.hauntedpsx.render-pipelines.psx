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
        public BoolParameter analogSignalEnabled = new BoolParameter(true);
        public ClampedFloatParameter analogSignalKernelWidth = new ClampedFloatParameter(1.5f, 1f, 5f);
        public ClampedFloatParameter analogSignalSharpenPercent = new ClampedFloatParameter(0.8f, 0, 1);
        public ClampedFloatParameter analogSignalHorizontalCarrierFrequency = new ClampedFloatParameter(1, 0.1f, 3f);
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
