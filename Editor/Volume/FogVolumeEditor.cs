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
        SerializedDataParameter m_IsEnabled;
        SerializedDataParameter m_FogFalloffMode;
        SerializedDataParameter m_Color;
        SerializedDataParameter m_DistanceMin;
        SerializedDataParameter m_DistanceMax;
        SerializedDataParameter m_HeightFalloffEnabled;
        SerializedDataParameter m_HeightMin;
        SerializedDataParameter m_HeightMax;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FogVolume>(serializedObject);
            m_IsEnabled = Unpack(o.Find(x => x.isEnabled));
            m_FogFalloffMode = Unpack(o.Find(x => x.fogFalloffMode));
            m_Color = Unpack(o.Find(x => x.color));
            m_DistanceMin = Unpack(o.Find(x => x.distanceMin));
            m_DistanceMax = Unpack(o.Find(x => x.distanceMax));
            m_HeightFalloffEnabled = Unpack(o.Find(x => x.heightFalloffEnabled));
            m_HeightMin = Unpack(o.Find(x => x.heightMin));
            m_HeightMax = Unpack(o.Find(x => x.heightMax));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_IsEnabled, EditorGUIUtility.TrTextContent("Enabled", "Controls whether or not fog is enabled"));
            PropertyField(m_FogFalloffMode, EditorGUIUtility.TrTextContent("Fog Falloff Mode", "Controls the shape of the fog falloff."));
            PropertyField(m_Color, EditorGUIUtility.TrTextContent("Color", "Controls color (and opacity) of global distance fog."));
            PropertyField(m_DistanceMin, EditorGUIUtility.TrTextContent("Distance Min", "Controls the distance that the global fog starts."));
            PropertyField(m_DistanceMax, EditorGUIUtility.TrTextContent("Distance Max", "Controls the distance that the global fog ends."));
            PropertyField(m_HeightFalloffEnabled, EditorGUIUtility.TrTextContent("Height Falloff Enabled", "Controls whether or not the fog falls off vertically, in addition to in depth."));
            PropertyField(m_HeightMin, EditorGUIUtility.TrTextContent("Height Min", "Controls the height that fog reaches full opacity. Only has an effect if Height Falloff Enabled is true."));
            PropertyField(m_HeightMax, EditorGUIUtility.TrTextContent("Height Max", "Controls the height that fog reaches zero opacity. Only has an effect if Height Falloff Enabled is true."));
        }
    }
}