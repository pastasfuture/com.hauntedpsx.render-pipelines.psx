using System;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    // We don't expose HDRenderQueue instead we create as much value as needed in the enum for our different pass
    // and use inspector to manipulate the value.
    // We want to use the RenderQueue to help with sorting. We define a neutral value for the RenderQueue and priority going from -X to +X
    // going from -X to +X instead of 0 to +X as builtin Unity is better for artists as they can decide late to sort behind or in front of the scene.
    // This is built after how HDRP exposes RenderQueues to artists.

    public static class PSXRenderQueue
    {
        const int k_TransparentPriorityQueueRange = 100;

        public enum Priority
        {
            BackgroundOpaque = UnityEngine.Rendering.RenderQueue.Background, // 1000
            BackgroundOpaqueAlphaTest = UnityEngine.Rendering.RenderQueue.Background + 450, // 1000 + 450 = 1450
            BackgroundOpaqueLast = UnityEngine.Rendering.RenderQueue.Background + 500, // 1000 + 500 = 1500

            BackgroundTransparentFirst = UnityEngine.Rendering.RenderQueue.Background + 500 + 1, // 1000 + 500 + 1 = 1501
            BackgroundTransparent = UnityEngine.Rendering.RenderQueue.Background + 500 + 1 + k_TransparentPriorityQueueRange, // 1000 + 500 + 1 + 100 = 1601
            BackgroundTransparentLast = UnityEngine.Rendering.RenderQueue.Background + 500 + 1 + k_TransparentPriorityQueueRange * 2, // 1000 + 500 + 1 + 100 * 2 = 1701

            MainOpaque = UnityEngine.Rendering.RenderQueue.Geometry, // 2000
            MainOpaqueAlphaTest = UnityEngine.Rendering.RenderQueue.AlphaTest, // 2450
            // Warning: we must not change Geometry last value to stay compatible with occlusion
            MainOpaqueLast = UnityEngine.Rendering.RenderQueue.GeometryLast, // 2500

            MainTransparentFirst = UnityEngine.Rendering.RenderQueue.Transparent - k_TransparentPriorityQueueRange, // 3000 - 100 = 2900
            MainTransparent = UnityEngine.Rendering.RenderQueue.Transparent, // 3000
            MainTransparentLast = UnityEngine.Rendering.RenderQueue.Transparent + k_TransparentPriorityQueueRange, // 3000 + 100 = 3100

            UIOverlayOpaque = UnityEngine.Rendering.RenderQueue.Overlay, // 4000
            UIOverlayOpaqueAlphaTest = UnityEngine.Rendering.RenderQueue.Overlay + 450, // 4000 + 450 = 4450
            UIOverlayOpaqueLast = UnityEngine.Rendering.RenderQueue.Overlay + 500, // 4000 + 500 = 4500

            UIOverlayTransparentFirst = UnityEngine.Rendering.RenderQueue.Overlay + 500 + 1, // 4000 + 500 + 1 = 4501
            UIOverlayTransparent = UnityEngine.Rendering.RenderQueue.Overlay + 500 + 1 + k_TransparentPriorityQueueRange, // 4000 + 500 + 1 + 100 = 4601
            UIOverlayTransparentLast = UnityEngine.Rendering.RenderQueue.Overlay + 500 + 1 + k_TransparentPriorityQueueRange * 2, // 4000 + 500 + 1 + 100 * 2 = 4701
        }

        public enum RenderQueueType
        {
            // Background "Layer"
            BackgroundOpaque,
            BackgroundTransparent,

            // Main / Default "Layer"
            MainOpaque,
            MainTransparent,

            // UI Overlay "Layer"
            UIOverlayOpaque,
            UIOverlayTransparent,

            Unknown
        }

        public static readonly RenderQueueRange k_RenderQueue_BackgroundOpaqueNoAlphaTest = new RenderQueueRange { lowerBound = (int)Priority.BackgroundOpaque, upperBound = (int)Priority.BackgroundOpaqueAlphaTest - 1 };
        public static readonly RenderQueueRange k_RenderQueue_BackgroundAlphaTest = new RenderQueueRange { lowerBound = (int)Priority.BackgroundOpaqueAlphaTest, upperBound = (int)Priority.BackgroundOpaqueLast };
        public static readonly RenderQueueRange k_RenderQueue_BackgroundAllOpaque = new RenderQueueRange { lowerBound = (int)Priority.BackgroundOpaque, upperBound = (int)Priority.BackgroundOpaqueLast };
        public static readonly RenderQueueRange k_RenderQueue_BackgroundTransparent = new RenderQueueRange { lowerBound = (int)Priority.BackgroundTransparentFirst, upperBound = (int)Priority.BackgroundTransparentLast };

        public static readonly RenderQueueRange k_RenderQueue_MainOpaqueNoAlphaTest = new RenderQueueRange { lowerBound = (int)Priority.MainOpaque, upperBound = (int)Priority.MainOpaqueAlphaTest - 1 };
        public static readonly RenderQueueRange k_RenderQueue_MainAlphaTest = new RenderQueueRange { lowerBound = (int)Priority.MainOpaqueAlphaTest, upperBound = (int)Priority.MainOpaqueLast };
        public static readonly RenderQueueRange k_RenderQueue_MainAllOpaque = new RenderQueueRange { lowerBound = (int)Priority.MainOpaque, upperBound = (int)Priority.MainOpaqueLast };
        public static readonly RenderQueueRange k_RenderQueue_MainTransparent = new RenderQueueRange { lowerBound = (int)Priority.MainTransparentFirst, upperBound = (int)Priority.MainTransparentLast };

        public static readonly RenderQueueRange k_RenderQueue_UIOverlayOpaqueNoAlphaTest = new RenderQueueRange { lowerBound = (int)Priority.UIOverlayOpaque, upperBound = (int)Priority.UIOverlayOpaqueAlphaTest - 1 };
        public static readonly RenderQueueRange k_RenderQueue_UIOverlayAlphaTest = new RenderQueueRange { lowerBound = (int)Priority.UIOverlayOpaqueAlphaTest, upperBound = (int)Priority.UIOverlayOpaqueLast };
        public static readonly RenderQueueRange k_RenderQueue_UIOverlayAllOpaque = new RenderQueueRange { lowerBound = (int)Priority.UIOverlayOpaque, upperBound = (int)Priority.UIOverlayOpaqueLast };
        public static readonly RenderQueueRange k_RenderQueue_UIOverlayTransparent = new RenderQueueRange { lowerBound = (int)Priority.UIOverlayTransparentFirst, upperBound = (int)Priority.UIOverlayTransparentLast };

        public static readonly RenderQueueRange k_RenderQueue_All = new RenderQueueRange { lowerBound = 0, upperBound = 5000 };

        public static bool Contains(this RenderQueueRange range, int value) => range.lowerBound <= value && value <= range.upperBound;

        public static int Clamps(this RenderQueueRange range, int value) => Math.Max(range.lowerBound, Math.Min(value, range.upperBound));

        public static RenderQueueType GetTypeByRenderQueueValue(int renderQueue)
        {
            if (k_RenderQueue_BackgroundAllOpaque.Contains(renderQueue)) { return RenderQueueType.BackgroundOpaque; }
            if (k_RenderQueue_BackgroundTransparent.Contains(renderQueue)) { return RenderQueueType.BackgroundTransparent; }

            if (k_RenderQueue_MainAllOpaque.Contains(renderQueue)) { return RenderQueueType.MainOpaque; }
            if (k_RenderQueue_MainTransparent.Contains(renderQueue)) { return RenderQueueType.MainTransparent; }

            if (k_RenderQueue_UIOverlayAllOpaque.Contains(renderQueue)) { return RenderQueueType.UIOverlayOpaque; }
            if (k_RenderQueue_UIOverlayTransparent.Contains(renderQueue)) { return RenderQueueType.UIOverlayTransparent; }

            return RenderQueueType.Unknown;
        }

        public static int ChangeType(RenderQueueType targetType, int offset = 0, bool alphaClip = false)
        {

            if (offset < -k_TransparentPriorityQueueRange || offset > k_TransparentPriorityQueueRange)
            {
                throw new ArgumentException("Out of bounds offset, was " + offset);
            }

            switch (targetType)
            {
                case RenderQueueType.BackgroundOpaque:
                    return alphaClip ? (int)Priority.BackgroundOpaqueAlphaTest : (int)Priority.BackgroundOpaque;

                case RenderQueueType.BackgroundTransparent:
                    return (int)Priority.BackgroundTransparent + offset;

                case RenderQueueType.MainOpaque:
                    return alphaClip ? (int)Priority.MainOpaqueAlphaTest : (int)Priority.MainOpaque;

                case RenderQueueType.MainTransparent:
                    return (int)Priority.MainTransparent + offset;

                case RenderQueueType.UIOverlayOpaque:
                    return alphaClip ? (int)Priority.UIOverlayOpaqueAlphaTest : (int)Priority.UIOverlayOpaque;

                case RenderQueueType.UIOverlayTransparent:
                    return (int)Priority.UIOverlayTransparent + offset;

                default:
                    throw new ArgumentException("Unknown RenderQueueType, was " + targetType);
            }
        }
    }
}