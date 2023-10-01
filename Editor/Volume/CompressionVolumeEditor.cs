using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
#if UNITY_2022_1_OR_NEWER
    [CustomEditor(typeof(CompressionVolume))]
#else
    [VolumeComponentEditor(typeof(CompressionVolume))]
#endif
    public class CompressionVolumeEditor : VolumeComponentEditor
    {

        SerializedDataParameter m_IsEnabled;
        SerializedDataParameter m_Weight;
        SerializedDataParameter m_Accuracy;
        SerializedDataParameter m_Mode;
        SerializedDataParameter m_Colorspace;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<CompressionVolume>(serializedObject);

            m_IsEnabled = Unpack(o.Find(x => x.isEnabled));
            m_Weight = Unpack(o.Find(x => x.weight));
            m_Accuracy = Unpack(o.Find(x => x.accuracy));
            m_Mode = Unpack(o.Find(x => x.mode));
            m_Colorspace = Unpack(o.Find(x => x.colorspace));
        }

        public override void OnInspectorGUI()
        {
            if (!PSXRenderPipeline.IsComputeShaderSupportedPlatform())
            {
                EditorGUILayout.HelpBox("Compression is unsupported on your current build target.\nCompression is currently only supported on platforms that implement compute shaders.\nUnsupported platforms will skip this effect.", MessageType.Warning);
            }
            PropertyField(m_IsEnabled, EditorGUIUtility.TrTextContent("Enabled", "Controls whether or not to apply an MPEG4 / JPEG-style video block compression post processing effect."));
            PropertyField(m_Weight, EditorGUIUtility.TrTextContent("Weight", "Blends between the compressed image, and the raw uncompressed image. A value of 0.0 is the raw uncompressed image, a value of 1.0 is the full compressed image."));
            PropertyField(m_Accuracy, EditorGUIUtility.TrTextContent("Accuracy", "Controls the accuracy of the final block compression compared to the original image. Decrease value to add more compression artifacts."));
            PropertyField(m_Mode, EditorGUIUtility.TrTextContent("Mode", "Set to Accurate to perform block compression on all three channels. Set to Fast to only perform block compression on the image luminance, chroma is just discretized. In sRGB colorspace setting, luminance is the Green channel."));
            PropertyField(m_Colorspace, EditorGUIUtility.TrTextContent("Colorspace", "Controls the color space block compression + discretization is performed in. Different color spaces provide different perceptual accuracy. Artistically, use this parameter to fine tune the chrominance of color error that is introduced."));
        }
    }
}