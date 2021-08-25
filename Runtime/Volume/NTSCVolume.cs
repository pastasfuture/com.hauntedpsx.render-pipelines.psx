using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/NTSCVolume")]
    public class NTSCVolume : VolumeComponent
    {
        public BoolParameter isEnabled = new BoolParameter(true);
        public ClampedFloatParameter horizontalCarrierFrequency = new ClampedFloatParameter(2.06f, 0.1f, 3f);
        public ClampedFloatParameter kernelWidthRatio = new ClampedFloatParameter(0.42f, 0.1f, 2f);
        //public ClampedFloatParameter kernelWidth = new ClampedFloatParameter(1f, 1f, 5f);
        public ClampedFloatParameter sharpenPercent = new ClampedFloatParameter(0.8f, 0, 1);
        public ClampedFloatParameter linePhaseShift = new ClampedFloatParameter(3.14f, 0, 6.28f);
        public ClampedFloatParameter flickerPercent = new ClampedFloatParameter(1f, 0f, 1f);
        public ClampedFloatParameter flickerScaleX = new ClampedFloatParameter(0.1f, 0f, 5f);
        public ClampedFloatParameter flickerScaleY = new ClampedFloatParameter(4f, 1f, 5f);
        
        
        static NTSCVolume s_Default = null;
        public static NTSCVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<NTSCVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}
