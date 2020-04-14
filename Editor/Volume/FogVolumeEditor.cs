using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(FogVolume))]
    public class FogVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Color;
        SerializedDataParameter m_DistanceMin;
        SerializedDataParameter m_DistanceMax;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FogVolume>(serializedObject);
            m_Color = Unpack(o.Find(x => x.color));
            m_DistanceMin = Unpack(o.Find(x => x.distanceMin));
            m_DistanceMax = Unpack(o.Find(x => x.distanceMax));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Color, EditorGUIUtility.TrTextContent("Color", "Controls color (and opacity) of global distance fog"));
            PropertyField(m_DistanceMin, EditorGUIUtility.TrTextContent("Distance Min", "Controls the distance that the global fog starts."));
            PropertyField(m_DistanceMax, EditorGUIUtility.TrTextContent("Distance Max", "Controls the distance that the global fog ends."));
        }
    }
}