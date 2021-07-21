using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    public class PSXRenderPipelineResources : ScriptableObject
    {
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Runtime/PostProcessing/Shaders/Sky.shader")]
            public Shader skyPS;
            [Reload("Runtime/PostProcessing/Shaders/AccumulationMotionBlur.shader")]
            public Shader accumulationMotionBlurPS;
            [Reload("Runtime/PostProcessing/Shaders/CopyColorRespectFlipY.shader")]
            public Shader copyColorRespectFlipYPS;
            [Reload("Runtime/PostProcessing/Shaders/CRT.shader")]
            public Shader crtPS;
            [Reload("Runtime/PostProcessing/Shaders/Compression.compute")]
            public ComputeShader compressionCS;

            [Reload("Runtime/Material/PSXTerrain/PSXTerrainDetail.shader")]
            public Shader terrainDetailLitPS;

            [Reload("Runtime/Material/PSXTerrain/PSXWavingGrass.shader")]
            public Shader terrainDetailGrassPS;

            [Reload("Runtime/Material/PSXTerrain/PSXWavingGrassBillboard.shader")]
            public Shader terrainDetailGrassBillboardPS;
        }

        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
            // Pre-baked noise
            [Reload("Runtime/RenderPipelineResources/Texture/WhiteNoise1024RGB.png", 0, 2)]
            public Texture2D[] whiteNoise1024RGBTex;

            [Reload("Runtime/RenderPipelineResources/Texture/Bayer/BayerL4x4.png", 0, 2)]
            public Texture2D[] framebufferDitherTex;

            [Reload("Runtime/RenderPipelineResources/Texture/Bayer/BayerL4x4.png", 0, 2)]
            public Texture2D[] alphaClippingDitherTex;

            [Reload("Runtime/RenderPipelineResources/Texture/BlueNoise16/L/LDR_LLL1_{0}.png", 0, 32)]
            public Texture2D[] blueNoise16LTex;
            
            [Reload("Runtime/RenderPipelineResources/Texture/BlueNoise16/RGB/LDR_RGB1_{0}.png", 0, 32)]
            public Texture2D[] blueNoise16RGBTex;

            [Reload("Runtime/RenderPipelineResources/Texture/SkyboxTextureCubeDefault.exr")]
            public Texture skyboxTextureCubeDefault;
        }

        [Serializable, ReloadGroup]
        public sealed class MaterialResources
        {
            [Reload("Runtime/RenderPipelineResources/Material/DefaultOpaqueMat.mat")]
            public Material defaultOpaqueMat;
        }

        public ShaderResources shaders;
        public TextureResources textures;
        public MaterialResources materials;
    }
}
