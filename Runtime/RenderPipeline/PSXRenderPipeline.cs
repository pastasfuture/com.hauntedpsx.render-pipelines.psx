using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    internal class PSXRenderPipeline : UnityEngine.Rendering.RenderPipeline
    {
        readonly PSXRenderPipelineAsset m_Asset;
        internal PSXRenderPipelineAsset asset { get { return m_Asset; }}

        internal const PerObjectData k_RendererConfigurationBakedLighting = PerObjectData.LightProbe | PerObjectData.Lightmaps | PerObjectData.LightProbeProxyVolume;
        internal const PerObjectData k_RendererConfigurationBakedLightingWithShadowMask = k_RendererConfigurationBakedLighting | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ShadowMask;

        Material crtMaterial;
        
        internal PSXRenderPipeline(PSXRenderPipelineAsset asset)
        {
            m_Asset = asset;
            Build();
            Allocate();
        }

        internal protected void Build()
        {
            ConfigureGlobalRenderPipelineTag();
            ConfigureFramerateFromAsset(m_Asset);
            ConfigureSRPBatcherFromAsset(m_Asset);
        }

        static void ConfigureGlobalRenderPipelineTag()
        {
            // https://docs.unity3d.com/ScriptReference/Shader-globalRenderPipeline.html
            // Set globalRenderPipeline so that only subshaders with Tags{ "RenderPipeline" = "PSXRenderPipeline" } will be rendered.
            Shader.globalRenderPipeline = PSXStringConstants.s_GlobalRenderPipelineStr;
        }

        static void ConfigureFramerateFromAsset(PSXRenderPipelineAsset asset)
        {
            if (asset.isFrameLimitEnabled)
            {
                QualitySettings.vSyncCount = 0; // VSync must be disabled
                Application.targetFrameRate = asset.frameLimit;                
            }
            else
            {
                // Render at platform's default framerate.
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
            }
        }

        static void ConfigureSRPBatcherFromAsset(PSXRenderPipelineAsset asset)
        {
            if (asset.isSRPBatcherEnabled)
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = true;
            }
        }

        internal protected void Allocate()
        {
            this.crtMaterial = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.crtPS);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            CoreUtils.Destroy(crtMaterial);
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (cameras.Length == 0) { return; }

            UnityEngine.Rendering.RenderPipeline.BeginFrameRendering(context, cameras);

            foreach (var camera in cameras)
            {
                if (camera == null) { continue; }

                UnityEngine.Rendering.RenderPipeline.BeginCameraRendering(context, camera);

                // TODO: Should we move this after we set the rasterization render target so that scene view UI is also pixelated?
                DrawSceneViewUI(camera);

                ScriptableCullingParameters cullingParameters;
                if (!camera.TryGetCullingParameters(camera.stereoEnabled, out cullingParameters)) { continue; }

                // Need to update the volume manager for the current camera before querying any volume parameter results.
                // This triggers the volume manager to blend volume parameters spatially, based on the camera position. 
                VolumeManager.instance.Update(camera.transform, camera.cullingMask);

                bool isPSXQualityEnabled = EvaluateIsPSXQualityEnabledFromVolume();

                // Disable PSX Quality effects if the editor post processing checkbox is disabled.
                // This allows users to easily preview their raw geometry and textures without needing to toggle it in the volume system.
                // This is important, since changes to the volume system will trigger changes to the volume profile on disk.
                // We do not want to force diffs on files when users are just temporarily previewing things. 
                isPSXQualityEnabled &= CoreUtils.ArePostProcessesEnabled(camera);

                // Disable shadow casters completely as we currently do not support dynamic light sources.
                cullingParameters.cullingOptions &= ~CullingOptions.ShadowCasters;

                // Disable lighting completely as we currently do not support dynamic light sources.
                cullingParameters.cullingOptions &= ~CullingOptions.NeedsLighting;

                // Disable stereo rendering as we currently do not support it.
                cullingParameters.cullingOptions &= ~CullingOptions.Stereo;

                // Disable reflection probes as they are currently not part of the psx render pipeline.
                cullingParameters.cullingOptions &= ~CullingOptions.NeedsReflectionProbes;

                CullingResults cullingResults = context.Cull(ref cullingParameters);

                // Setup camera for rendering (sets render target, view/projection matrices and other
                // per-camera built-in shader variables).
                context.SetupCameraProperties(camera);

                ComputeRasterizationResolution(out int rasterizationWidth, out int rasterizationHeight, m_Asset.targetRasterizationResolutionWidth, m_Asset.targetRasterizationResolutionHeight, camera, isPSXQualityEnabled);
                RenderTexture rasterizationRT = RenderTexture.GetTemporary(rasterizationWidth, rasterizationHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear, 1, RenderTextureMemoryless.None, VRTextureUsage.None, false); 

                var cmd = CommandBufferPool.Get(PSXStringConstants.s_CommandBufferRenderForwardStr);
                cmd.SetRenderTarget(rasterizationRT);
                {
                    // Clear background to fog color to create seamless blend between forward-rendered fog, and "sky" / infinity.
                    PushGlobalRasterizationParameters(camera, cmd, rasterizationWidth, rasterizationHeight);
                    PushQualityOverrideParameters(camera, cmd, isPSXQualityEnabled);
                    PushPrecisionParameters(camera, cmd, m_Asset);
                    PushFogParameters(camera, cmd);
                    PushLightingParameters(camera, cmd);
                    PushTonemapperParameters(camera, cmd);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Release();
                    
                    DrawOpaque(context, camera, ref cullingResults);
                    DrawTransparent(context, camera, ref cullingResults);
                    // TODO: DrawSkybox(context, camera);
                    DrawLegacyCanvasUI(context, camera, ref cullingResults);

                    // TODO: Draw post image effect gizmos before or after CRT filter?
                    DrawGizmos(context, camera, GizmoSubset.PreImageEffects);
                    DrawGizmos(context, camera, GizmoSubset.PostImageEffects);
                }

                cmd = CommandBufferPool.Get(PSXStringConstants.s_CommandBufferRenderPostProcessStr);
                cmd.SetRenderTarget(camera.targetTexture);
                {
                    PushGlobalPostProcessingParameters(camera, cmd, m_Asset, rasterizationRT, rasterizationWidth, rasterizationHeight);
                    PushCathodeRayTubeParameters(camera, cmd);
                    PSXRenderPipeline.DrawFullScreenQuad(cmd, crtMaterial);
                }
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();
                context.Submit();
                RenderTexture.ReleaseTemporary(rasterizationRT);
                UnityEngine.Rendering.RenderPipeline.EndCameraRendering(context, camera);
            }
        }

        static bool IsMainGameView(Camera camera)
        {
            return camera.cameraType == CameraType.Game && camera.targetTexture == null; 
        }

        static void ComputeRasterizationResolution(out int width, out int height, int targetWidth, int targetHeight, Camera camera, bool isPSXQualityEnabled)
        {
            width = camera.pixelWidth;
            height = camera.pixelHeight;

            if (isPSXQualityEnabled)
            {
                // Rather than explicitly hardcoding PSX framebuffer resolution, and enforcing 1.333 aspect ratio
                // we allow arbitrary aspect ratios and match requested PSX framebuffer pixel density. (targetRasterizationResolutionWidth and targetRasterizationResolutionHeight).
                // TODO: Could create an option for this in the RenderPipelineAsset that allows users to enforce aspect with black bars.
                width = targetWidth;
                height = targetHeight;
                if (camera.pixelWidth >= camera.pixelHeight)
                {
                    // Horizontal aspect.
                    width = Mathf.FloorToInt((float)height * (float)camera.pixelWidth / (float)camera.pixelHeight + 0.5f);

                }
                else
                {
                    // Vertical aspect.
                    height = Mathf.FloorToInt((float)height * (float)camera.pixelHeight / (float)camera.pixelWidth + 0.5f);
                }
            }
        }

        static Color GetFogColorFromFogVolume()
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<FogVolume>();
            if (!volumeSettings) volumeSettings = FogVolume.@default;
            return volumeSettings.color.value;
        }

        static bool EvaluateIsPSXQualityEnabledFromVolume()
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<QualityOverrideVolume>();
            if (!volumeSettings) volumeSettings = QualityOverrideVolume.@default;
            return volumeSettings.isPSXQualityEnabled.value;
        }

        static void PushQualityOverrideParameters(Camera cmaera, CommandBuffer cmd, bool isPSXQualityEnabled)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushQualityOverrideParameters))
            {
                // Note: We do not read the quality setting directly on the volume here, as we have already constructed this value previously
                // outside of this function.
                // The volume system is not the only parameter that effects if PSX Quality is enabled or disabled.
                // We also have things like the post processing toggle in the scene view. 

                cmd.SetGlobalInt(PSXShaderIDs._IsPSXQualityEnabled, isPSXQualityEnabled ? 1 : 0);
            }
        }

        static void PushTonemapperParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushTonemapperParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<TonemapperVolume>();
                if (!volumeSettings) volumeSettings = TonemapperVolume.@default;

                int isEnabled = volumeSettings.isEnabled.value ? 1 : 0;
                float contrast = Mathf.Lerp(1e-5f, 1.95f, volumeSettings.contrast.value);
                float shoulder = Mathf.Lerp(0.9f, 1.1f, volumeSettings.shoulder.value);
                float whitepoint = volumeSettings.whitepoint.value;

                float a = contrast;
                float d = shoulder;
                float m = whitepoint;
                float i = volumeSettings.graypointIn.value;
                float o = volumeSettings.graypointOut.value;

                float b = -(o * Mathf.Pow(m, a) - Mathf.Pow(i, a)) / (o * (Mathf.Pow(i, a * d) - Mathf.Pow(m, a * d)));
                float c = ((o * Mathf.Pow(i, a * d)) * Mathf.Pow(m, a) - Mathf.Pow(i, a) * Mathf.Pow(m, a * d))
                    / (o * (Mathf.Pow(i, a * d) - Mathf.Pow(m, a * d)));

                Vector2 graypointCoefficients = new Vector2(b, c + 1e-5f);

                float crossTalk = Mathf.Lerp(1e-5f, 32.0f, volumeSettings.crossTalk.value);
                float saturation = Mathf.Lerp(0.0f, 32.0f, volumeSettings.saturation.value);
                float crossTalkSaturation = Mathf.Lerp(1e-5f, 32.0f, volumeSettings.crossTalkSaturation.value);

                cmd.SetGlobalInt(PSXShaderIDs._TonemapperIsEnabled, isEnabled);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperContrast, contrast);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperShoulder, shoulder);
                cmd.SetGlobalVector(PSXShaderIDs._TonemapperGraypointCoefficients, graypointCoefficients);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperWhitepoint, whitepoint);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperCrossTalk, crossTalk);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperSaturation, saturation);
                cmd.SetGlobalFloat(PSXShaderIDs._TonemapperCrossTalkSaturation, crossTalkSaturation);
            }
        }

        static void PushLightingParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushLightingParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<LightingVolume>();
                if (!volumeSettings) volumeSettings = LightingVolume.@default;

                bool lightingIsEnabled = volumeSettings.lightingIsEnabled.value;

                // Respect the sceneview lighting enabled / disabled toggle.
                lightingIsEnabled &= !CoreUtils.IsSceneLightingDisabled(camera);

                cmd.SetGlobalInt(PSXShaderIDs._LightingIsEnabled, lightingIsEnabled ? 1 : 0);
                cmd.SetGlobalFloat(PSXShaderIDs._BakedLightingMultiplier, volumeSettings.bakedLightingMultiplier.value);
                cmd.SetGlobalFloat(PSXShaderIDs._VertexColorLightingMultiplier, volumeSettings.vertexColorLightingMultiplier.value);
            }
        }

        static void PushPrecisionParameters(Camera camera, CommandBuffer cmd, PSXRenderPipelineAsset asset)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushPrecisionParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<PrecisionVolume>();
                if (!volumeSettings) volumeSettings = PrecisionVolume.@default;

                float precisionGeometry = Mathf.Pow(2.0f, Mathf.Lerp(0, 5, volumeSettings.geometry.value));

                float precisionGeometryScaleInverse = Mathf.Pow(2.0f, Mathf.Lerp(6.0f, 0.0f, volumeSettings.geometry.value));
                float precisionGeometryScale = 1.0f / precisionGeometryScaleInverse;
                cmd.SetGlobalVector(PSXShaderIDs._PrecisionGeometry, new Vector2(precisionGeometryScale, precisionGeometryScaleInverse));

                int precisionColorIndex = Mathf.FloorToInt(volumeSettings.color.value * 5.0f + 0.5f);
                Vector3 precisionColor = Vector3.zero; // Silence the compiler warnings.
                switch (precisionColorIndex)
                {
                    case 5: precisionColor = new Vector3(1 << 5, 1 << 6, 1 << 5); break; // Standard PS1 5:6:5 color space.
                    case 4: precisionColor = new Vector3(1 << 4, 1 << 4, 1 << 4); break;
                    case 3: precisionColor = new Vector3(1 << 3, 1 << 3, 1 << 3); break;
                    case 2: precisionColor = new Vector3(1 << 2, 1 << 2, 1 << 2); break;
                    case 1: precisionColor = new Vector3(1 << 1, 1 << 1, 1 << 1); break;
                    case 0: precisionColor = new Vector3(1 << 0, 1 << 0, 1 << 0); break;
                    default: Debug.Assert(false); break;
                }

                cmd.SetGlobalVector(PSXShaderIDs._PrecisionColor, precisionColor);
                cmd.SetGlobalVector(PSXShaderIDs._PrecisionColorInverse, new Vector3(1.0f / precisionColor.x, 1.0f / precisionColor.y, 1.0f / precisionColor.z));

                cmd.SetGlobalInt(PSXShaderIDs._FramebufferDitherIsEnabled, volumeSettings.framebufferDitherIsEnabled.value ? 1 : 0);
                Texture2D framebufferDitherTex = GetFramebufferDitherTexFromAssetAndFrame(asset, (uint)Time.frameCount);
                cmd.SetGlobalTexture(PSXShaderIDs._FramebufferDitherTexture, framebufferDitherTex);
                cmd.SetGlobalVector(PSXShaderIDs._FramebufferDitherSize, new Vector4(
                    framebufferDitherTex.width,
                    framebufferDitherTex.height,
                    1.0f / framebufferDitherTex.width,
                    1.0f / framebufferDitherTex.height
                ));

                cmd.SetGlobalFloat(PSXShaderIDs._DrawDistance, volumeSettings.drawDistance.value);
            }
        }

        static void PushFogParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushFogParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<FogVolume>();
                if (!volumeSettings) volumeSettings = FogVolume.@default;

                Vector4 fogDistanceScaleBias = new Vector4(
                    1.0f / (volumeSettings.distanceMax.value - volumeSettings.distanceMin.value), 
                    -volumeSettings.distanceMin.value / (volumeSettings.distanceMax.value - volumeSettings.distanceMin.value),
                    -1.0f / (volumeSettings.heightMax.value - volumeSettings.heightMin.value),
                    -volumeSettings.heightMin.value / (volumeSettings.heightMax.value - volumeSettings.heightMin.value) + 1.0f
                );

                // Respect the Scene View fog enable / disable toggle.
                bool isFogEnabled = volumeSettings.isEnabled.value && CoreUtils.IsSceneViewFogEnabled(camera);
                if (!isFogEnabled)
                {
                    // To visually disable fog, we simply throw fog start and end to infinity.
                    fogDistanceScaleBias = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                }
                else if (!volumeSettings.heightFalloffEnabled.value)
                {
                    fogDistanceScaleBias.z = 0.0f;
                    fogDistanceScaleBias.w = 1.0f;
                }

                int fogFalloffMode = (int)volumeSettings.fogFalloffMode.value;
                cmd.SetGlobalInt(PSXShaderIDs._FogFalloffMode, fogFalloffMode);
                cmd.SetGlobalVector(PSXShaderIDs._FogColor, new Vector4(volumeSettings.color.value.r, volumeSettings.color.value.g, volumeSettings.color.value.b, volumeSettings.color.value.a));
                cmd.SetGlobalVector(PSXShaderIDs._FogDistanceScaleBias, fogDistanceScaleBias);
            }
        }

        void PushGlobalRasterizationParameters(Camera camera, CommandBuffer cmd, int rasterizationWidth, int rasterizationHeight)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushGlobalRasterizationParameters))
            {
                cmd.ClearRenderTarget(clearDepth: true, clearColor: true, backgroundColor: GetFogColorFromFogVolume());
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSize, new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight));
                cmd.SetGlobalVector(PSXShaderIDs._Time, new Vector4(Time.timeSinceLevelLoad / 20.0f, Time.timeSinceLevelLoad, Time.timeSinceLevelLoad * 2.0f, Time.timeSinceLevelLoad * 3.0f));
            
                Texture2D alphaClippingDitherTex = GetAlphaClippingDitherTexFromAssetAndFrame(asset, (uint)Time.frameCount);
                cmd.SetGlobalTexture(PSXShaderIDs._AlphaClippingDitherTexture, alphaClippingDitherTex);
                cmd.SetGlobalVector(PSXShaderIDs._AlphaClippingDitherSize, new Vector4(
                    alphaClippingDitherTex.width,
                    alphaClippingDitherTex.height,
                    1.0f / alphaClippingDitherTex.width,
                    1.0f / alphaClippingDitherTex.height
                ));
            }  
        }

        static void PushGlobalPostProcessingParameters(Camera camera, CommandBuffer cmd, PSXRenderPipelineAsset asset, RenderTexture rasterizationRT, int rasterizationWidth, int rasterizationHeight)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushGlobalPostProcessingParameters))
            {
                bool flipY = IsMainGameView(camera);
                cmd.SetGlobalInt(PSXShaderIDs._FlipY, flipY ? 1 : 0);
                cmd.SetGlobalVector(PSXShaderIDs._UVTransform, flipY ? new Vector4(1.0f, -1.0f, 0.0f, 1.0f) : new Vector4(1.0f,  1.0f, 0.0f, 0.0f));
                cmd.SetGlobalVector(PSXShaderIDs._ScreenSize, new Vector4(camera.pixelWidth, camera.pixelHeight, 1.0f / (float)camera.pixelWidth, 1.0f / (float)camera.pixelHeight));
                cmd.SetGlobalVector(PSXShaderIDs._FrameBufferScreenSize, new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight));
                cmd.SetGlobalTexture(PSXShaderIDs._FrameBufferTexture, rasterizationRT);

                Texture2D whiteNoiseTexture = GetWhiteNoise1024RGBTexFromAssetAndFrame(asset, (uint)Time.frameCount);
                cmd.SetGlobalTexture(PSXShaderIDs._WhiteNoiseTexture, whiteNoiseTexture);
                cmd.SetGlobalVector(PSXShaderIDs._WhiteNoiseSize, new Vector4(whiteNoiseTexture.width, whiteNoiseTexture.height, 1.0f / (float)whiteNoiseTexture.width, 1.0f / (float)whiteNoiseTexture.height));
                
                Texture2D blueNoiseTexture = GetBlueNoise16RGBTexFromAssetAndFrame(asset, (uint)Time.frameCount);
                cmd.SetGlobalTexture(PSXShaderIDs._BlueNoiseTexture, blueNoiseTexture);
                cmd.SetGlobalVector(PSXShaderIDs._BlueNoiseSize, new Vector4(blueNoiseTexture.width, blueNoiseTexture.height, 1.0f / (float)blueNoiseTexture.width, 1.0f / (float)blueNoiseTexture.height));
                

                cmd.SetGlobalVector(PSXShaderIDs._Time, new Vector4(Time.timeSinceLevelLoad / 20.0f, Time.timeSinceLevelLoad, Time.timeSinceLevelLoad * 2.0f, Time.timeSinceLevelLoad * 3.0f));
            }
        }

        static Texture2D GetFramebufferDitherTexFromAssetAndFrame(PSXRenderPipelineAsset asset, uint frameCount)
        {
            Texture2D ditherTexture = Texture2D.grayTexture;
            if (asset.renderPipelineResources.textures.framebufferDitherTex != null && asset.renderPipelineResources.textures.framebufferDitherTex.Length > 0)
            {
                uint ditherTextureIndex = frameCount % (uint)asset.renderPipelineResources.textures.framebufferDitherTex.Length;
                ditherTexture = asset.renderPipelineResources.textures.framebufferDitherTex[ditherTextureIndex];
            }

            return ditherTexture;
        }

        static Texture2D GetAlphaClippingDitherTexFromAssetAndFrame(PSXRenderPipelineAsset asset, uint frameCount)
        {
            Texture2D ditherTexture = Texture2D.grayTexture;
            if (asset.renderPipelineResources.textures.alphaClippingDitherTex != null && asset.renderPipelineResources.textures.alphaClippingDitherTex.Length > 0)
            {
                uint ditherTextureIndex = frameCount % (uint)asset.renderPipelineResources.textures.alphaClippingDitherTex.Length;
                ditherTexture = asset.renderPipelineResources.textures.alphaClippingDitherTex[ditherTextureIndex];
            }

            return ditherTexture;
        }

        static Texture2D GetWhiteNoise1024RGBTexFromAssetAndFrame(PSXRenderPipelineAsset asset, uint frameCount)
        {
            Texture2D whiteNoiseTexture = Texture2D.grayTexture;
            if (asset.renderPipelineResources.textures.whiteNoise1024RGBTex != null && asset.renderPipelineResources.textures.whiteNoise1024RGBTex.Length > 0)
            {
                uint whiteNoiseTextureIndex = frameCount % (uint)asset.renderPipelineResources.textures.whiteNoise1024RGBTex.Length;
                whiteNoiseTexture = asset.renderPipelineResources.textures.whiteNoise1024RGBTex[whiteNoiseTextureIndex];
            }

            return whiteNoiseTexture;
        }

        static Texture2D GetBlueNoise16RGBTexFromAssetAndFrame(PSXRenderPipelineAsset asset, uint frameCount)
        {
            Texture2D blueNoiseTexture = Texture2D.grayTexture;
            if (asset.renderPipelineResources.textures.blueNoise16RGBTex != null && asset.renderPipelineResources.textures.blueNoise16RGBTex.Length > 0)
            {
                uint blueNoiseTextureIndex = frameCount % (uint)asset.renderPipelineResources.textures.blueNoise16RGBTex.Length;
                blueNoiseTexture = asset.renderPipelineResources.textures.blueNoise16RGBTex[blueNoiseTextureIndex];
            }

            return blueNoiseTexture;
        }

        static void PushCathodeRayTubeParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, PSXProfilingSamplers.s_PushCathodeRayTubeParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<CathodeRayTubeVolume>();
                if (!volumeSettings) volumeSettings = CathodeRayTubeVolume.@default;

                cmd.SetGlobalInt(PSXShaderIDs._CRTIsEnabled, volumeSettings.isEnabled.value ? 1 : 0);

                cmd.SetGlobalFloat(PSXShaderIDs._CRTBloom, volumeSettings.bloom.value);
                cmd.SetGlobalVector(PSXShaderIDs._CRTGrateMaskScale, new Vector2(volumeSettings.grateMaskScale.value, 1.0f / volumeSettings.grateMaskScale.value));

                // Hardness of scanline.
                //  -8.0 = soft
                // -16.0 = medium
                cmd.SetGlobalFloat(PSXShaderIDs._CRTScanlineSharpness, Mathf.Lerp(-8.0f, -32.0f, volumeSettings.scanlineSharpness.value));

                // Hardness of pixels in scanline.
                // -2.0 = soft
                // -4.0 = hard
                cmd.SetGlobalFloat(PSXShaderIDs._CRTImageSharpness, Mathf.Lerp(-2.0f, -4.0f, volumeSettings.imageSharpness.value));

                cmd.SetGlobalVector(PSXShaderIDs._CRTBloomSharpness, new Vector2(
                    // Hardness of short horizontal bloom.
                    //  -0.5 = wide to the point of clipping (bad)
                    //  -1.0 = wide
                    //  -2.0 = not very wide at all
                    Mathf.Lerp(-1.0f, -2.0f, volumeSettings.bloomSharpnessX.value),

                    // Hardness of short vertical bloom.
                    //  -1.0 = wide to the point of clipping (bad)
                    //  -1.5 = wide
                    //  -4.0 = not very wide at all
                    Mathf.Lerp(-1.5f, -4.0f, volumeSettings.bloomSharpnessY.value)
                ));

                cmd.SetGlobalFloat(PSXShaderIDs._CRTNoiseIntensity, volumeSettings.noiseIntensity.value);
                cmd.SetGlobalFloat(PSXShaderIDs._CRTNoiseSaturation, volumeSettings.noiseSaturation.value);

                cmd.SetGlobalVector(PSXShaderIDs._CRTGrateMaskIntensityMinMax, new Vector2(volumeSettings.grateMaskIntensityMin.value * 2.0f, volumeSettings.grateMaskIntensityMax.value * 2.0f));

                // Display warp.
                // 0.0 = none
                // 1.0/8.0 = extreme
                cmd.SetGlobalVector(PSXShaderIDs._CRTBarrelDistortion, new Vector2(volumeSettings.barrelDistortionX.value * 0.125f, volumeSettings.barrelDistortionY.value * 0.125f));
            
                cmd.SetGlobalFloat(PSXShaderIDs._CRTVignetteSquared, volumeSettings.vignette.value * volumeSettings.vignette.value);
            }
        }

        static void DrawSceneViewUI(Camera camera)
        {
        #if UNITY_EDITOR
            // Emit scene view UI
            if (camera.cameraType == CameraType.SceneView)
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        #endif
        }

        static void DrawOpaque(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            // Draw opaque objects using PSX shader pass
            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            var drawingSettings = new DrawingSettings(PSXShaderPassNames.s_PSXLit, sortingSettings)
            {
                // TODO: Only enable lightmap data sending if requested in the render pipeline asset.
                perObjectData = k_RendererConfigurationBakedLighting
            };
            
            var filteringSettings = new FilteringSettings()
            {
                renderQueueRange = RenderQueueRange.opaque,
                layerMask = camera.cullingMask, // Respect the culling mask specified on the camera so that users can selectively omit specific layers from rendering to this camera.
                renderingLayerMask = UInt32.MaxValue, // Everything
                excludeMotionVectorObjects = false
            };

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        static void DrawTransparent(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            // Draw transparent objects using PSX shader pass
            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };

            var drawingSettings = new DrawingSettings(PSXShaderPassNames.s_PSXLit, sortingSettings)
            {
                // TODO: Only enable lightmap data sending if requested in the render pipeline asset.
                perObjectData = k_RendererConfigurationBakedLighting
            };
            
            var filteringSettings = new FilteringSettings()
            {
                renderQueueRange = RenderQueueRange.transparent,
                layerMask = camera.cullingMask, // Respect the culling mask specified on the camera so that users can selectively omit specific layers from rendering to this camera.
                renderingLayerMask = UInt32.MaxValue, // Everything
                excludeMotionVectorObjects = false
            };

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        static void DrawSkybox(ScriptableRenderContext context, Camera camera)
        {
            context.DrawSkybox(camera);
        }

        static void DrawLegacyCanvasUI(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            // Draw legacy Canvas UI meshes.
            var sortingSettings = new SortingSettings(camera);
            var drawSettings = new DrawingSettings(PSXShaderPassNames.s_SRPDefaultUnlit, sortingSettings);
            var filterSettings = FilteringSettings.defaultValue;
            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
        }

        static Mesh s_FullscreenMesh = null;
        static Mesh fullscreenMesh
        {
            get
            {
                if (s_FullscreenMesh != null)
                    return s_FullscreenMesh;

                float topV = 0.0f;
                float bottomV = 1.0f;

                s_FullscreenMesh = new Mesh { name = "Fullscreen Quad" };
                s_FullscreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                s_FullscreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });

                s_FullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                s_FullscreenMesh.UploadMeshData(true);
                return s_FullscreenMesh;
            }
        }

        // Draws a fullscreen quad (to maintain webgl build support).
        static void DrawFullScreenQuad(CommandBuffer cmd, Material material,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            cmd.DrawMesh(PSXRenderPipeline.fullscreenMesh, Matrix4x4.identity, material);
        }

        static void DrawGizmos(ScriptableRenderContext context, Camera camera, GizmoSubset gizmoSubset)
        {
    #if UNITY_EDITOR
            if (UnityEditor.Handles.ShouldRenderGizmos())
                context.DrawGizmos(camera, gizmoSubset);
        #endif
        }
    }
}