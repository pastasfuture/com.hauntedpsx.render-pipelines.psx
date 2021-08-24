using System.Collections;
using System.Collections.Generic;
using HauntedPSX.RenderPipelines.PSX.Runtime;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(AnalogSignalVolume))]
    public class AnalogSignalVolumeEditor : VolumeComponentEditor
    {
        
        SerializedDataParameter m_AnalogSignalEnabled;
        SerializedDataParameter m_AnalogSignalBlurStrength;
        SerializedDataParameter m_AnalogSignalKernelWidth;
        SerializedDataParameter m_AnalogSignalSharpenPercent;
        SerializedDataParameter m_AnalogSignalHorizontalCarrierFrequency;
        SerializedDataParameter m_AnalogSignalLinePhaseShift;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<AnalogSignalVolume>(serializedObject);
            m_AnalogSignalEnabled = Unpack(o.Find(x => x.analogSignalEnabled));
            m_AnalogSignalKernelWidth = Unpack(o.Find(x => x.analogSignalKernelWidth));
            m_AnalogSignalSharpenPercent = Unpack(o.Find(x => x.analogSignalSharpenPercent));
            m_AnalogSignalHorizontalCarrierFrequency = Unpack(o.Find(x => x.analogSignalHorizontalCarrierFrequency));
            m_AnalogSignalLinePhaseShift = Unpack(o.Find(x => x.analogSignalLinePhaseShift));
        }
        
        public override void OnInspectorGUI()
        {
            PropertyField(m_AnalogSignalEnabled, EditorGUIUtility.TrTextContent("Enabled", "Controls whether the analog signal effect is active, which creates color bleeding and a natural blurriness."));
            PropertyField(m_AnalogSignalKernelWidth, EditorGUIUtility.TrTextContent("Kernel Width", "Controls the scale of the horizontal blur."));
            PropertyField(m_AnalogSignalSharpenPercent, EditorGUIUtility.TrTextContent("Sharpen", "How much to apply sharpening after blurring."));
            PropertyField(m_AnalogSignalHorizontalCarrierFrequency, EditorGUIUtility.TrTextContent("Horizontal Carrier Frequency", 
                "The carrier wave is driven by a very fast oscillator at a fixed frequency. Since the beam is travelling, " +
                "the phase of the carrier is linear both in time but also in horizontal distance over a scanline. This value " +
                "determines the frequency of the wave of the horizontal carrier. " +
                "\n\nIdeally, this should be set to a value which " +
                "makes the scanlines as hidden as possible. Doing it this way will create a \"rainbowing\" effect along edges, " +
                "directly related to the scanline frequency produced by this value."));
            PropertyField(m_AnalogSignalLinePhaseShift, EditorGUIUtility.TrTextContent("Line Phase Shift", 
                ""));
        }
        
    }
}