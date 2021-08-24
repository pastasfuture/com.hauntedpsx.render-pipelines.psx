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
            m_AnalogSignalBlurStrength = Unpack(o.Find(x => x.analogSignalBlurStrength));
            m_AnalogSignalKernelWidth = Unpack(o.Find(x => x.analogSignalKernelWidth));
            m_AnalogSignalSharpenPercent = Unpack(o.Find(x => x.analogSignalSharpenPercent));
            m_AnalogSignalHorizontalCarrierFrequency = Unpack(o.Find(x => x.analogSignalHorizontalCarrierFrequency));
            m_AnalogSignalLinePhaseShift = Unpack(o.Find(x => x.analogSignalLinePhaseShift));
        }
        
        public override void OnInspectorGUI()
        {
            PropertyField(m_AnalogSignalEnabled, EditorGUIUtility.TrTextContent("Enable Analog Signal", "Controls whether the analog signal effect is active, which creates color bleeding and a natural blurriness."));
            PropertyField(m_AnalogSignalBlurStrength, EditorGUIUtility.TrTextContent("Blur Strength", "Controls the strength of the Gaussian blur used in the Analog Signal effect."));
            PropertyField(m_AnalogSignalKernelWidth, EditorGUIUtility.TrTextContent("Kernel Width", "Controls the scale of the horizontal blur."));
            PropertyField(m_AnalogSignalSharpenPercent, EditorGUIUtility.TrTextContent("Sharpen", "How much to apply sharpening after blurring."));
            PropertyField(m_AnalogSignalHorizontalCarrierFrequency, EditorGUIUtility.TrTextContent("Horizontal Carrier Frequency", "."));
            PropertyField(m_AnalogSignalLinePhaseShift, EditorGUIUtility.TrTextContent("Line Phase Shift", "."));
        }
        
    }
}