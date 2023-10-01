using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
#if UNITY_2022_1_OR_NEWER
    [CustomEditor(typeof(TerrainGrassVolume))]
#else
    [VolumeComponentEditor(typeof(TerrainGrassVolume))]
#endif
    public class TerrainGrassVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_TextureFilterMode;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<TerrainGrassVolume>(serializedObject);
            m_TextureFilterMode = Unpack(o.Find(x => x.textureFilterMode));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_TextureFilterMode, EditorGUIUtility.TrTextContent("Texture Filter Mode", "Controls how the Terrain Grass textures are filtered.\nTextureFilterMode.TextureImportSettings is the standard unity behavior. Textures will be filtered using the texture's import settings.\nTextureFilterMode.Point will force PSX era nearest neighbor point sampling, regardless of texture import settings.\nTextureFilterMode.PointMipmaps is the same as TextureFilterMode.Point but supports supports point sampled lods via the texture's mipmap chain.\nTextureFilterMode.N64 will force N64 era 3-point barycentric texture filtering.\nTextureFilterMode.N64MipMaps is the same as TextureFilterMode.N64 but supports N64 sampled lods via the texture's mipmap chain."));
        }
    }
}