Shader "Hidden/TerrainEngine/Details/PSX/WavingDoublePass"
{
    Properties
    {
        _WavingTint ("Fade Color", Color) = (.7,.6,.5, 0)
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
        _WaveAndDistance ("Wave and distance", Vector) = (12, 3.6, 1, 1)
        _Cutoff ("Cutoff", float) = 0.5
    }
    SubShader
    {
        // Tags {"Queue" = "Geometry+200" "RenderType" = "Grass" "IgnoreProjector" = "True" }//"DisableBatching"="True"
        Tags { "RenderType" = "PSXLit" }
        
        Cull Off
        LOD 200
        AlphaTest Greater [_Cutoff]
        ColorMask RGB

        Pass
        {
            Tags { "LightMode" = "PSXLit" }

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Global Keywords (set by render pipeline)
            #pragma multi_compile _OUTPUT_LDR _OUTPUT_HDR
            #pragma multi_compile _FOG_COLOR_LUT_MODE_DISABLED _FOG_COLOR_LUT_MODE_TEXTURE2D_DISTANCE_AND_HEIGHT _FOG_COLOR_LUT_MODE_TEXTURECUBE

            // -------------------------------------
            // Force Enabled Material Keywords
            // Grass shader has no method of assigning a material for toggling the keywords that would normally be user
            // exposed such as in the PSXLit material.
            // For now, simply enable all features we believe are relevant (at a performance cost).
            // #pragma shader_feature _ _VERTEX_COLOR_MODE_COLOR _VERTEX_COLOR_MODE_LIGHTING
            #define _LIGHTING_BAKED_ON
            #define _LIGHTING_DYNAMIC_ON
            // #define _SHADING_EVALUATION_MODE_PER_VERTEX
            #define _SHADING_EVALUATION_MODE_PER_PIXEL
            // #pragma shader_feature _EMISSION
            // #pragma shader_feature _ALPHATEST_ON
            // #pragma shader_feature _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            // #pragma shader_feature _REFLECTION_ON
            #define _FOG_ON

            #pragma shader_feature _TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS _TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT _TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS _TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64 _TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS
            
            #if defined(_TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS)
                #define _TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS
            #elif defined(_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT)
                #define _TEXTURE_FILTER_MODE_POINT
            #elif defined(_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS)
                #define _TEXTURE_FILTER_MODE_POINT_MIPMAPS
            #elif defined(_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64)
                #define _TEXTURE_FILTER_MODE_N64
            #elif defined(_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS)
                #define _TEXTURE_FILTER_MODE_N64_MIPMAPS
            #else
                #define _TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS
            #endif

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex WavingGrassVert
            #pragma fragment LitPassFragmentGrass

            // Force Enabled Material Keywords
            // Grass shader has no method of assigning a material for toggling the keywords that would normally be user
            // exposed such as in the PSXLit material.
            // For now, simply enable all features we believe are relevant (at a performance cost).
            #define _ALPHATEST_ON
            #define _BRDF_MODE_LAMBERT

            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXTerrain/PSXWavingGrassInput.hlsl"
            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXTerrain/PSXWavingGrassPasses.hlsl"

            ENDHLSL
        }

        Pass
        {
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Global Keywords (set by render pipeline)
            #pragma multi_compile _OUTPUT_LDR _OUTPUT_HDR

            // -------------------------------------
            // Force Enabled Material Keywords
            // Grass shader has no method of assigning a material for toggling the keywords that would normally be user
            // exposed such as in the PSXLit material.
            // For now, simply enable all features we believe are relevant (at a performance cost).
            // #pragma shader_feature _ _VERTEX_COLOR_MODE_COLOR _VERTEX_COLOR_MODE_LIGHTING
            // #define _LIGHTING_BAKED_ON
            // #define _LIGHTING_DYNAMIC_ON
            // #define _SHADING_EVALUATION_MODE_PER_VERTEX
            // #define _SHADING_EVALUATION_MODE_PER_PIXEL
            // #pragma shader_feature _EMISSION
            // #pragma shader_feature _ALPHATEST_ON
            // #pragma shader_feature _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            // #pragma shader_feature _REFLECTION_ON
            // #define _FOG_ON

            #pragma shader_feature _TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS _TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT _TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS _TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64 _TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS

            #if defined(_TERRAIN_GRASS_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS)
                #define _TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS
            #elif defined(_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT)
                #define _TEXTURE_FILTER_MODE_POINT
            #elif defined(_TERRAIN_GRASS_TEXTURE_FILTER_MODE_POINT_MIPMAPS)
                #define _TEXTURE_FILTER_MODE_POINT_MIPMAPS
            #elif defined(_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64)
                #define _TEXTURE_FILTER_MODE_N64
            #elif defined(_TERRAIN_GRASS_TEXTURE_FILTER_MODE_N64_MIPMAPS)
                #define _TEXTURE_FILTER_MODE_N64_MIPMAPS
            #else
                #define _TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS
            #endif

            // -------------------------------------
            // Material Keywords
            #define _ALPHATEST_ON
            #define _BRDF_MODE_LAMBERT
            // #pragma shader_feature _GLOSSINESS_FROM_BASE_ALPHA

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXTerrain/PSXWavingGrassInput.hlsl"
            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXTerrain/PSXWavingGrassPasses.hlsl"
            ENDHLSL
        }
    }
}
