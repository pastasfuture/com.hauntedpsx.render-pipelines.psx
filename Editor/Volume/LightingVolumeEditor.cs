using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(LightingVolume))]
    public class LightingVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_LightingIsEnabled;
        SerializedDataParameter m_BakedLightingMultipler;
        SerializedDataParameter m_VertexColorLightingMultiplier;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<LightingVolume>(serializedObject);
            m_LightingIsEnabled = Unpack(o.Find(x => x.lightingIsEnabled));
            m_BakedLightingMultipler = Unpack(o.Find(x => x.bakedLightingMultiplier));
            m_VertexColorLightingMultiplier = Unpack(o.Find(x => x.vertexColorLightingMultiplier));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_LightingIsEnabled, EditorGUIUtility.TrTextContent("Lighting Is Enabled"));
            PropertyField(m_BakedLightingMultipler, EditorGUIUtility.TrTextContent("Baked Lighting Multiplier"));
            PropertyField(m_IndirectLightIntensity, EditorGUIUtility.TrTextContent("Vertex Color Lighting Multiplier"));
        }
    }
}