using System.Diagnostics;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
        SerializedDataParameter m_CameraFilterPreset;
        SerializedDataParameter m_RasterizationAntiAliasingMode;
        SerializedDataParameter m_UpscaleFilterMode;

        private static readonly GUIContent s_RasterizationAntiAliasingModeContent = EditorGUIUtility.TrTextContent("Rasterization Anti-Aliasing Mode", "Controls which algorithm is used for anti-aliasing during rasterization.");
        private static readonly GUIContent s_UpscaleFilterModeContent = EditorGUIUtility.TrTextContent("Upscale Filter Mode", "Controls which filter is used when upscaling from rasterization resolution to final output resolution.\nPoint is useful for emulating PSX-style rendering with hard edge pixels.\nBilinear uses simple hardware interpolation, resulting in a soft, but lofi look.\nN64 Doubler X synthetically doubles the resolution on the X axis by averaging two neighboring pixels on the X axis every other pixel column.\nN64 Doubler Y synthetically doubles the resolution on the Y axis by averaging two neighboring pixels on the Y axis every other pixel row.\nN64 Doubler XY synthetically doubles the resolution on both XY axes by averaging two neighboring pixels on the XY axe every other pixel row and column.\nBlur Box 2x2 creates a lofi small blur effect by averaging neighboring pixels in a 2x2 neighborhood.");

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
            m_CameraFilterPreset = Unpack(o.Find(x => x.cameraFilterPreset));
            m_RasterizationAntiAliasingMode = Unpack(o.Find(x => x.rasterizationAntiAliasingMode));
            m_UpscaleFilterMode = Unpack(o.Find(x => x.upscaleFilterMode));
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
            
            PropertyField(m_CameraFilterPreset, EditorGUIUtility.TrTextContent("Camera Filter Preset", "Controls which anti-aliasing algorithm and which upscale filter are used in a single parameter.\nPSX is useful for emulating PSX-style rendering, with no anti-aliasing and sharp point sampled upscaling.\nCustom allows full control of which anti-aliasing algorithm and which upscale filter are used.\nN64 is useful for emulating N64-style rendering."));
            CameraVolume.CameraFilterPreset cameraFilterPreset = (CameraVolume.CameraFilterPreset)m_CameraFilterPreset.value.intValue;
            
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            m_RasterizationAntiAliasingMode.overrideState.boolValue = m_CameraFilterPreset.overrideState.boolValue;
            m_UpscaleFilterMode.overrideState.boolValue = m_CameraFilterPreset.overrideState.boolValue;

            if (cameraFilterPreset == CameraVolume.CameraFilterPreset.Custom)
            {
                PropertyField(m_RasterizationAntiAliasingMode, s_RasterizationAntiAliasingModeContent);
                PropertyField(m_UpscaleFilterMode, s_UpscaleFilterModeContent);
            }
            else
            {
                switch (cameraFilterPreset)
                {
                    case CameraVolume.CameraFilterPreset.PSX:
                    {
                        m_RasterizationAntiAliasingMode.value.intValue = (int)CameraVolume.RasterizationAntiAliasingMode.None;
                        m_UpscaleFilterMode.value.intValue = (int)CameraVolume.UpscaleFilterMode.Point;
                        break;
                    }
                    case CameraVolume.CameraFilterPreset.N64:
                    {
                        m_RasterizationAntiAliasingMode.value.intValue = (int)CameraVolume.RasterizationAntiAliasingMode.MSAA4x;
                        m_UpscaleFilterMode.value.intValue = (int)CameraVolume.UpscaleFilterMode.N64DoublerX;
                        break;
                    }
                    default:
                    {
                        Debug.Assert(false);
                        break;
                    }
                }

                EditorGUI.BeginDisabledGroup(disabled: true);
                PropertyField(m_RasterizationAntiAliasingMode, s_RasterizationAntiAliasingModeContent);
                PropertyField(m_UpscaleFilterMode, s_UpscaleFilterModeContent);
                EditorGUI.EndDisabledGroup();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}