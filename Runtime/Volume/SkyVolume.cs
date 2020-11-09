using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    [Serializable, VolumeComponentMenu("HauntedPS1/SkyVolume")]
    public class SkyVolume : VolumeComponent
    {
        [Serializable]
        public enum SkyMode
        {
            FogColor = 0,
            BackgroundColor,
            Skybox,
            TiledLayers
        };

        [Serializable]
        public sealed class SkyModeParameter : VolumeParameter<SkyMode>
        {
            public SkyModeParameter(SkyMode value, bool overrideState = false)
                : base(value, overrideState) {}
        }

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
                : base(value, overrideState) {}
        }

        public SkyModeParameter skyMode = new SkyModeParameter(SkyMode.FogColor);
        public ClampedFloatParameter framebufferDitherWeight = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public Vector3Parameter skyRotation = new Vector3Parameter(Vector3.zero);
        public TextureFilterModeParameter textureFilterMode = new TextureFilterModeParameter(TextureFilterMode.TextureImportSettings);
        
        public TextureParameter skyboxTexture = new TextureParameter(null);

        public MinFloatParameter tiledLayersSkyHeightScale = new MinFloatParameter(0.25f, 0.0f);
        public ClampedFloatParameter tiledLayersSkyHorizonOffset = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        public ColorParameter tiledLayersSkyColorLayer0 = new ColorParameter(Color.white, hdr: false, showAlpha: true, showEyeDropper: true);
        public TextureParameter tiledLayersSkyTextureLayer0 = new TextureParameter(null);
        public Vector4Parameter tiledLayersSkyTextureScaleOffsetLayer0 = new Vector4Parameter(new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
        public FloatParameter tiledLayersSkyRotationLayer0 = new FloatParameter(0.0f);
        public Vector2Parameter tiledLayersSkyScrollScaleLayer0 = new Vector2Parameter(new Vector2(0.125f, 0.125f));
        public FloatParameter tiledLayersSkyScrollRotationLayer0 = new FloatParameter(0.0f);
        public ColorParameter tiledLayersSkyColorLayer1 = new ColorParameter(Color.white, hdr: false, showAlpha: true, showEyeDropper: true);
        public TextureParameter tiledLayersSkyTextureLayer1 = new TextureParameter(null);
        public Vector4Parameter tiledLayersSkyTextureScaleOffsetLayer1 = new Vector4Parameter(new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
        public FloatParameter tiledLayersSkyRotationLayer1 = new FloatParameter(0.0f);
        public Vector2Parameter tiledLayersSkyScrollScaleLayer1 = new Vector2Parameter(new Vector2(-0.25f, -0.25f));
        public FloatParameter tiledLayersSkyScrollRotationLayer1 = new FloatParameter(0.0f);


        static SkyVolume s_Default = null;
        public static SkyVolume @default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = ScriptableObject.CreateInstance<SkyVolume>();
                    s_Default.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_Default;
            }
        }
    }
}