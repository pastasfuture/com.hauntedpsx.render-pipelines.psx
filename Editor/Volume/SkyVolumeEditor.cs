using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
#if UNITY_2022_1_OR_NEWER
    [CustomEditor(typeof(SkyVolume))]
#else
    [VolumeComponentEditor(typeof(SkyVolume))]
#endif
    public class SkyVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_SkyMode;
        SerializedDataParameter m_FramebufferDitherWeight;
        SerializedDataParameter m_SkyRotation;
        SerializedDataParameter m_TextureFilterMode;

        SerializedDataParameter m_SkyboxTexture;

        SerializedDataParameter m_TiledLayersSkyHeightScale;
        SerializedDataParameter m_TiledLayersSkyHorizonOffset;
        SerializedDataParameter m_TiledLayersSkyColorLayer0;
        SerializedDataParameter m_TiledLayersSkyTextureLayer0;
        SerializedDataParameter m_TiledLayersSkyTextureScaleOffsetLayer0;
        SerializedDataParameter m_TiledLayersSkyRotationLayer0;
        SerializedDataParameter m_TiledLayersSkyScrollScaleLayer0;
        SerializedDataParameter m_TiledLayersSkyScrollRotationLayer0;
        SerializedDataParameter m_TiledLayersSkyColorLayer1;
        SerializedDataParameter m_TiledLayersSkyTextureLayer1;
        SerializedDataParameter m_TiledLayersSkyTextureScaleOffsetLayer1;
        SerializedDataParameter m_TiledLayersSkyRotationLayer1;
        SerializedDataParameter m_TiledLayersSkyScrollScaleLayer1;
        SerializedDataParameter m_TiledLayersSkyScrollRotationLayer1;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SkyVolume>(serializedObject);

            m_SkyMode = Unpack(o.Find(x => x.skyMode));
            m_FramebufferDitherWeight = Unpack(o.Find(x => x.framebufferDitherWeight));
            m_SkyRotation = Unpack(o.Find(x => x.skyRotation));
            m_TextureFilterMode = Unpack(o.Find(x => x.textureFilterMode));

            m_SkyboxTexture = Unpack(o.Find(x => x.skyboxTexture));

            m_TiledLayersSkyHeightScale = Unpack(o.Find(x => x.tiledLayersSkyHeightScale));
            m_TiledLayersSkyHorizonOffset = Unpack(o.Find(x => x.tiledLayersSkyHorizonOffset));
            m_TiledLayersSkyColorLayer0 = Unpack(o.Find(x => x.tiledLayersSkyColorLayer0));
            m_TiledLayersSkyTextureLayer0 = Unpack(o.Find(x => x.tiledLayersSkyTextureLayer0));
            m_TiledLayersSkyTextureScaleOffsetLayer0 = Unpack(o.Find(x => x.tiledLayersSkyTextureScaleOffsetLayer0));
            m_TiledLayersSkyRotationLayer0 = Unpack(o.Find(x => x.tiledLayersSkyRotationLayer0));
            m_TiledLayersSkyScrollScaleLayer0 = Unpack(o.Find(x => x.tiledLayersSkyScrollScaleLayer0));
            m_TiledLayersSkyScrollRotationLayer0 = Unpack(o.Find(x => x.tiledLayersSkyScrollRotationLayer0));

            m_TiledLayersSkyColorLayer1 = Unpack(o.Find(x => x.tiledLayersSkyColorLayer1));
            m_TiledLayersSkyTextureLayer1 = Unpack(o.Find(x => x.tiledLayersSkyTextureLayer1));
            m_TiledLayersSkyTextureScaleOffsetLayer1 = Unpack(o.Find(x => x.tiledLayersSkyTextureScaleOffsetLayer1));
            m_TiledLayersSkyRotationLayer1 = Unpack(o.Find(x => x.tiledLayersSkyRotationLayer1));
            m_TiledLayersSkyScrollScaleLayer1 = Unpack(o.Find(x => x.tiledLayersSkyScrollScaleLayer1));
            m_TiledLayersSkyScrollRotationLayer1 = Unpack(o.Find(x => x.tiledLayersSkyScrollRotationLayer1));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_SkyMode, EditorGUIUtility.TrTextContent("Sky Mode", "Controls the mode the sky is rendered in.\nFog Color fills the sky with the fog color defined in the Fog Volume. Useful for smooth blending between fog and sky.\nBackground Color fills the sky with the background color defined on your Camera.\nSkybox fills the sky with the Skybox Texture specified on this SkyVolume.\nTiled Layers renders the sky using two 2D textures, projected onto an ellipsoid, tiled, scrolled, and blended. Useful for emulating Quake-era skys."));
            PropertyField(m_FramebufferDitherWeight, EditorGUIUtility.TrTextContent("Framebuffer Dither Weight", "Controls the amount of dither applied to the sky between precision steps. A value of 0.0 will apply no dither to the sky. A value of 1.0 will apply the maximum amount of dither specified in the PrecisionVolume settings."));

            if (m_SkyMode.value.intValue == (int)SkyVolume.SkyMode.Skybox
                || m_SkyMode.value.intValue == (int)SkyVolume.SkyMode.TiledLayers)
            {
                PropertyField(m_SkyRotation, EditorGUIUtility.TrTextContent("Sky Rotation", "Rotation of sky in euler degrees."));
            }

            // TODO: Currently, TextureFilterMode is only supported on TiledLayers.
            if (m_SkyMode.value.intValue == (int)SkyVolume.SkyMode.TiledLayers)
            {
                PropertyField(m_TextureFilterMode, EditorGUIUtility.TrTextContent("Texture Filter Mode", "Controls how the Sky textures are filtered.\nTextureFilterMode.TextureImportSettings is the standard unity behavior. Textures will be filtered using the texture's import settings.\nTextureFilterMode.Point will force PSX era nearest neighbor point sampling, regardless of texture import settings.\nTextureFilterMode.PointMipmaps is the same as TextureFilterMode.Point but supports supports point sampled lods via the texture's mipmap chain.\nTextureFilterMode.N64 will force N64 era 3-point barycentric texture filtering.\nTextureFilterMode.N64MipMaps is the same as TextureFilterMode.N64 but supports N64 sampled lods via the texture's mipmap chain."));
            }

            if (m_SkyMode.value.intValue == (int)SkyVolume.SkyMode.Skybox)
            {
                PropertyField(m_SkyboxTexture, EditorGUIUtility.TrTextContent("Skybox Texture", "Accepts Cubemap Textures"));

                if (m_SkyboxTexture.value.objectReferenceValue != null)
                {
                    Texture skyboxTexture = m_SkyboxTexture.value.objectReferenceValue as Texture;
                    if (skyboxTexture.dimension != TextureDimension.Cube)
                    {
                        m_SkyboxTexture.value.objectReferenceValue = null;
                    }
                }
            }
            else if (m_SkyMode.value.intValue == (int)SkyVolume.SkyMode.TiledLayers)
            {
                PropertyField(m_TiledLayersSkyHeightScale, EditorGUIUtility.TrTextContent("Sky Height Scale"));
                PropertyField(m_TiledLayersSkyHorizonOffset, EditorGUIUtility.TrTextContent("Sky Horizon Offset"));

                PropertyField(m_TiledLayersSkyColorLayer0, EditorGUIUtility.TrTextContent("Sky Color Layer 0"));
                PropertyField(m_TiledLayersSkyTextureLayer0, EditorGUIUtility.TrTextContent("Sky Texture Layer 0"));
                if (m_TiledLayersSkyTextureLayer0.value.objectReferenceValue != null)
                {
                    Texture tiledLayersSkyTextureLayer0 = m_TiledLayersSkyTextureLayer0.value.objectReferenceValue as Texture;
                    if (tiledLayersSkyTextureLayer0.dimension != TextureDimension.Tex2D)
                    {
                        m_TiledLayersSkyTextureLayer0.value.objectReferenceValue = null;
                    }
                }
                PropertyField(m_TiledLayersSkyTextureScaleOffsetLayer0, EditorGUIUtility.TrTextContent("Sky Texture Scale Offset Layer 0"));
                PropertyField(m_TiledLayersSkyRotationLayer0, EditorGUIUtility.TrTextContent("Sky Rotation Layer 0"));
                PropertyField(m_TiledLayersSkyScrollScaleLayer0, EditorGUIUtility.TrTextContent("Sky Scroll Scale Layer 0"));
                PropertyField(m_TiledLayersSkyScrollRotationLayer0, EditorGUIUtility.TrTextContent("Sky Scroll Rotation Layer 0"));

                PropertyField(m_TiledLayersSkyColorLayer1, EditorGUIUtility.TrTextContent("Sky Color Layer 1"));
                PropertyField(m_TiledLayersSkyTextureLayer1, EditorGUIUtility.TrTextContent("Sky Texture Layer 1"));
                if (m_TiledLayersSkyTextureLayer1.value.objectReferenceValue != null)
                {
                    Texture tiledLayersSkyTextureLayer1 = m_TiledLayersSkyTextureLayer1.value.objectReferenceValue as Texture;
                    if (tiledLayersSkyTextureLayer1.dimension != TextureDimension.Tex2D)
                    {
                        m_TiledLayersSkyTextureLayer1.value.objectReferenceValue = null;
                    }
                }
                PropertyField(m_TiledLayersSkyTextureScaleOffsetLayer1, EditorGUIUtility.TrTextContent("Sky Texture Scale Offset Layer 1"));
                PropertyField(m_TiledLayersSkyRotationLayer1, EditorGUIUtility.TrTextContent("Sky Rotation Layer 1"));
                PropertyField(m_TiledLayersSkyScrollScaleLayer1, EditorGUIUtility.TrTextContent("Sky Scroll Scale Layer 1"));
                PropertyField(m_TiledLayersSkyScrollRotationLayer1, EditorGUIUtility.TrTextContent("Sky Scroll Rotation Layer 1"));
            }

        }
    }
}