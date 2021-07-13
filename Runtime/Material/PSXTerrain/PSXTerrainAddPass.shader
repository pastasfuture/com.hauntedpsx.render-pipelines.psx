Shader "Hidden/PSX/PSXTerrain (Add Pass)"
{
    Properties
    {
        // Layer count is passed down to guide height-blend enable/disable, due
        // to the fact that heigh-based blend will be broken with multipass.
        [HideInInspector] [PerRendererData] _NumLayersCount ("Total Layer Count", Float) = 1.0

        // set by terrain engine
        // Note, in AddPass, these Splat layers need to default to "white"
        // in TerrainShader.shader, these need to default to grey.
        [HideInInspector] _Control("Control (RGBA)", 2D) = "red" {}
        [HideInInspector] _Splat3("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _Splat2("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat1("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat0("Layer 0 (R)", 2D) = "white" {}

        // used in fallback on old cards & base map
        [HideInInspector] _MainTex("BaseMap (RGB)", 2D) = "grey" {}
        [HideInInspector] _BaseColor("Main Color", Color) = (1,1,1,1)

        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}

        // PSX Standard Material Parameters:
        _AffineTextureWarpingWeight("_AffineTextureWarpingWeight", Float) = 1.0
        _PrecisionGeometryWeight("_PrecisionGeometryWeight", Float) = 1.0
        _PrecisionGeometryOverrideMode("_PrecisionGeometryOverrideMode", Float) = 0.0
        _PrecisionGeometryOverrideParameters("_PrecisionGeometryOverrideParameters", Vector) = (0, 0, 0, 0)
        _PrecisionColorOverrideMode("_PrecisionColorOverrideMode", Float) = 0.0
        _PrecisionColorOverrideParameters("_PrecisionColorOverrideParameters", Vector) = (0, 0, 0, 0)
        _FogWeight("_FogWeight", Float) = 1.0

        // C# side material state tracking.
        [HideInInspector] _TextureFilterMode("__textureFilterMode", Float) = 0.0
        // [HideInInspector] _VertexColorMode("__vertexColorMode", Float) = 0.0
        [HideInInspector] _RenderQueueCategory("__renderQueueCategory", Float) = 0.0
        [HideInInspector] _LightingMode("__lightingMode", Float) = 0.0
        [HideInInspector] _LightingBaked("__lightingBaked", Float) = 1.0
        [HideInInspector] _LightingDynamic("__lightingDynamic", Float) = 1.0
        [HideInInspector] _ShadingEvaluationMode("__shadingEvaluationMode", Float) = 0.0
        [HideInInspector] _BRDFMode("__brdfMode", Float) = 0.0
        // [HideInInspector] _Surface("__surface", Float) = 0.0
        // [HideInInspector] _Blend("__blend", Float) = 0.0
        // [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        // [HideInInspector] _BlendOp("__blendOp", Float) = 0.0
        // [HideInInspector] _SrcBlend("__src", Float) = 1.0
        // [HideInInspector] _DstBlend("__dst", Float) = 0.0
        // [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        // [HideInInspector] _Reflection("__reflection", Float) = 0.0
    }

    HLSLINCLUDE

    #pragma multi_compile_fragment __ _ALPHATEST_ON

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "PSXLit" }
        LOD 100

        Pass
        {
            Name "PSXLit"
            Tags { "LightMode" = "PSXLit" }

            Cull[_Cull]
            Blend One One // Additive mode for AddPass, Disabled in PSXTerrain.shader
            ZWrite Off
            ZTest Equal // Less | Greater | LEqual | GEqual | Equal | NotEqual | Always
            
            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #pragma vertex TerrainLitPassVert
            #pragma fragment TerrainLitPassFrag

            // -------------------------------------
            // Global Keywords (set by render pipeline)
            #pragma multi_compile _OUTPUT_LDR _OUTPUT_HDR
            #pragma multi_compile _FOG_COLOR_LUT_MODE_DISABLED _FOG_COLOR_LUT_MODE_TEXTURE2D_DISTANCE_AND_HEIGHT _FOG_COLOR_LUT_MODE_TEXTURECUBE

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS _TEXTURE_FILTER_MODE_POINT _TEXTURE_FILTER_MODE_POINT_MIPMAPS _TEXTURE_FILTER_MODE_N64 _TEXTURE_FILTER_MODE_N64_MIPMAPS
            // #pragma shader_feature _ _VERTEX_COLOR_MODE_COLOR _VERTEX_COLOR_MODE_LIGHTING
            #pragma shader_feature _LIGHTING_BAKED_ON
            #pragma shader_feature _LIGHTING_DYNAMIC_ON
            #pragma shader_feature _SHADING_EVALUATION_MODE_PER_VERTEX _SHADING_EVALUATION_MODE_PER_PIXEL _SHADING_EVALUATION_MODE_PER_OBJECT
            #pragma shader_feature _BRDF_MODE_LAMBERT _BRDF_MODE_WRAPPED_LIGHTING
            // #pragma shader_feature _EMISSION
            // #pragma shader_feature _ALPHATEST_ON
            // #pragma shader_feature _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            // #pragma shader_feature _REFLECTION_ON
            #pragma shader_feature _FOG_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #define TERRAIN_SPLAT_ADDPASS

            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXTerrain/PSXTerrainInput.hlsl"
            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXTerrain/PSXTerrainPass.hlsl"
            ENDHLSL
        }
    }
}
