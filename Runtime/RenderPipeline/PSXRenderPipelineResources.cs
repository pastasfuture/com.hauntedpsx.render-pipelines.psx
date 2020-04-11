using System;
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
            [Reload("Runtime/PostProcessing/Shaders/CRT.shader")]
            public Shader crtPS;
        }

        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
            // Pre-baked noise
            [Reload("Runtime/RenderPipelineResources/Texture/WhiteNoise1024RGB.png")]
            public Texture2D[] whiteNoise1024RGBTex;

            [Reload("Runtime/RenderPipelineResources/Texture/BlueNoise16/L/LDR_LLL1_{0}.png", 0, 32)]
            public Texture2D[] framebufferDitherTex;

            [Reload("Runtime/RenderPipelineResources/Texture/BlueNoise16/L/LDR_LLL1_{0}.png", 0, 32)]
            public Texture2D[] blueNoise16LTex;
            
            [Reload("Runtime/RenderPipelineResources/Texture/BlueNoise16/RGB/LDR_RGB1_{0}.png", 0, 32)]
            public Texture2D[] blueNoise16RGBTex;
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

#if UNITY_EDITOR
    [ExecuteInEditMode]
    static class PSXRenderPipelineResourcesFactory
    {
        static readonly string s_DefaultPath = "Assets/PSXRenderPipelineResources.asset";

        [UnityEditor.MenuItem("HauntedPS1/Create HauntedPS1 Render Pipeline Resources")]
        static void CreatePSXRenderPipelineAsset()
        {
            var newAsset = ScriptableObject.CreateInstance<PSXRenderPipelineResources>();
            ResourceReloader.ReloadAllNullIn(newAsset, PSXStringConstants.s_PackagePath);
            UnityEditor.AssetDatabase.CreateAsset(newAsset, s_DefaultPath);
        }
    }
#endif
}
