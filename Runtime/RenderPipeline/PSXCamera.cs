using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using UnityEngine.Experimental.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    // AllocHistoryFrameRT
    public class PSXCamera
    {
        private static Dictionary<Camera, PSXCamera> s_Cameras = new Dictionary<Camera, PSXCamera>();
        private static List<Camera> s_Cleanup = new List<Camera>(); // Recycled to reduce GC pressure

        internal static PSXCamera GetOrCreate(Camera camera)
        {
            PSXCamera psxCamera;

            if (!s_Cameras.TryGetValue(camera, out psxCamera))
            {
                psxCamera = new PSXCamera(camera);
                s_Cameras.Add(camera, psxCamera);
            }

            return psxCamera;
        }

        internal static void ClearAll()
        {
            foreach (var cam in s_Cameras)
            {
                cam.Value.Dispose();
            }

            s_Cameras.Clear();
            s_Cleanup.Clear();
        }

        internal Camera camera;
        private bool isFirstFrame;
        private uint cameraFrameCount;
        private uint cameraAccumulationMotionBlurFrameCount;
        private uint cameraAccumulationMotionBlurBufferCount;
        private BufferedRTHandleSystem historyRTSystem = new BufferedRTHandleSystem();

        internal PSXCamera(Camera cam)
        {
            camera = cam;

            Reset();
        }

        void Reset()
        {
            isFirstFrame = true;
            cameraFrameCount = 0;
            cameraAccumulationMotionBlurFrameCount = 0;
            cameraAccumulationMotionBlurBufferCount = 0;
        }

        internal void ResetAccumulationMotionBlurFrameCount()
        {
            cameraAccumulationMotionBlurFrameCount = 0;
        }

        /// <summary>
        /// Allocates a history RTHandle with the unique identifier id.
        /// </summary>
        /// <param name="id">Unique id for this history buffer.</param>
        /// <param name="allocator">Allocator function for the history RTHandle.</param>
        /// <param name="bufferCount">Number of buffer that should be allocated.</param>
        /// <returns>A new RTHandle.</returns>
        internal RTHandle AllocHistoryFrameRT(int id, Func<string, int, RTHandleSystem, RTHandle> allocator, int bufferCount)
        {
            historyRTSystem.AllocBuffer(id, (rts, i) => allocator(camera.name, i, rts), bufferCount);
            return historyRTSystem.GetFrameRT(id, 0);
        }

        /// <summary>
        /// Returns the id RTHandle from the previous frame.
        /// </summary>
        /// <param name="id">Id of the history RTHandle.</param>
        /// <returns>The RTHandle from previous frame.</returns>
        internal RTHandle GetPreviousFrameRT(int id)
        {
            return historyRTSystem.GetFrameRT(id, 1);
        }

        /// <summary>
        /// Returns the id RTHandle of the current frame.
        /// </summary>
        /// <param name="id">Id of the history RTHandle.</param>
        /// <returns>The RTHandle of the current frame.</returns>
        internal RTHandle GetCurrentFrameRT(int id)
        {
            return historyRTSystem.GetFrameRT(id, 0);
        }

        void Dispose()
        {
            Reset();

            if (historyRTSystem != null)
            {
                historyRTSystem.Dispose();
                historyRTSystem = null;
            }
        }

        // Look for any camera that hasn't been used in the last frame and remove them from the pool.
        internal static void CleanUnused()
        {
            foreach (var key in s_Cameras.Keys)
            {
                PSXCamera camera = s_Cameras[key];

                // Unfortunately, the scene view camera is always isActiveAndEnabled==false so we can't rely on this. For this reason we never release it (which should be fine in the editor)
                if (camera.camera != null && camera.camera.cameraType == CameraType.SceneView)
                    continue;

                //bool hasPersistentHistory = camera.m_AdditionalCameraData != null && camera.m_AdditionalCameraData.hasPersistentHistory;
                bool hasPersistentHistory = false;
                bool cameraIsPersistent = false; // camera.isPersistent

                // We keep preview camera around as they are generally disabled/enabled every frame. They will be destroyed later when camera.camera is null
                if (camera.camera == null || (!camera.camera.isActiveAndEnabled && camera.camera.cameraType != CameraType.Preview && !hasPersistentHistory && !cameraIsPersistent))
                    s_Cleanup.Add(key);
            }

            foreach (var cam in s_Cleanup)
            {
                s_Cameras[cam].Dispose();
                s_Cameras.Remove(cam);
            }

            s_Cleanup.Clear();
        }

        internal struct PSXCameraUpdateContext
        {
            public int rasterizationWidth;
            public int rasterizationHeight;
            public bool rasterizationHistoryRequested;
            public bool rasterizationPreUICopyRequested;
            public bool rasterizationRandomWriteRequested;
            public bool rasterizationDepthBufferRequested;
        }

        internal void UpdateBeginFrame(PSXCameraUpdateContext context)
        {
#if UNITY_2021_2_OR_NEWER
            RTHandles.SetReferenceSize(context.rasterizationWidth, context.rasterizationHeight);
            historyRTSystem.SwapAndSetReferenceSize(context.rasterizationWidth, context.rasterizationHeight);
#else
            RTHandles.SetReferenceSize(context.rasterizationWidth, context.rasterizationHeight, MSAASamples.None);
            historyRTSystem.SwapAndSetReferenceSize(context.rasterizationWidth, context.rasterizationHeight, MSAASamples.None);
#endif

            EnsureRasterizationRT(context);
            EnsureRasterizationPreUIRT(context);
            EnsureRasterizationDepthStencilRT(context);
        }

        internal void UpdateEndFrame()
        {
            isFirstFrame = false;
            ++cameraFrameCount;
            ++cameraAccumulationMotionBlurFrameCount;
        }

        void EnsureRasterizationRT(PSXCameraUpdateContext context)
        {
            uint rasterizationRTCountRequested = context.rasterizationHistoryRequested ? 2u : 1u;
            uint rasterizationRTCountCurrent = cameraAccumulationMotionBlurBufferCount;

            bool rasterizationRTNeedsAllocation = rasterizationRTCountRequested != rasterizationRTCountCurrent;
            if (!rasterizationRTNeedsAllocation)
            {
                RTHandle rasterizationRTCurrent = GetCurrentFrameRT((int)PSXCameraFrameHistoryType.Rasterization);
                if (rasterizationRTCurrent.rt.descriptor.enableRandomWrite != context.rasterizationRandomWriteRequested)
                {
                    rasterizationRTNeedsAllocation = true;
                }
            }

            if (rasterizationRTNeedsAllocation)
            {
                historyRTSystem.ReleaseBuffer((int)PSXCameraFrameHistoryType.Rasterization);

                var rasterizationRTAllocatorData = new RasterizationRTAllocator(scaleFactor: 1.0f, enableRandomWrite: context.rasterizationRandomWriteRequested);
                AllocHistoryFrameRT((int)PSXCameraFrameHistoryType.Rasterization, rasterizationRTAllocatorData.Allocator, (int)rasterizationRTCountRequested);

                // Reset so that our camera frame count is zeroed on resizes (when the history is no longer valid).
                Reset();

                cameraAccumulationMotionBlurBufferCount = rasterizationRTCountRequested;
            }
        }

        void EnsureRasterizationPreUIRT(PSXCameraUpdateContext context)
        {
            bool rasterizationPreUIRTAllocated = (GetCurrentFrameRT((int)PSXCameraFrameHistoryType.RasterizationPreUICopy) != null);

            if (context.rasterizationPreUICopyRequested)
            {
                if (!rasterizationPreUIRTAllocated)
                {
                    var rasterizationPreUIRTAllocatorData = new RasterizationPreUIRTAllocator(scaleFactor: 1.0f);
                    AllocHistoryFrameRT((int)PSXCameraFrameHistoryType.RasterizationPreUICopy, rasterizationPreUIRTAllocatorData.Allocator, 1);
                }
            }
            else
            {
                if (rasterizationPreUIRTAllocated)
                {
                    historyRTSystem.ReleaseBuffer((int)PSXCameraFrameHistoryType.RasterizationPreUICopy);
                }
            }
        }

        void EnsureRasterizationDepthStencilRT(PSXCameraUpdateContext context)
        {
            bool depthStencilRTAllocated = GetCurrentFrameRT((int)PSXCameraFrameHistoryType.RasterizationDepthStencil) != null;
            if (context.rasterizationDepthBufferRequested)
            {
                if (!depthStencilRTAllocated)
                {
                    var rasterizationDepthStencilRTAllocatorData = new RasterizationDepthStencilRTAllocator(scaleFactor: 1.0f);
                    AllocHistoryFrameRT((int)PSXCameraFrameHistoryType.RasterizationDepthStencil, rasterizationDepthStencilRTAllocatorData.Allocator, 1);
                }
            }
            else
            {
                if (depthStencilRTAllocated)
                {
                    historyRTSystem.ReleaseBuffer((int)PSXCameraFrameHistoryType.RasterizationDepthStencil);
                }
            }
        }

        internal uint GetCameraFrameCount()
        {
            return cameraFrameCount;
        }

        internal bool GetIsFirstFrame()
        {
            return isFirstFrame;
        }

        internal uint GetCameraAccumulationMotionBlurFrameCount()
        {
            return cameraAccumulationMotionBlurFrameCount;
        }

        // Workaround for the Allocator callback so it doesn't allocate memory because of the capture of scaleFactor.
        struct RasterizationRTAllocator
        {
            float scaleFactor;
            bool enableRandomWrite;

            public RasterizationRTAllocator(float scaleFactor, bool enableRandomWrite)
            {
                this.scaleFactor = scaleFactor;
                this.enableRandomWrite = enableRandomWrite;
            }

            public RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(
                    Vector2.one * scaleFactor,
                    slices: 1, //TextureXR.slices,
                    filterMode: FilterMode.Bilinear, // Bilinear so that sampling at non-pixel centers is supported for use in motion blur shader.
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                    dimension: TextureDimension.Tex2D, //TextureXR.dimension,
                    useDynamicScale: false, // useDynamicScale: true,
                    enableRandomWrite: enableRandomWrite,
                    name: string.Format("{0}_Rasterization RT History_{1}", id, frameIndex)
                );
            }
        }

        struct RasterizationPreUIRTAllocator
        {
            float scaleFactor;

            public RasterizationPreUIRTAllocator(float scaleFactor)
            {
                this.scaleFactor = scaleFactor;
            }

            public RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(
                    Vector2.one * scaleFactor,
                    slices: 1, //TextureXR.slices,
                    filterMode: FilterMode.Point,
                    colorFormat: GraphicsFormat.R8G8B8A8_UNorm,
                    dimension: TextureDimension.Tex2D, //TextureXR.dimension,
                    useDynamicScale: false, // useDynamicScale: true,
                    enableRandomWrite: false,
                    name: string.Format("{0}_Rasterization Pre UI RT History_{1}", id, frameIndex)
                );
            }
        }

        struct RasterizationDepthStencilRTAllocator
        {
            float scaleFactor;

            public RasterizationDepthStencilRTAllocator(float scaleFactor)
            {
                this.scaleFactor = scaleFactor;
            }

            public RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(
                    Vector2.one * scaleFactor,
                    slices: 1, //TextureXR.slices,
                    filterMode: FilterMode.Point,
                    depthBufferBits: DepthBits.Depth24,//DepthBits.Depth24,
                    isShadowMap: true, // This is not actually a shadow map RT. This is a workaround for force the RTHandleSystem to not allocate a stencil texture. This is necessary because WebGL does not support the hardcoded R8 UInt stencil format.
                    dimension: TextureDimension.Tex2D, //TextureXR.dimension,
                    useDynamicScale: false, // useDynamicScale: true,
                    name: string.Format("{0}_Rasterization Depth Stencil RT History_{1}", id, frameIndex)
                );
            }
        }
    }
}
