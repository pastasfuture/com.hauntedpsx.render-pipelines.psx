using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using Unity.Collections;
using GlobalIllumination = UnityEngine.Experimental.GlobalIllumination;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    public partial class PSXRenderPipeline : UnityEngine.Rendering.RenderPipeline
    {
        // Holds light direction for directional lights or position for punctual lights.
        // When w is set to 1.0, it means it's a punctual light.
        Vector4 k_DefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 k_DefaultLightColor = Color.black;

        // Default light attenuation is setup in a particular way that it causes
        // directional lights to return 1.0 for both distance and angle attenuation
        Vector4 k_DefaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        Vector4 k_DefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        Vector4 k_DefaultLightsProbeChannel = new Vector4(-1.0f, 1.0f, 0.0f, 0.0f);

        Vector4[] m_AdditionalLightPositions;
        Vector4[] m_AdditionalLightColors;
        Vector4[] m_AdditionalLightAttenuations;
        Vector4[] m_AdditionalLightSpotDirections;
        Vector4[] m_AdditionalLightOcclusionProbeChannels;

        public enum MixedLightingSetup
        {
            None,
            ShadowMask,
            Subtractive,
        };

        MixedLightingSetup m_MixedLightingSetup;

        void AllocateLighting()
        {
            // Set callback for converting light sources from HPSXRP representation to lightmapper representation.
            Lightmapping.SetDelegate(lightsDelegate);
        }

        void DisposeLighting()
        {
            // Clear callback for converting light sources from HPSXRP representation to lightmapper representation.
            Lightmapping.ResetDelegate();
        }

        void PushDynamicLightingParameters(Camera camera, CommandBuffer cmd, ref CullingResults cullingResults)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushDynamicLightingParameters))
            {
                m_MixedLightingSetup = MixedLightingSetup.None;

                // TODO: We always set the shadowmask global keyword to false here, and then optionally set it to true later.
                // This makes it easier to implement given all of the early outs below, but it does mean they we set the keyword twice.
                CoreUtils.SetKeyword(cmd, PSXShaderKeywords.k_LIGHTMAP_SHADOW_MASK, false);

                var volumeSettings = VolumeManager.instance.stack.GetComponent<LightingVolume>();
                if (!volumeSettings) volumeSettings = LightingVolume.@default;

                bool lightingIsEnabled = volumeSettings.lightingIsEnabled.value;

                // Respect the sceneview lighting enabled / disabled toggle.
                lightingIsEnabled &= !CoreUtils.IsSceneLightingDisabled(camera);

                if (!lightingIsEnabled) { return; }
                if (volumeSettings.dynamicLightingMultiplier.value == 0.0f) { return; }

                int dynamicLightsMaxCount = volumeSettings.dynamicLightsMaxCount.value;
                int dynamicLightsMaxPerObjectCount = volumeSettings.dynamicLightsMaxPerObjectCount.value;

                if (dynamicLightsMaxCount == 0) { return; }

                EnsureAdditionalLightData(dynamicLightsMaxCount);

                int additionalLightsCount = SetupPerObjectLightIndices(camera, ref cullingResults, dynamicLightsMaxCount);
                if (additionalLightsCount == 0)
                {
                    cmd.SetGlobalVector(PSXShaderIDs._AdditionalLightsCount, Vector4.zero);
                    return;
                }

                for (int i = 0, lightIter = 0; i < cullingResults.visibleLights.Length && lightIter < dynamicLightsMaxCount; ++i)
                {
                    VisibleLight light = cullingResults.visibleLights[i];
                    if (IsLightLayerVisible(light.light.gameObject.layer, camera.cullingMask))
                    {
                        InitializeLightConstants(
                            cullingResults.visibleLights,
                            i,
                            out m_AdditionalLightPositions[lightIter],
                            out m_AdditionalLightColors[lightIter],
                            out m_AdditionalLightAttenuations[lightIter],
                            out m_AdditionalLightSpotDirections[lightIter],
                            out m_AdditionalLightOcclusionProbeChannels[lightIter]
                        );
                        lightIter++;
                    }
                }

                cmd.SetGlobalVectorArray(PSXShaderIDs._AdditionalLightsPosition, m_AdditionalLightPositions);
                cmd.SetGlobalVectorArray(PSXShaderIDs._AdditionalLightsColor, m_AdditionalLightColors);
                cmd.SetGlobalVectorArray(PSXShaderIDs._AdditionalLightsAttenuation, m_AdditionalLightAttenuations);
                cmd.SetGlobalVectorArray(PSXShaderIDs._AdditionalLightsSpotDir, m_AdditionalLightSpotDirections);
                cmd.SetGlobalVectorArray(PSXShaderIDs._AdditionalLightOcclusionProbeChannel, m_AdditionalLightOcclusionProbeChannels);

                cmd.SetGlobalVector(PSXShaderIDs._AdditionalLightsCount, new Vector4(dynamicLightsMaxPerObjectCount, 0.0f, 0.0f, 0.0f));

                // Turn on Shadow Mask sampling globally if we encountered a mixed light set to ShadowMask.
                CoreUtils.SetKeyword(cmd, PSXShaderKeywords.k_LIGHTMAP_SHADOW_MASK, m_MixedLightingSetup == MixedLightingSetup.ShadowMask);
            }
        }

        bool IsLightLayerVisible(int lightLayer, int cullingMask)
        {
            return ((1 << lightLayer) & cullingMask) > 0;
        }

        void EnsureAdditionalLightData(int capacity)
        {
            Debug.Assert(capacity > 0);

            if (m_AdditionalLightPositions == null || m_AdditionalLightPositions.Length < capacity)
            {
                m_AdditionalLightPositions = new Vector4[capacity];
                m_AdditionalLightColors = new Vector4[capacity];
                m_AdditionalLightAttenuations = new Vector4[capacity];
                m_AdditionalLightSpotDirections = new Vector4[capacity];
                m_AdditionalLightOcclusionProbeChannels = new Vector4[capacity];
            }
        }

        int SetupPerObjectLightIndices(Camera camera, ref CullingResults cullingResults, int dynamicLightsMaxCount)
        {
            Debug.Assert(dynamicLightsMaxCount > 0);

            var visibleLights = cullingResults.visibleLights;

            var perObjectLightIndexMap = cullingResults.GetLightIndexMap(Allocator.Temp);
            int globalLightsSkippedCount = 0;
            int additionalLightsCount = 0;

            // Disable all directional lights from the perobject light indices
            // Pipeline handles main light globally and there's no support for additional directional lights atm.
            for (int i = 0; i < visibleLights.Length; ++i)
            {
                if (additionalLightsCount >= dynamicLightsMaxCount)
                    break;

                VisibleLight light = visibleLights[i];
                // if (i == lightData.mainLightIndex)
                // {
                //     perObjectLightIndexMap[i] = -1;
                //     ++globalLightsSkippedCount;
                // }
                if (!IsLightLayerVisible(light.light.gameObject.layer, camera.cullingMask))
                {
                    perObjectLightIndexMap[i] = -1;
                    ++globalLightsSkippedCount;
                }
                else
                {
                    perObjectLightIndexMap[i] -= globalLightsSkippedCount;
                    ++additionalLightsCount;
                }
            }

            // Disable all remaining lights we cannot fit into the global light buffer.
            for (int i = globalLightsSkippedCount + additionalLightsCount; i < perObjectLightIndexMap.Length; ++i)
                perObjectLightIndexMap[i] = -1;

            cullingResults.SetLightIndexMap(perObjectLightIndexMap);        
            perObjectLightIndexMap.Dispose();
            return additionalLightsCount;
        }

        void InitializeLightConstants(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel)
        {
            lightPos = k_DefaultLightPosition;
            lightColor = k_DefaultLightColor;
            lightAttenuation = k_DefaultLightAttenuation;
            lightSpotDir = k_DefaultLightSpotDirection;
            lightOcclusionProbeChannel = k_DefaultLightsProbeChannel;

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            VisibleLight lightData = lights[lightIndex];
            if (lightData.lightType == LightType.Directional)
            {
                Vector4 dir = -lightData.localToWorldMatrix.GetColumn(2);
                lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            }
            else
            {
                Vector4 pos = lightData.localToWorldMatrix.GetColumn(3);
                lightPos = new Vector4(pos.x, pos.y, pos.z, 1.0f);
            }

            // VisibleLight.finalColor returns color in active color space (gamma in our case)
            // For shading, we want to do all our math in linear space, so we pipe in the linearized version.
            lightColor = lightData.finalColor.linear;

            // Directional Light attenuation is initialize so distance attenuation always be 1.0
            if (lightData.lightType != LightType.Directional)
            {
                // Light attenuation in universal matches the unity vanilla one.
                // attenuation = 1.0 / distanceToLightSqr
                // We offer two different smoothing factors.
                // The smoothing factors make sure that the light intensity is zero at the light range limit.
                // The first smoothing factor is a linear fade starting at 80 % of the light range.
                // smoothFactor = (lightRangeSqr - distanceToLightSqr) / (lightRangeSqr - fadeStartDistanceSqr)
                // We rewrite smoothFactor to be able to pre compute the constant terms below and apply the smooth factor
                // with one MAD instruction
                // smoothFactor =  distanceSqr * (1.0 / (fadeDistanceSqr - lightRangeSqr)) + (-lightRangeSqr / (fadeDistanceSqr - lightRangeSqr)
                //                 distanceSqr *           oneOverFadeRangeSqr             +              lightRangeSqrOverFadeRangeSqr

                // The other smoothing factor matches the one used in the Unity lightmapper but is slower than the linear one.
                // smoothFactor = (1.0 - saturate((distanceSqr * 1.0 / lightrangeSqr)^2))^2
                float lightRangeSqr = lightData.range * lightData.range;
                float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                float oneOverFadeRangeSqr = 1.0f / fadeRangeSqr;
                float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightData.range * lightData.range);

                // On mobile and Nintendo Switch: Use the faster linear smoothing factor (SHADER_HINT_NICE_QUALITY).
                // On other devices: Use the smoothing factor that matches the GI.
                lightAttenuation.x = Application.isMobilePlatform || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Switch ? oneOverFadeRangeSqr : oneOverLightRangeSqr;
                lightAttenuation.y = lightRangeSqrOverFadeRangeSqr;
            }

            if (lightData.lightType == LightType.Spot)
            {
                Vector4 dir = lightData.localToWorldMatrix.GetColumn(2);
                lightSpotDir = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);

                // Spot Attenuation with a linear falloff can be defined as
                // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
                // This can be rewritten as
                // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
                // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
                // If we precompute the terms in a MAD instruction
                float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * lightData.spotAngle * 0.5f);
                // We neeed to do a null check for particle lights
                // This should be changed in the future
                // Particle lights will use an inline function
                float cosInnerAngle;
                if (lightData.light != null)
                    cosInnerAngle = Mathf.Cos(lightData.light.innerSpotAngle * Mathf.Deg2Rad * 0.5f);
                else
                    cosInnerAngle = Mathf.Cos((2.0f * Mathf.Atan(Mathf.Tan(lightData.spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f)) * 0.5f);
                float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
                float invAngleRange = 1.0f / smoothAngleRange;
                float add = -cosOuterAngle * invAngleRange;
                lightAttenuation.z = invAngleRange;
                lightAttenuation.w = add;
            }

            Light light = lightData.light;

            // Set the occlusion probe channel.
            int occlusionProbeChannel = light != null ? light.bakingOutput.occlusionMaskChannel : -1;

            lightOcclusionProbeChannel.x = occlusionProbeChannel;
            lightOcclusionProbeChannel.y = occlusionProbeChannel == -1 ? 1f : light.shadowStrength;

            if (light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                lightData.light.shadows != LightShadows.None &&
                m_MixedLightingSetup == MixedLightingSetup.None)
            {
                switch (light.bakingOutput.mixedLightingMode)
                {
                    //case MixedLightingMode.Subtractive:
                    //    m_MixedLightingSetup = MixedLightingSetup.Subtractive;
                    //    break;
                    case MixedLightingMode.Shadowmask:
                        m_MixedLightingSetup = MixedLightingSetup.ShadowMask;
                        break;
                }
            }
        }

        // Handle conversion from HPSXRP representation of light sources to lightmapper's representation of light sources here.
        // This is based on the approach that Universal and HDRP take.
        static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] requests, NativeArray<GlobalIllumination.LightDataGI> lightsOutput) =>
        {
            // Editor only.
#if UNITY_EDITOR
            GlobalIllumination.LightDataGI lightData = new GlobalIllumination.LightDataGI();

            for (int i = 0; i < requests.Length; i++)
            {
                Light light = requests[i];

                switch (light.type)
                {
                    case LightType.Directional:
                        GlobalIllumination.DirectionalLight directionalLight = new GlobalIllumination.DirectionalLight();
                        GlobalIllumination.LightmapperUtils.Extract(light, ref directionalLight);
                        lightData.Init(ref directionalLight);
                        break;
                    case LightType.Point:
                        GlobalIllumination.PointLight pointLight = new GlobalIllumination.PointLight();
                        GlobalIllumination.LightmapperUtils.Extract(light, ref pointLight);
                        lightData.Init(ref pointLight);
                        break;
                    case LightType.Spot:
                        GlobalIllumination.SpotLight spotLight = new GlobalIllumination.SpotLight();
                        GlobalIllumination.LightmapperUtils.Extract(light, ref spotLight);
                        spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                        spotLight.angularFalloff = GlobalIllumination.AngularFalloffType.AnalyticAndInnerAngle;
                        lightData.Init(ref spotLight);
                        break;
                    case LightType.Area:
                        GlobalIllumination.RectangleLight rectangleLight = new GlobalIllumination.RectangleLight();
                        GlobalIllumination.LightmapperUtils.Extract(light, ref rectangleLight);
                        rectangleLight.mode = GlobalIllumination.LightMode.Baked;
                        lightData.Init(ref rectangleLight);
                        break;
                    case LightType.Disc:
                        GlobalIllumination.DiscLight discLight = new GlobalIllumination.DiscLight();
                        GlobalIllumination.LightmapperUtils.Extract(light, ref discLight);
                        discLight.mode = GlobalIllumination.LightMode.Baked;
                        lightData.Init(ref discLight);
                        break;
                    default:
                        lightData.InitNoBake(light.GetInstanceID());
                        break;
                }

                GlobalIllumination.LinearColor directColor, indirectColor;
                directColor = GlobalIllumination.LinearColor.Convert(light.color, light.intensity);
                indirectColor = GlobalIllumination.LightmapperUtils.ExtractIndirect(light);

                if (light.type != LightType.Area && light.type != LightType.Disc)
                {
                    // Division by PI is handled at runtime in the shaders when evaluating lambert, rather than being baked into the light color.
                    directColor.intensity /= Mathf.PI;
                    indirectColor.intensity /= Mathf.PI;
                }

                lightData.color = directColor;
                lightData.indirectColor = indirectColor;
                lightData.falloff = GlobalIllumination.FalloffType.InverseSquared;
                lightsOutput[i] = lightData;
            }
#else
            GlobalIllumination.LightDataGI lightData = new GlobalIllumination.LightDataGI();

            for (int i = 0; i < requests.Length; i++)
            {
                Light light = requests[i];
                lightData.InitNoBake(light.GetInstanceID());
                lightsOutput[i] = lightData;
            }
            Debug.LogWarning("Realtime GI is not supported in HPSXRP.");
#endif
        };
    }
}