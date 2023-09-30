using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/TerrainGrassVolume")]
    public class TerrainGrassVolume : VolumeComponent
    {
        // TODO: This type was copied from the editor-only PSXMaterialUtils static class.
        // Should look into creating a runtime static class for these types to be shared across editor and runtime (i.e: PSXMaterialTypes? PSXTypes?)
        [Serializable]
        public enum TextureFilterMode
        {
            TextureImportSettings = 0,
            Point,
            PointMipmaps,
            N64,
            N64Mipmaps
        };

        [Serializable]
        public sealed class TextureFilterModeParameter : VolumeParameter<TextureFilterMode>
        {
            public TextureFilterModeParameter(TextureFilterMode value, bool overrideState = false)
                : base(value, overrideState) { }
        }

        public TextureFilterModeParameter textureFilterMode = new TextureFilterModeParameter(TextureFilterMode.TextureImportSettings);

        static TerrainGrassVolume s_Default = null;
        public static TerrainGrassVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<TerrainGrassVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}