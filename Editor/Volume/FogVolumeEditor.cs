using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
#if UNITY_2022_1_OR_NEWER
    [CustomEditor(typeof(FogVolume))]
#else
    [VolumeComponentEditor(typeof(FogVolume))]
#endif
    public class FogVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_IsEnabled;
        SerializedDataParameter m_BlendMode;
        SerializedDataParameter m_FogFalloffMode;
        SerializedDataParameter m_Color;
        SerializedDataParameter m_PrecisionAlpha;
        SerializedDataParameter m_PrecisionAlphaDitherTexture;
        SerializedDataParameter m_PrecisionAlphaDither;
        SerializedDataParameter m_DistanceMin;
        SerializedDataParameter m_DistanceMax;
        SerializedDataParameter m_FogFalloffCurve;
        SerializedDataParameter m_HeightFalloffEnabled;
        SerializedDataParameter m_HeightFalloffMirrored;
        SerializedDataParameter m_HeightMin;
        SerializedDataParameter m_HeightMax;
        SerializedDataParameter m_ColorLUTMode;
        SerializedDataParameter m_ColorLUTTexture;
        SerializedDataParameter m_ColorLUTRotationDegrees;
        SerializedDataParameter m_ColorLUTWeight;
        SerializedDataParameter m_ColorLUTWeightLayer1;

        SerializedDataParameter m_IsAdditionalLayerEnabled;
        SerializedDataParameter m_FogFalloffModeLayer1;
        SerializedDataParameter m_ColorLayer1;
        SerializedDataParameter m_DistanceMinLayer1;
        SerializedDataParameter m_DistanceMaxLayer1;
        SerializedDataParameter m_FogFalloffCurveLayer1;
        SerializedDataParameter m_HeightFalloffEnabledLayer1;
        SerializedDataParameter m_HeightFalloffMirroredLayer1;
        SerializedDataParameter m_HeightMinLayer1;
        SerializedDataParameter m_HeightMaxLayer1;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FogVolume>(serializedObject);
            m_IsEnabled = Unpack(o.Find(x => x.isEnabled));
            m_BlendMode = Unpack(o.Find(x => x.blendMode));
            m_FogFalloffMode = Unpack(o.Find(x => x.fogFalloffMode));
            m_Color = Unpack(o.Find(x => x.color));
            m_PrecisionAlpha = Unpack(o.Find(x => x.precisionAlpha));
            m_PrecisionAlphaDitherTexture = Unpack(o.Find(x => x.precisionAlphaDitherTexture));
            m_PrecisionAlphaDither = Unpack(o.Find(x => x.precisionAlphaDither));
            m_DistanceMin = Unpack(o.Find(x => x.distanceMin));
            m_DistanceMax = Unpack(o.Find(x => x.distanceMax));
            m_FogFalloffCurve = Unpack(o.Find(x => x.fogFalloffCurve));
            m_HeightFalloffEnabled = Unpack(o.Find(x => x.heightFalloffEnabled));
            m_HeightFalloffMirrored = Unpack(o.Find(x => x.heightFalloffMirrored));
            m_HeightMin = Unpack(o.Find(x => x.heightMin));
            m_HeightMax = Unpack(o.Find(x => x.heightMax));
            m_ColorLUTMode = Unpack(o.Find(x => x.colorLUTMode));
            m_ColorLUTTexture = Unpack(o.Find(x => x.colorLUTTexture));
            m_ColorLUTRotationDegrees = Unpack(o.Find(x => x.colorLUTRotationDegrees));
            m_ColorLUTWeight = Unpack(o.Find(x => x.colorLUTWeight));
            m_ColorLUTWeightLayer1 = Unpack(o.Find(x => x.colorLUTWeightLayer1));

            m_IsAdditionalLayerEnabled = Unpack(o.Find(x => x.isAdditionalLayerEnabled));
            m_FogFalloffModeLayer1 = Unpack(o.Find(x => x.fogFalloffModeLayer1));
            m_ColorLayer1 = Unpack(o.Find(x => x.colorLayer1));
            m_DistanceMinLayer1 = Unpack(o.Find(x => x.distanceMinLayer1));
            m_DistanceMaxLayer1 = Unpack(o.Find(x => x.distanceMaxLayer1));
            m_FogFalloffCurveLayer1 = Unpack(o.Find(x => x.fogFalloffCurveLayer1));
            m_HeightFalloffEnabledLayer1 = Unpack(o.Find(x => x.heightFalloffEnabledLayer1));
            m_HeightFalloffMirroredLayer1 = Unpack(o.Find(x => x.heightFalloffMirroredLayer1));
            m_HeightMinLayer1 = Unpack(o.Find(x => x.heightMinLayer1));
            m_HeightMaxLayer1 = Unpack(o.Find(x => x.heightMaxLayer1));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_IsEnabled, EditorGUIUtility.TrTextContent("Enabled", "Controls whether or not fog is enabled"));
            PropertyField(m_BlendMode, EditorGUIUtility.TrTextContent("Blend Mode", "Selects the function used to blend Fog with the underlying geometry color."));
            PropertyField(m_FogFalloffMode, EditorGUIUtility.TrTextContent("Fog Falloff Mode", "Controls the shape of the fog falloff."));
            PropertyField(m_Color, EditorGUIUtility.TrTextContent("Color", "Controls color (and opacity) of global distance fog."));
            PropertyField(m_PrecisionAlpha, EditorGUIUtility.TrTextContent("Precision Alpha", "Controls the fog alpha precision. Lower values creates more alpha (opacity) banding along fog fade."));
            PropertyField(m_PrecisionAlphaDitherTexture, EditorGUIUtility.TrTextContent("Alpha Dither Texture", "Specifies the texture to use for dithering fog alpha (opacity) between precision alpha banding steps. This is only applied on materials with ShadingEvaluationMode.PerPixel set. PerVertex evaluation does not receive dither."));
            if (m_PrecisionAlphaDitherTexture.value.objectReferenceValue == null)
            {
                Debug.Log(PSXRenderPipeline.instance?.asset.renderPipelineResources?.textures.framebufferDitherTex);
                m_PrecisionAlphaDitherTexture.value.objectReferenceValue = PSXRenderPipeline.instance?.asset.renderPipelineResources?.textures.framebufferDitherTex[0];
            }
            PropertyField(m_PrecisionAlphaDither, EditorGUIUtility.TrTextContent("Alpha Dither", "Controls the amount of dither applied between fog precision alpha banding steps.\nA value of 1.0 fully breaks up banding (at the cost of dither noise).\nA value of 0.0 applies no dither, so banding artifacts from low precision fog alpha are fully visible."));
            PropertyField(m_DistanceMin, EditorGUIUtility.TrTextContent("Distance Min", "Controls the distance that the global fog starts."));
            PropertyField(m_DistanceMax, EditorGUIUtility.TrTextContent("Distance Max", "Controls the distance that the global fog ends."));
            PropertyField(m_FogFalloffCurve, EditorGUIUtility.TrTextContent("Falloff Curve", "Controls the curve of the fog falloff over distance and height.\nA value of 0.0 is a standard linear falloff between distance min and distance max (and height min and height max with height falloff).\nValues > 0.0 increasingly add a shoulder to the falloff (ease in). A value < 0.0 decreasingly add a toe to the falloff (ease out)."));
            PropertyField(m_HeightFalloffEnabled, EditorGUIUtility.TrTextContent("Height Falloff Enabled", "Controls whether or not the fog falls off vertically, in addition to in depth."));
            if (m_HeightFalloffEnabled.value.boolValue)
            {
                PropertyField(m_HeightFalloffMirrored, EditorGUIUtility.TrTextContent("Height Falloff Mirrored", "If enabled, height fog will be mirrored about Height Max, creating fog that fades in below and above. Especially useful when combined with ColorLUTMode.Texture2DDistanceAndHeight, which can be used to give the mirrored fog a different color."));
                PropertyField(m_HeightMin, EditorGUIUtility.TrTextContent("Height Min", "Controls the height that fog reaches full opacity. Only has an effect if Height Falloff Enabled is true."));
                PropertyField(m_HeightMax, EditorGUIUtility.TrTextContent("Height Max", "Controls the height that fog reaches zero opacity. Only has an effect if Height Falloff Enabled is true."));
            }
            PropertyField(m_ColorLUTMode, EditorGUIUtility.TrTextContent("Color LUT Mode", "Controls whether or not a Color Look-Up-Texture is used, and the format used.\nDisabled: No Color LUT is used.\nTexture 2D Distance and Height: Uses a Texture2D where the horizontal axis stores the color along distance, and the vertical axis stores the color along height.\nTextureCube: Uses a TextureCube that is sampled using view direction (direction from camera to surface)."));
            if ((FogVolume.FogColorLUTMode)m_ColorLUTMode.value.intValue == FogVolume.FogColorLUTMode.Texture2DDistanceAndHeight)
            {
                PropertyField(m_ColorLUTTexture, EditorGUIUtility.TrTextContent("Color LUT Texture", "Specifies the texture used as the Color Look-Up-Texture. Expects a Texture2D where the horizontal axis stores the color along distance, and the vertical axis stores the color along height."));
            }
            else if ((FogVolume.FogColorLUTMode)m_ColorLUTMode.value.intValue == FogVolume.FogColorLUTMode.TextureCube)
            {
                PropertyField(m_ColorLUTTexture, EditorGUIUtility.TrTextContent("Color LUT Texture", "Specifies the texture used as the Color Look-Up-Texture. Expects a TextureCube that is sampled using view direction (direction from camera to surface)."));
                PropertyField(m_ColorLUTRotationDegrees, EditorGUIUtility.TrTextContent("Color LUT Rotation", "Specifies the rotation of the Color Look-Up-Texture Cubemap in Euler Degrees."));
            }

            if ((FogVolume.FogColorLUTMode)m_ColorLUTMode.value.intValue != FogVolume.FogColorLUTMode.Disabled)
            {
                PropertyField(m_ColorLUTWeight, EditorGUIUtility.TrTextContent("Color LUT Weight", "Specifies the amount of influence the Color LUT Texture has on the final fog color. A value of 1.0 is full influence, meaning Color LUT Texture is multiplied against the Fog Color. A value of 0.0 is no influence, meaning the final color is simply the Fog Color."));
                if (m_IsAdditionalLayerEnabled.value.boolValue)
                {
                    PropertyField(m_ColorLUTWeightLayer1, EditorGUIUtility.TrTextContent("Color LUT Weight Secondary", "Specifies the amount of influence the Color LUT Texture has on the Secondary Fog Layer."));
                }
            }

            PropertyField(m_IsAdditionalLayerEnabled, EditorGUIUtility.TrTextContent("Secondary Fog Layer Enabled", "Controls whether or not to apply a second layer of fog. A second layer can be useful for simulating soft global fog, in addition to sharper, low hanging height fog. Computing a second fog layer comes at additional cost and should be disabled when not necessary."));
            if (m_IsAdditionalLayerEnabled.value.boolValue)
            {
                PropertyField(m_FogFalloffModeLayer1, EditorGUIUtility.TrTextContent("Fog Falloff Mode Secondary", "Controls the shape of the fog falloff."));
                PropertyField(m_ColorLayer1, EditorGUIUtility.TrTextContent("Color Secondary", "Controls color (and opacity) of global distance fog."));

                PropertyField(m_DistanceMinLayer1, EditorGUIUtility.TrTextContent("Distance Min Secondary", "Controls the distance that the global fog starts."));
                PropertyField(m_DistanceMaxLayer1, EditorGUIUtility.TrTextContent("Distance Max Secondary", "Controls the distance that the global fog ends."));
                PropertyField(m_FogFalloffCurveLayer1, EditorGUIUtility.TrTextContent("Falloff Curve Secondary", "Controls the curve of the fog falloff over distance and height.\nA value of 0.0 is a standard linear falloff between distance min and distance max (and height min and height max with height falloff).\nValues > 0.0 increasingly add a shoulder to the falloff (ease in). A value < 0.0 decreasingly add a toe to the falloff (ease out)."));
                PropertyField(m_HeightFalloffEnabledLayer1, EditorGUIUtility.TrTextContent("Height Falloff Enabled Secondary", "Controls whether or not the fog falls off vertically, in addition to in depth."));
                if (m_HeightFalloffEnabledLayer1.value.boolValue)
                {
                    PropertyField(m_HeightFalloffMirroredLayer1, EditorGUIUtility.TrTextContent("Height Falloff Mirrored Secondary", "If enabled, height fog will be mirrored about Height Max, creating fog that fades in below and above. Especially useful when combined with ColorLUTMode.Texture2DDistanceAndHeight, which can be used to give the mirrored fog a different color."));
                    PropertyField(m_HeightMinLayer1, EditorGUIUtility.TrTextContent("Height Min Secondary", "Controls the height that fog reaches full opacity. Only has an effect if Height Falloff Enabled is true."));
                    PropertyField(m_HeightMaxLayer1, EditorGUIUtility.TrTextContent("Height Max Secondary", "Controls the height that fog reaches zero opacity. Only has an effect if Height Falloff Enabled is true."));
                }

            }
        }
    }
}