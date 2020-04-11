using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/QualityOverrideVolume")]
    public class QualityOverrideVolume : VolumeComponent
    {
        // Default to PSX Quality Disabled so that prefab editing is easier
        // (most of the time you do not want to edit prefabs with low / pixelated / CRT settings on)
        public BoolParameter isPSXQualityEnabled = new BoolParameter(false);

        static QualityOverrideVolume s_Default = null;
        public static QualityOverrideVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<QualityOverrideVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}