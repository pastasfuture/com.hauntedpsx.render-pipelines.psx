using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
#if UNITY_2022_1_OR_NEWER
    [CustomEditor(typeof(CameraVolume))]
#else
    [VolumeComponentEditor(typeof(CameraVolume))]
#endif
    public class CameraVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_IsFrameLimitEnabled;
        SerializedDataParameter m_FrameLimit;
        SerializedDataParameter m_AspectMode;
        SerializedDataParameter m_TargetRasterizationResolutionWidth;
        SerializedDataParameter m_TargetRasterizationResolutionHeight;
        SerializedDataParameter m_IsDepthBufferEnabled;
        SerializedDataParameter m_IsClearDepthAfterBackgroundEnabled;
        SerializedDataParameter m_IsClearDepthBeforeUIEnabled;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<CameraVolume>(serializedObject);

            m_IsFrameLimitEnabled = Unpack(o.Find(x => x.isFrameLimitEnabled));
            m_FrameLimit = Unpack(o.Find(x => x.frameLimit));
            m_AspectMode = Unpack(o.Find(x => x.aspectMode));
            m_TargetRasterizationResolutionWidth = Unpack(o.Find(x => x.targetRasterizationResolutionWidth));
            m_TargetRasterizationResolutionHeight = Unpack(o.Find(x => x.targetRasterizationResolutionHeight));
            m_IsDepthBufferEnabled = Unpack(o.Find(x => x.isDepthBufferEnabled));
            m_IsClearDepthAfterBackgroundEnabled = Unpack(o.Find(x => x.isClearDepthAfterBackgroundEnabled));
            m_IsClearDepthBeforeUIEnabled = Unpack(o.Find(x => x.isClearDepthBeforeUIEnabled));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_IsFrameLimitEnabled, EditorGUIUtility.TrTextContent("Frame Limit Enabled", "Controls whether or not to limit rendering frame rate. Set to false to allow the render pipeline to render as high of an FPS as the system will support. Set to true to specify a target frame rate. Useful for simulating feeling of slower PSX era games, or to simulate video (30 fps) or film (24 fps)."));
            if (m_IsFrameLimitEnabled.value.boolValue)
            {
                PropertyField(m_FrameLimit, EditorGUIUtility.TrTextContent("Frame Limit", "Controls the target frame rate when frame limit is enabled."));
            }


            PropertyField(m_AspectMode, EditorGUIUtility.TrTextContent("Aspect Ratio Mode", "Controls how the aspect ratio of the camera is handled.\nFree Stretch: Naive upscale from rasterization resolution to screen resolution. Results in pixel drop-out and moire interferance patterns when screen resolution is not an even multiple of rasterization resolution. Not reccomended.\nFree Fit Pixel Perfect: Upscale rasterization resolution to screen resolution by the max round multiple of rasterization resolution that can be contained within the screen. Perfectly preserves all pixels and dither patterns. Maintains aspect ratio. Results in black border when screen resolution is not an even multiple of rasterization resolution.\nFree Crop Pixel Perfect: Upscale rasterization resolution to screen resolution by the max round multiple of rasterization resolution that can completely fill the screen. Perfectly preserves all pixels and dither patterns. Maintains aspect ratio. Results in zoomed in image / change of aspect ration when screen resolution is not an even multiple of rasterization resolution.\nFree Bleed Pixel Perfect: Same as Free Fit Pixel Perfect, but the camera field of view is automatically expanded to fill areas that would otherwise require black borders.\nLocked Fit Pixel Perfect: Same as Free Fit Pixel Perfect, but enforces the aspect ratio defined by the rasterization resolution X and Y parameters. Useful for forcing a retro 4:3 aspect ratio on any screen.\nLocked Stretch: enforces the aspect ratio defined by the rasterization resolution X and Y parameters while upscaling in a pixel-imperfect way. Pixel dropping and duplication artifacts can occur.\nNative: Rasterizes at native camera resolution and aspect ratio. No scaling is performed."));
            if ((CameraVolume.CameraAspectMode)m_AspectMode.value.intValue != CameraVolume.CameraAspectMode.Native)
            {
                PropertyField(m_TargetRasterizationResolutionWidth, EditorGUIUtility.TrTextContent("Rasterization Resolution X", "Controls the pixel width of the framebuffer that objects are rendered to. i.e: to simulate PSX Mode 0 rendering, set this to 256. If Aspect Ratio Mode is set to Free, this value is treated as a target, rather than an absolute resolution. HPSXRP will automatically adapt the true resolution of it's framebuffer to correctly match the screen aspect ratio, while keeping the pixel density of the target specified here."));
                PropertyField(m_TargetRasterizationResolutionHeight, EditorGUIUtility.TrTextContent("Rasterization Resolution Y", "Controls the pixel height of the framebuffer that objects are rendered to. i.e: to simulate PSX Mode 0 rendering, set this to 240. If Aspect Ratio Mode is set to Free, this value is treated as a target, rather than an absolute resolution. HPSXRP will automatically adapt the true resolution of it's framebuffer to correctly match the screen aspect ratio, while keeping the pixel density of the target specified here."));
            }

            PropertyField(m_IsDepthBufferEnabled, EditorGUIUtility.TrTextContent("Depth Buffer Enabled", "Controls whether or not a depth buffer is used during rendering. Set to true to ensure pixel perfect sorting for opaque objects. Set to false for a more authentic PSX look, as PSX hardware did not have a depth buffer.\nSetting to false will also trigger draw calls to render back to front, which approximately gives you correct sorting, but will still have failure cases.\nDisabling the depth buffer also comes at a performance cost, as back to front rendering will trigger more pixels to be shaded compared to a depth buffer and front to back rendering."));
            PropertyField(m_IsClearDepthAfterBackgroundEnabled, EditorGUIUtility.TrTextContent("Clear Depth After Background Rendering", "Controls whether or not the depth buffer is cleared after rendering background category geometry. Clearing the depth buffer after the background is rendered is useful for stopping distant background sets from clipping against main category geometry.\nThis setting will only have an effect if you have materials set to the Background category, and materials set to the Main category."));
            PropertyField(m_IsClearDepthBeforeUIEnabled, EditorGUIUtility.TrTextContent("Clear Depth Before UI Rendering", "Controls whether or not the depth buffer is cleared before rendering world space UI, and materials set to the UIOverlay category. Clearing the depth buffer before rendering world space UI is useful for stopping UI from clipping against world geometry, while still allowing the UI to be rendered at the rasterization resolution.\nThis setting will only have an effect if you have materials set to the Main category, and materials set to the UIOverlay category."));
        }
    }
}