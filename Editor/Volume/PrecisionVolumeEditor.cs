using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(PrecisionVolume))]
    public class PrecisionVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Geometry;
        SerializedDataParameter m_Color;
        SerializedDataParameter m_FramebufferDitherIsEnabled;
        SerializedDataParameter m_DrawDistance;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<PSXPrecisionVolume>(serializedObject);
            m_Geometry = Unpack(o.Find(x => x.geometry));
            m_Color = Unpack(o.Find(x => x.color));
            m_FramebufferDitherIsEnabled = Unpack(o.Find(x => x.framebufferDitherIsEnabled));
            m_DrawDistance = Unpack(o.Find(x => x.drawDistance));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Geometry, EditorGUIUtility.TrTextContent("Geometry", "Controls the vertex precision. Lower values creates more vertex jitter + snapping."));
            PropertyField(m_Color, EditorGUIUtility.TrTextContent("Color", "Controls the color precision. Lower values creates more color banding along gradients."));
            PropertyField(m_FramebufferDitherIsEnabled, EditorGUIUtility.TrTextContent("Framebuffer Dither", "If enabled, framebuffer banding will be dithered between using framebufferDitherTex specified in the PSXRenderPipelineResources."));
            PropertyField(m_DrawDistance, EditorGUIUtility.TrTextContent("Draw Distance", "Controls the max distance (in meters) that triangles will render."));
        }
    }
}