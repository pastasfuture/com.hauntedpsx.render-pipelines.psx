Shader "Hidden/PSX/PSXTerrain (Basemap Gen)"
{
    Properties
    {
        // Layer count is passed down to guide height-blend enable/disable, due
        // to the fact that heigh-based blend will be broken with multipass.
        [HideInInspector] [PerRendererData] _NumLayersCount ("Total Layer Count", Float) = 1.0
        [HideInInspector] _Control("AlphaMap", 2D) = "" {}
        
        [HideInInspector] _Splat0 ("Layer 0 (R)", 2D) = "white" {}
        [HideInInspector] _Splat1 ("Layer 1 (G)", 2D) = "white" {}
        [HideInInspector] _Splat2 ("Layer 2 (B)", 2D) = "white" {}
        [HideInInspector] _Splat3 ("Layer 3 (A)", 2D) = "white" {}
        [HideInInspector] _Mask3("Mask 3 (A)", 2D) = "grey" {}
        [HideInInspector] _Mask2("Mask 2 (B)", 2D) = "grey" {}
        [HideInInspector] _Mask1("Mask 1 (G)", 2D) = "grey" {}
        [HideInInspector] _Mask0("Mask 0 (R)", 2D) = "grey" {}        
        [HideInInspector] [Gamma] _Metallic0 ("Metallic 0", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic1 ("Metallic 1", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic2 ("Metallic 2", Range(0.0, 1.0)) = 0.0
        [HideInInspector] [Gamma] _Metallic3 ("Metallic 3", Range(0.0, 1.0)) = 0.0
        [HideInInspector] _Smoothness0 ("Smoothness 0", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness1 ("Smoothness 1", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness2 ("Smoothness 2", Range(0.0, 1.0)) = 1.0
        [HideInInspector] _Smoothness3 ("Smoothness 3", Range(0.0, 1.0)) = 1.0

        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
    }
    
    Subshader
    {
        HLSLINCLUDE
        // Required to compile gles 2.0 with standard srp library
        #pragma prefer_hlslcc gles
        #pragma exclude_renderers d3d11_9x
        #pragma target 3.0
        
        #define _TERRAIN_BASEMAP_GEN

        #pragma shader_feature_local _TERRAIN_BLEND_HEIGHT
        #pragma shader_feature_local _MASKMAP
        
        #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXTerrain/PSXTerrainInput.hlsl"
        #include "Packages/com.hauntedpsx.render-pipelines.psx/Runtime/Material/PSXTerrain/PSXTerrainPass.hlsl"
       
        ENDHLSL
        
        Pass
        {
            Tags
            {
                "Name" = "_MainTex"
                "Format" = "ARGB32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One [_DstBlend]     
            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            
            Varyings Vert(Attributes i)
            {
                Varyings o = (Varyings) 0;
                
                o.vertex = TransformWorldToHClip(i.positionOS.xyz);
                
                // NOTE : This is basically coming from the vertex shader in TerrainLitPasses
                // There are other plenty of other values that the original version computes, but for this
                // pass, we are only interested in a few, so I'm just skipping the rest.
                o.uvw = float3(i.uv, 1.0f);

                return o;
            }
            
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.uvw.xy;
                float4 uvMainAndLM = float4(uv, uv * unity_LightmapST.xy + unity_LightmapST.zw);
                float4 uvSplat01 = float4(TRANSFORM_TEX(uv, _Splat0), TRANSFORM_TEX(uv, _Splat1));
                float4 uvSplat23 = float4(TRANSFORM_TEX(uv, _Splat2), TRANSFORM_TEX(uv, _Splat3));

                half4 splatControl;
                half weight;
                half4 color = 0.0h;
                float2 splatUV = (uvMainAndLM.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
                splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);

                float3 precisionColor;
                float3 precisionColorInverse;
                float precisionColorIndexNormalized = _PrecisionColor.w;
                float precisionChromaBit = _PrecisionColorInverse.w;
                ApplyPrecisionColorOverride(precisionColor, precisionColorInverse, _PrecisionColor.rgb, _PrecisionColorInverse.rgb, precisionColorIndexNormalized, precisionChromaBit, _PrecisionColorOverrideMode, _PrecisionColorOverrideParameters);
                          
                SplatmapMix(uvMainAndLM, uvSplat01, uvSplat23, precisionColor, precisionColorInverse, splatControl, weight, color);
                
                return half4(color.rgb, 1.0f);
            }

            ENDHLSL
        }
    }
}
