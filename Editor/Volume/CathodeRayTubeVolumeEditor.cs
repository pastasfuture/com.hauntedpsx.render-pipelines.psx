using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
#if UNITY_2022_1_OR_NEWER
    [CustomEditor(typeof(CathodeRayTubeVolume))]
#else
    [VolumeComponentEditor(typeof(CathodeRayTubeVolume))]
#endif
    public class CathodeRayTubeVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_IsEnabled;
        SerializedDataParameter m_Bloom;
        SerializedDataParameter m_GrateMaskMode;
        SerializedDataParameter m_GrateMaskTexture;
        SerializedDataParameter m_GrateMaskScale;
        SerializedDataParameter m_ScanlineSharpness;
        SerializedDataParameter m_ImageSharpness;
        SerializedDataParameter m_BloomSharpnessX;
        SerializedDataParameter m_BloomSharpnessY;
        SerializedDataParameter m_NoiseIntensity;
        SerializedDataParameter m_NoiseSaturation;
        SerializedDataParameter m_GrateMaskIntensityMin;
        SerializedDataParameter m_GrateMaskIntensityMax;
        SerializedDataParameter m_BarrelDistortionX;
        SerializedDataParameter m_BarrelDistortionY;
        SerializedDataParameter m_Vignette;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<CathodeRayTubeVolume>(serializedObject);
            m_IsEnabled = Unpack(o.Find(x => x.isEnabled));
            m_Bloom = Unpack(o.Find(x => x.bloom));
            m_GrateMaskMode = Unpack(o.Find(x => x.grateMaskMode));
            m_GrateMaskTexture = Unpack(o.Find(x => x.grateMaskTexture));
            m_GrateMaskScale = Unpack(o.Find(x => x.grateMaskScale));
            m_ScanlineSharpness = Unpack(o.Find(x => x.scanlineSharpness));
            m_ImageSharpness = Unpack(o.Find(x => x.imageSharpness));
            m_BloomSharpnessX = Unpack(o.Find(x => x.bloomSharpnessX));
            m_BloomSharpnessY = Unpack(o.Find(x => x.bloomSharpnessY));
            m_NoiseIntensity = Unpack(o.Find(x => x.noiseIntensity));
            m_NoiseSaturation = Unpack(o.Find(x => x.noiseSaturation));
            m_GrateMaskIntensityMin = Unpack(o.Find(x => x.grateMaskIntensityMin));
            m_GrateMaskIntensityMax = Unpack(o.Find(x => x.grateMaskIntensityMax));
            m_BarrelDistortionX = Unpack(o.Find(x => x.barrelDistortionX));
            m_BarrelDistortionY = Unpack(o.Find(x => x.barrelDistortionY));
            m_Vignette = Unpack(o.Find(x => x.vignette));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_IsEnabled, EditorGUIUtility.TrTextContent("Enabled", "Controls whether or not the CRT simulation is enabled."));
            PropertyField(m_Bloom, EditorGUIUtility.TrTextContent("Bloom", "Controls the overall strength of the spatial ghosting across pixels."));
            PropertyField(m_GrateMaskMode, EditorGUIUtility.TrTextContent("Grate Mask Mode", "Controls the R, G, B grate mask pattern."));
            PropertyField(m_GrateMaskTexture, EditorGUIUtility.TrTextContent("Grate Mask Texture", "Controls the R, G, B grate mask pattern using this texture's R, G, B values"));
            PropertyField(m_GrateMaskScale, EditorGUIUtility.TrTextContent("Grate Mask Scale", "Controls the scale of the RGB color matrix."));
            PropertyField(m_ScanlineSharpness, EditorGUIUtility.TrTextContent("Scanline Sharpness", "Controls the sharpness of the scanline overlay."));
            PropertyField(m_ImageSharpness, EditorGUIUtility.TrTextContent("Image Sharpness", "Controls the sharpness of the image displayed on the virtual CRT."));
            PropertyField(m_BloomSharpnessX, EditorGUIUtility.TrTextContent("Bloom Sharpness X", "Controls the sharpness (width) of bloom (spatial ghosting) horizontally."));
            PropertyField(m_BloomSharpnessY, EditorGUIUtility.TrTextContent("Bloom Sharpness Y", "Controls the sharpness (height) of bloom (spatial ghosting) vertically (across scanlines)."));
            PropertyField(m_NoiseIntensity, EditorGUIUtility.TrTextContent("Noise Intensity", "Controls the intensity of the simulated analog noise in the video signal."));
            PropertyField(m_NoiseSaturation, EditorGUIUtility.TrTextContent("Noise Saturation", "Controls the Saturation of the simulated analog noise in the video signal. A value of 0.0 will produce pure place and white noise."));
            PropertyField(m_GrateMaskIntensityMin, EditorGUIUtility.TrTextContent("Grate Mask Intensity Min", "Controls the blackpoint of the RGB color matrix cells."));
            PropertyField(m_GrateMaskIntensityMax, EditorGUIUtility.TrTextContent("Grate Mask Intensity Max", "Controls the whitepoint of the RGB color matrix cells."));
            PropertyField(m_BarrelDistortionX, EditorGUIUtility.TrTextContent("Barrel Distortion X", "Controls the intensity of the simulated CRT barrel distortion (fish bowl effect) horizontally."));
            PropertyField(m_BarrelDistortionY, EditorGUIUtility.TrTextContent("Barrel Distortion Y", "Controls the intensity of the simulated CRT barrel distortion (fish bowl effect) vertically."));
            PropertyField(m_Vignette, EditorGUIUtility.TrTextContent("Vignette", "Controls the amount of image darkening that occurs at the side of the CRT screen."));
        }
    }
}