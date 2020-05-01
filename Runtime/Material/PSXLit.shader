Shader "PSX/PSXLit"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _MainColor("MainColor", Color) = (1,1,1,1)
        _EmissionTexture("EmissionTexture", 2D) = "white" {}
        _EmissionColor("EmissionColor", Color) = (0,0,0,0)
        _AlphaClippingDitherIsEnabled("_AlphaClippingDitherIsEnabled", Float) = 0.0

        // Blending state
        [HideInInspector] _LightingMode("__lightingMode", Float) = 0.0
        [HideInInspector] _LightingBaked("__lightingBaked", Float) = 1.0
        [HideInInspector] _LightingVertexColor("__lightingVertexColor", Float) = 0.0
        [HideInInspector] _LightingDynamic("__lightingDynamic", Float) = 1.0
        [HideInInspector] _ShadingEvaluationMode("__shadingEvaluationMode", Float) = 0.0
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType" = "PSXLit" }
        LOD 100

        Pass
        {
            Name "PSXLit"
            Tags { "LightMode" = "PSXLit" }

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
            #pragma shader_feature _LIGHTING_BAKED_ON
            #pragma shader_feature _LIGHTING_VERTEX_COLOR_ON
            #pragma shader_feature _LIGHTING_DYNAMIC_ON
            #pragma shader_feature _SHADING_EVALUATION_MODE_PER_VERTEX _SHADING_EVALUATION_MODE_PER_PIXEL
            #pragma shader_feature _EMISSION
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXLitPass.hlsl"            
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
            #pragma shader_feature _EMISSION
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON

            #pragma vertex LitMetaPassVertex
            #pragma fragment LitMetaPassFragment

            #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXLitMetaPass.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "HauntedPSX.RenderPipelines.PSX.Editor.PSXLitShaderGUI"
}
