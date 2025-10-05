using System.Collections;
using System.Collections.Generic;
using HauntedPSX.RenderPipelines.PSX.Runtime;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(NTSCVolume))]
    public class NTSCVolumeEditor : VolumeComponentEditor
    {
        
        SerializedDataParameter m_isEnabled;
        SerializedDataParameter m_KernelRadius;
        SerializedDataParameter m_KernelWidthRatio;
        SerializedDataParameter m_Sharpness;
        SerializedDataParameter m_HorizontalCarrierFrequency;
        SerializedDataParameter m_LinePhaseShift;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<NTSCVolume>(serializedObject);
            m_isEnabled = Unpack(o.Find(x => x.isEnabled));
            m_HorizontalCarrierFrequency = Unpack(o.Find(x => x.horizontalCarrierFrequency));
            m_KernelRadius = Unpack(o.Find(x => x.kernelRadius));
            m_KernelWidthRatio = Unpack(o.Find(x => x.kernelWidthRatio));
            m_Sharpness = Unpack(o.Find(x => x.sharpness));
            m_LinePhaseShift = Unpack(o.Find(x => x.linePhaseShift));
        }
        
        public override void OnInspectorGUI()
        {
            PropertyField(m_isEnabled, EditorGUIUtility.TrTextContent("Enabled", "Controls whether the NTSC effect is active, which creates color bleeding and a natural blurriness."));
            PropertyField(m_HorizontalCarrierFrequency, EditorGUIUtility.TrTextContent("Horizontal Carrier Frequency", 
                "The carrier wave is driven by a very fast oscillator at a fixed frequency. Since the beam is travelling, " +
                "the phase of the carrier is linear both in time but also in horizontal distance over a scanline. This value " +
                "determines the frequency of the wave of the horizontal carrier. " +
                "\n\nIdeally, this should be set to a value which " +
                "makes the scanlines as hidden as possible. Doing it this way will create a \"rainbowing\" effect along edges, " +
                "directly related to the scanline frequency produced by this value."));
            PropertyField(m_KernelRadius, EditorGUIUtility.TrTextContent("Kernel Radius", "Controls how many steps the Gaussian blur should take (default 3)."));
            PropertyField(m_KernelWidthRatio,
                EditorGUIUtility.TrTextContent("Kernel Width Ratio",
                    "Controls the scale of the horizontal blur. " +
                    "\n\nTo achieve the intended effect, this should be used to blur out the vertical lines produced by the Horizontal Carrier Frequency parameter."));
            PropertyField(m_Sharpness, EditorGUIUtility.TrTextContent("Sharpness", "How much to apply sharpening after blurring."));
            PropertyField(m_LinePhaseShift, EditorGUIUtility.TrTextContent("Line Phase Shift", 
                "Offsets the wave produced by the Horizontal Carrier Frequency. In most cases this value is unnoticable, and is best left at the default of 3.14."));
        }
        
    }
}