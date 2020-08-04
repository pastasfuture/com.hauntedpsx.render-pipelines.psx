using UnityEditor;
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
        SerializedDataParameter m_Chroma;
        SerializedDataParameter m_Alpha;
        SerializedDataParameter m_AffineTextureWarping;
        SerializedDataParameter m_FramebufferDither;
        SerializedDataParameter m_DrawDistanceFalloffMode;
        SerializedDataParameter m_DrawDistance;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<PrecisionVolume>(serializedObject);
            m_Geometry = Unpack(o.Find(x => x.geometry));
            m_Color = Unpack(o.Find(x => x.color));
            m_Chroma = Unpack(o.Find(x => x.chroma));
            m_Alpha = Unpack(o.Find(x => x.alpha));
            m_AffineTextureWarping = Unpack(o.Find(x => x.affineTextureWarping));
            m_FramebufferDither = Unpack(o.Find(x => x.framebufferDither));
            m_DrawDistanceFalloffMode = Unpack(o.Find(x => x.drawDistanceFalloffMode));
            m_DrawDistance = Unpack(o.Find(x => x.drawDistance));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Geometry, EditorGUIUtility.TrTextContent("Geometry", "Controls the vertex precision. Lower values creates more vertex jitter + snapping."));
            PropertyField(m_Color, EditorGUIUtility.TrTextContent("Color", "Controls the color precision. Lower values creates more color banding along gradients."));
            PropertyField(m_Chroma, EditorGUIUtility.TrTextContent("Chroma", "Controls the amount of chroma shift that is visible within color precision banding steps. A value of 1.0 adds precision in the green channel, useful for simulating a 5:6:5 style color space. This also means grayscale values will always have a chroma tint, creating a grungier look. A value of 0.0 gives you consistent precision across R, G and B channels, grayscale values with no chroma tint."));
            PropertyField(m_Alpha, EditorGUIUtility.TrTextContent("Alpha", "Controls the alpha precision. Lower values creates more alpha (opacity) banding along fades."));
            PropertyField(m_AffineTextureWarping, EditorGUIUtility.TrTextContent("Affine Texture Warping", "Controls the amount of affine texture warping to apply.\nA value of 1.0 creates the most warping, and is most accurate to PSX hardware.\nA value of 0.0 creates no warping, uvs are perspective correct."));
            PropertyField(m_FramebufferDither, EditorGUIUtility.TrTextContent("Framebuffer Dither", "Controls the amount of dither applied between banding steps.\nA value of 1.0 fully breaks up banding (at the cost of dither noise).\nA value of 0.0 applies no dither, so banding artifacts from low precision color / alpha are fully visible.\nFramebuffer banding will be dithered between using framebufferDitherTex specified in the PSXRenderPipelineResources."));
            PropertyField(m_DrawDistanceFalloffMode, EditorGUIUtility.TrTextContent("Draw Distance Falloff Mode", "Controls the shape of the draw distance clipping."));
            PropertyField(m_DrawDistance, EditorGUIUtility.TrTextContent("Draw Distance", "Controls the max distance (in meters) that triangles will render."));
        }
    }
}