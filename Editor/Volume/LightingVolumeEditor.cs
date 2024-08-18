using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
#if UNITY_2022_1_OR_NEWER
    [CustomEditor(typeof(LightingVolume))]
#else
    [VolumeComponentEditor(typeof(LightingVolume))]
#endif
    public class LightingVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_LightingIsEnabled;
        SerializedDataParameter m_BakedLightingMultipler;
        SerializedDataParameter m_VertexColorLightingMultiplier;
        SerializedDataParameter m_DynamicLightingMultiplier;
        SerializedDataParameter m_DynamicLightsMaxCount;
        SerializedDataParameter m_DynamicLightsMaxPerObjectCount;
        SerializedDataParameter m_LightingClampMode;
        SerializedDataParameter m_LightingClamp;
        SerializedDataParameter m_LightingClampSharpness;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<LightingVolume>(serializedObject);
            m_LightingIsEnabled = Unpack(o.Find(x => x.lightingIsEnabled));
            m_BakedLightingMultipler = Unpack(o.Find(x => x.bakedLightingMultiplier));
            m_VertexColorLightingMultiplier = Unpack(o.Find(x => x.vertexColorLightingMultiplier));
            m_DynamicLightingMultiplier = Unpack(o.Find(x => x.dynamicLightingMultiplier));
            m_DynamicLightsMaxCount = Unpack(o.Find(x => x.dynamicLightsMaxCount));
            m_DynamicLightsMaxPerObjectCount = Unpack(o.Find(x => x.dynamicLightsMaxPerObjectCount));
            m_LightingClampMode = Unpack(o.Find(x => x.lightingClampMode));
            m_LightingClamp = Unpack(o.Find(x => x.lightingClamp));
            m_LightingClampSharpness = Unpack(o.Find(x => x.lightingClampSharpness));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_LightingIsEnabled, EditorGUIUtility.TrTextContent("Lighting Is Enabled", "Controls whether lighting is enabled or disabled."));
            PropertyField(m_BakedLightingMultipler, EditorGUIUtility.TrTextContent("Baked Lighting Multiplier", "Controls the intensity of the baked lighting information when applied to the surface."));
            PropertyField(m_VertexColorLightingMultiplier, EditorGUIUtility.TrTextContent("Vertex Color Lighting Multiplier", "Controls the intensity of the vertex colors when applied to the surface."));
            PropertyField(m_DynamicLightingMultiplier, EditorGUIUtility.TrTextContent("Dynamic Lighting Multiplier", "Controls the intensity of dynamic lighting when applied to the surface. Set to 0.0 to globally disable dynamic light sources."));
            if (m_DynamicLightingMultiplier.value.floatValue > 0.0f)
            {
                PropertyField(m_DynamicLightsMaxCount, EditorGUIUtility.TrTextContent("Dynamic Lights Max", "Controls the maximum number of realtime light sources that can be active within the viewport. If the scene light count exceeds this, lights will be discarded."));
                PropertyField(m_DynamicLightsMaxPerObjectCount, EditorGUIUtility.TrTextContent("Dynamic Lights Max Per Object", "Controls the maximum number of realtime light sources that can affect a single object. Any lights that exceed this count will be discard. Reduce this value to improve performance. Increase this value to potentially improve quality."));
            }
            PropertyField(m_LightingClampMode, EditorGUIUtility.TrTextContent("Lighting Clamp Mode", "Controls whether a light brightness limit is imposed.\nNone: No limit imposed. Most physically correct. Artifacts can occur with bright light sources, and lower color precision or texture compression.\nClamp: Introduces an artist-defined hard clamp value. Useful for limiting artifacts, or stylized rendering.\nSoft Clamp: Same as Clamp, but uses a soft-min curve to apply the clamp. Results in a smoother transition from unclamped to clamped brightness values."));

            var lightingClampModeValue = (LightingVolume.LightingClampMode)m_LightingClampMode.value.intValue;
            if (lightingClampModeValue != LightingVolume.LightingClampMode.None)
            {
                PropertyField(m_LightingClamp, EditorGUIUtility.TrTextContent("Lighting Clamp", "Light intensity reflected off a surface (both diffuse and specular) is clamped to never exceed this clamp value.\nA clamp value of 1.0 ensures that surfaces can never become brighter than their authored color. Useful for limiting artifacts, or stylized rendering.\nBe careful with this value. If too low of a clamp value is used, scenes can appear overly dark or low contrast."));

                if (lightingClampModeValue == LightingVolume.LightingClampMode.SoftClamp)
                {
                    PropertyField(m_LightingClampSharpness, EditorGUIUtility.TrTextContent("Lighting Soft Clamp Sharpness", "Controls how sharply light intensity transitions from unclamped to clamped.\nA value of 0.0 is the softest transition.\nA value of 1.0 is the sharpest transition."));
                }
            }
        }
    }
}