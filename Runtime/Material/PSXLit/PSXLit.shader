Shader "PSX/PSXLit"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _MainColor("MainColor", Color) = (1,1,1,1)
        _EmissionTexture("EmissionTexture", 2D) = "white" {}
        _EmissionColor("EmissionColor", Color) = (0,0,0,0)
        _EmissionBakedMultiplier("EmissionBakedMultiplier", Float) = 1.0
        _AlphaClippingDitherIsEnabled("_AlphaClippingDitherIsEnabled", Float) = 0.0
        _AlphaClippingScaleBiasMinMax("_AlphaClippingScaleBiasMinMax", Vector) = (1.0, 0.0, 0.0, 1.0)
        _AffineTextureWarpingWeight("_AffineTextureWarpingWeight", Float) = 1.0
        _PrecisionGeometryWeight("_PrecisionGeometryWeight", Float) = 1.0
        _FogWeight("_FogWeight", Float) = 1.0
        _DrawDistanceOverrideMode("_DrawDistanceOverrideMode", Int) = 0
        _DrawDistanceOverride("_DrawDistanceOverride", Vector) = (100, 10000, 0, 0)
        _ReflectionCubemap("_ReflectionCubemap", Cube) = "black" {}
        _ReflectionTexture("_ReflectionTexture", 2D) = "white" {}
        _ReflectionColor("_ReflectionColor", Color) = (1,1,1,1)
        _ReflectionBlendMode("_ReflectionBlendMode", Int) = 0
        [HideInInspector] _DoubleSidedConstants("_DoubleSidedConstants", Vector) = (1, 1, 1, 0)

        // C# side material state tracking.
        [HideInInspector] _TextureFilterMode("__textureFilterMode", Float) = 0.0
        [HideInInspector] _VertexColorMode("__vertexColorMode", Float) = 0.0
        [HideInInspector] _RenderQueueCategory("__renderQueueCategory", Float) = 0.0
        [HideInInspector] _LightingMode("__lightingMode", Float) = 0.0
        [HideInInspector] _LightingBaked("__lightingBaked", Float) = 1.0
        [HideInInspector] _LightingDynamic("__lightingDynamic", Float) = 1.0
        [HideInInspector] _ShadingEvaluationMode("__shadingEvaluationMode", Float) = 0.0
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _BlendOp("__blendOp", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
        [HideInInspector] _ColorMask("__colorMask", Float) = 15.0 // UnityEngine.Rendering.ColorWriteMask.All
        [HideInInspector] _Reflection("__reflection", Float) = 0.0
        [HideInInspector] _DoubleSidedNormalMode("__doubleSidedNormalMode", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType" = "PSXLit" }
        LOD 100

        Pass
        {
            Name "PSXLit"
            Tags { "LightMode" = "PSXLit" }

            BlendOp[_BlendOp]
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]
            ColorMask[_ColorMask]

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            // -------------------------------------
            // Global Keywords (set by render pipeline)
            #pragma multi_compile _OUTPUT_LDR _OUTPUT_HDR

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS _TEXTURE_FILTER_MODE_POINT _TEXTURE_FILTER_MODE_POINT_MIPMAPS _TEXTURE_FILTER_MODE_N64 _TEXTURE_FILTER_MODE_N64_MIPMAPS
            #pragma shader_feature _ _VERTEX_COLOR_MODE_COLOR _VERTEX_COLOR_MODE_LIGHTING _VERTEX_COLOR_MODE_COLOR_BACKGROUND
            #pragma shader_feature _LIGHTING_BAKED_ON
            #pragma shader_feature _LIGHTING_DYNAMIC_ON
            #pragma shader_feature _SHADING_EVALUATION_MODE_PER_VERTEX _SHADING_EVALUATION_MODE_PER_PIXEL
            #pragma shader_feature _EMISSION
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature _REFLECTION_ON
            #pragma shader_feature _FOG_ON
            #pragma shader_feature _DOUBLE_SIDED_ON
            #pragma shader_feature _BLENDMODE_TONEMAPPER_OFF

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXLit/PSXLitInput.hlsl"
            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXLit/PSXLitPass.hlsl"            
            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ _VERTEX_COLOR_MODE_COLOR _VERTEX_COLOR_MODE_LIGHTING _VERTEX_COLOR_MODE_COLOR_BACKGROUND
            #pragma shader_feature _EMISSION
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON

            #pragma vertex LitMetaPassVertex
            #pragma fragment LitMetaPassFragment

            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXLit/PSXLitInput.hlsl"
            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXLit/PSXLitMetaPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "SceneSelectionPass"
            Tags { "LightMode" = "SceneSelectionPass" }

            BlendOp[_BlendOp]
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #define SCENESELECTIONPASS // This will drive the output of the scene selection shader
            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXLit/PSXLitInput.hlsl"
            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXLit/PSXLitPass.hlsl"            
            ENDHLSL
        }
    }

    CustomEditor "HauntedPSX.RenderPipelines.PSX.Editor.PSXLitShaderGUI"
}
