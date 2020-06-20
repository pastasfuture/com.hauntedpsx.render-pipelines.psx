using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(CameraVolume))]
    public class CameraVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_IsFrameLimitEnabled;
        SerializedDataParameter m_FrameLimit;
        SerializedDataParameter m_AspectMode;
        SerializedDataParameter m_TargetRasterizationResolutionWidth;
        SerializedDataParameter m_TargetRasterizationResolutionHeight;
        SerializedDataParameter m_IsClearDepthBeforeUIEnabled;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<CameraVolume>(serializedObject);

            m_IsFrameLimitEnabled = Unpack(o.Find(x => x.isFrameLimitEnabled));
            m_FrameLimit = Unpack(o.Find(x => x.frameLimit));
            m_AspectMode = Unpack(o.Find(x => x.aspectMode));
            m_TargetRasterizationResolutionWidth = Unpack(o.Find(x => x.targetRasterizationResolutionWidth));
            m_TargetRasterizationResolutionHeight = Unpack(o.Find(x => x.targetRasterizationResolutionHeight));
            m_IsClearDepthBeforeUIEnabled = Unpack(o.Find(x => x.isClearDepthBeforeUIEnabled));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_IsFrameLimitEnabled, EditorGUIUtility.TrTextContent("Frame Limit Enabled", "Controls whether or not to limit rendering frame rate. Set to false to allow the render pipeline to render as high of an FPS as the system will support. Set to true to specify a target frame rate. Useful for simulating feeling of slower PSX era games, or to simulate video (30 fps) or film (24 fps)."));
            if (m_IsFrameLimitEnabled.value.boolValue)
            {
                PropertyField(m_FrameLimit, EditorGUIUtility.TrTextContent("Frame Limit", "Controls the target frame rate when frame limit is enabled."));
            }

            PropertyField(m_AspectMode, EditorGUIUtility.TrTextContent("Aspect Ratio Mode", "Controls how the aspect ratio of the camera is handled. Free mode will render the frame at the native aspect ratio of the window. Locked mode will force the camera to render at the exact aspect ratio specified by the Rasterization Resolution parameters. Locked is useful if you want to force the screen into 4:3 for a more authentic PSX experience, or into an ultra wide mode for cinematics."));
            PropertyField(m_TargetRasterizationResolutionWidth, EditorGUIUtility.TrTextContent("Rasterization Resolution X", "Controls the pixel width of the framebuffer that objects are rendered to. i.e: to simulate PSX Mode 0 rendering, set this to 256. If Aspect Ratio Mode is set to Free, this value is treated as a target, rather than an absolute resolution. HPSXRP will automatically adapt the true resolution of it's framebuffer to correctly match the screen aspect ratio, while keeping the pixel density of the target specified here."));
            PropertyField(m_TargetRasterizationResolutionHeight, EditorGUIUtility.TrTextContent("Rasterization Resolution Y", "Controls the pixel height of the framebuffer that objects are rendered to. i.e: to simulate PSX Mode 0 rendering, set this to 240. If Aspect Ratio Mode is set to Free, this value is treated as a target, rather than an absolute resolution. HPSXRP will automatically adapt the true resolution of it's framebuffer to correctly match the screen aspect ratio, while keeping the pixel density of the target specified here."));
        
            PropertyField(m_IsClearDepthBeforeUIEnabled, EditorGUIUtility.TrTextContent("Clear Depth Before UI Rendering", "Controls whether or not the depth buffer is cleared before rendering world space UI. Clearing the depth buffer before rendering world space UI is useful for stopping UI from clipping against world geometry, while still allowing the UI to be rendered at the rasterization resolution."));
        }
    }
}