using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
#if UNITY_2022_1_OR_NEWER
    [CustomEditor(typeof(AccumulationMotionBlurVolume))]
#else
    [VolumeComponentEditor(typeof(AccumulationMotionBlurVolume))]
#endif
    public class AccumulationMotionBlurVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Weight;
        SerializedDataParameter m_Vignette;
        SerializedDataParameter m_Dither;
        SerializedDataParameter m_Zoom;
        SerializedDataParameter m_ZoomDither;
        SerializedDataParameter m_Anisotropy;
        SerializedDataParameter m_ApplyToUIOverlay;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<AccumulationMotionBlurVolume>(serializedObject);
            m_Weight = Unpack(o.Find(x => x.weight));
            m_Vignette = Unpack(o.Find(x => x.vignette));
            m_Dither = Unpack(o.Find(x => x.dither));
            m_Zoom = Unpack(o.Find(x => x.zoom));
            m_ZoomDither = Unpack(o.Find(x => x.zoomDither));
            m_Anisotropy = Unpack(o.Find(x => x.anisotropy));
            m_ApplyToUIOverlay = Unpack(o.Find(x => x.applyToUIOverlay));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Weight, EditorGUIUtility.TrTextContent("Weight", "Controls the amount of motion blur.\nA value of 0.0 completely disables motion blur.\nA value of 1.0 is the maxium amount of motion blur.\nRather than using per-pixel motion vectors to render motion blur in a physically plausible way as is done in a contemporary PBR render pipeline, motion blur in HPSXRP is implemented by simply blending the previous frame with the current frame.\nThis accumulation-based motion blur was the common implementation in the PSX / N64 era.\nLerping between the the current frame and the previous frame is called an Exponential Moving Average.\nAn Exponential Moving Average creates a gaussian-shaped falloff over time.\nAn Exponential Moving Average has a non-linear response to the Weight variable.\nIn particular, values between [0.0, 0.5] have a fairly small effect, compared to values between [0.9, 0.95] which have a relatively strong effect."));
            PropertyField(m_Vignette, EditorGUIUtility.TrTextContent("Vignette", "Controls the amount the effect fades out toward the center of the screen.\nA value of 0.0 creates uniform zoom across the entire screen, no fade out.\nA value of 1.0 removes zoom from the center of the screen.\nA value of -1.0 removes zoom from the edges of the screen."));
            PropertyField(m_Dither, EditorGUIUtility.TrTextContent("Dither", "Controls the amount of dither to apply to the weight when compositing the frame history with the current frame.\nThe history is composited with an 8 bit per pixel alpha value.\nDither is required to appoximately capture very low history weight pixels."));
            PropertyField(m_Zoom, EditorGUIUtility.TrTextContent("Zoom", "Controls the amount of zoom applied to the history before it is blended.\nValues > 0.0 create a outward radial blur effect.\nValues < 0.0 create an inward pincushion blur effect."));
            if (Mathf.Abs(m_Zoom.value.floatValue) > 1e-5f)
            {
                PropertyField(m_ZoomDither, EditorGUIUtility.TrTextContent("Zoom Dither", "Controls how much dither to apply to break up banding artifacts that occur when zooming more than 1 pixel.\nA value of 0.0 causes maximum banding, which is what is often seen in PSX / N64 zoom blur effects.\n A value of 1.0 removes all banding but introduces dither noise."));
                PropertyField(m_Anisotropy, EditorGUIUtility.TrTextContent("Zoom Anisotropy", "Controls the directionality of the zoom.\nA value of 0.0 blurs uniformly in all directions.\nA value of 1.0 blurs only horizontally.\nA value of -1.0 blurs only vertically."));
            }
            PropertyField(m_ApplyToUIOverlay, EditorGUIUtility.TrTextContent("Apply to UI Overlay", "When enabled, motion blur will be applied to UI Overlay geometry as well as background and main geometry.\nWhen disabled motion blur is only applied to background and main geometry.\nWhen disabled, an additional render target is allocated and blitted to in order to capture the pre-ui state of the rasterization render target."));
        }
    }
}