using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
#if UNITY_2022_1_OR_NEWER
    [CustomEditor(typeof(TonemapperVolume))]
#else
    [VolumeComponentEditor(typeof(TonemapperVolume))]
#endif
    public class TonemapperVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_IsEnabled;
        SerializedDataParameter m_Contrast;
        SerializedDataParameter m_Shoulder;
        SerializedDataParameter m_GraypointIn;
        SerializedDataParameter m_GraypointOut;
        SerializedDataParameter m_Whitepoint;
        SerializedDataParameter m_CrossTalk;
        SerializedDataParameter m_Saturation;
        SerializedDataParameter m_CrossTalkSaturation;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<TonemapperVolume>(serializedObject);
            m_IsEnabled = Unpack(o.Find(x => x.isEnabled));
            m_Contrast = Unpack(o.Find(x => x.contrast));
            m_Shoulder = Unpack(o.Find(x => x.shoulder));
            m_GraypointIn = Unpack(o.Find(x => x.graypointIn));
            m_GraypointOut = Unpack(o.Find(x => x.graypointOut));
            m_Whitepoint = Unpack(o.Find(x => x.whitepoint));
            m_CrossTalk = Unpack(o.Find(x => x.crossTalk));
            m_Saturation = Unpack(o.Find(x => x.saturation));
            m_CrossTalkSaturation = Unpack(o.Find(x => x.crossTalkSaturation));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_IsEnabled, EditorGUIUtility.TrTextContent("Enabled", "Controls whether or not Tonemapping is enabled.\nDisabled is more faithful to PS1 content, as tonemapping was not popularized in games at the time.\nEnabled gives more creative control."));
            PropertyField(m_Contrast, EditorGUIUtility.TrTextContent("Contrast", "Controls the contrast of the tonemapper."));
            PropertyField(m_Shoulder, EditorGUIUtility.TrTextContent("Shoulder", "Controls speed at which raw (radiance) values approach a tonemapped pixel value of 1.0."));
            PropertyField(m_GraypointIn, EditorGUIUtility.TrTextContent("Graypoint In", "Controls the raw (radiance) value to place our graypoint compensation handle. Useful for ambient room light compensation."));
            PropertyField(m_GraypointOut, EditorGUIUtility.TrTextContent("Graypoint Out", "Controls the output pixel valute to transform our graypoint compensation handle to. Useful for ambient room light compensation."));
            PropertyField(m_Whitepoint, EditorGUIUtility.TrTextContent("Whitepoint", "Controls the raw (radiance) value at which the tonemapper will clip to 1.0."));
            PropertyField(m_CrossTalk, EditorGUIUtility.TrTextContent("Cross Talk", "Controls the amount of cross talk of the R, G, B color channels. High cross talk causes bright colors to saturate towards white. Low cross talk causes bright colors to saturate towards it's pure hue."));
            PropertyField(m_Saturation, EditorGUIUtility.TrTextContent("Saturation", "Controls global saturation of image. 0.0 applies no saturation modification (raw image). -1 is fully desaturated. 1 is max saturation."));
            PropertyField(m_CrossTalkSaturation, EditorGUIUtility.TrTextContent("Cross Talk Saturation", "Controls saturation of the cross talk."));
        }
    }
}