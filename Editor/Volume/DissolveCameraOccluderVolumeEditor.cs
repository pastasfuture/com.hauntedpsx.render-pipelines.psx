using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(DissolveCameraOccluderVolume))]
    public class DissolveCameraOccluderVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Mode;
        SerializedDataParameter m_FadeAlpha;
        SerializedDataParameter m_DistanceMin;
        SerializedDataParameter m_DistanceMax;
        SerializedDataParameter m_RadiusMin;
        SerializedDataParameter m_RadiusMax;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<DissolveCameraOccluderVolume>(serializedObject);

            m_Mode = Unpack(o.Find(x => x.mode));
            m_FadeAlpha = Unpack(o.Find(x => x.fadeAlpha));
            m_DistanceMin = Unpack(o.Find(x => x.distanceMin));
            m_DistanceMax = Unpack(o.Find(x => x.distanceMax));
            m_RadiusMin = Unpack(o.Find(x => x.radiusMin));
            m_RadiusMax = Unpack(o.Find(x => x.radiusMax));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Mode, EditorGUIUtility.TrTextContent("Mode", "Controls how camera occluding geometry is dissolved.\nDisabled: Fully disables the technique.\nDistance Fade: Fades out based on distance to the camera.\nRadius Fade: Fades out based on distance to the camera and a screen space distance from the center of the screen."));

            if ((DissolveCameraOccluderVolume.DissolveCameraOccluderMode)m_Mode.value.intValue == DissolveCameraOccluderVolume.DissolveCameraOccluderMode.Disabled) { return; }

            PropertyField(m_FadeAlpha, EditorGUIUtility.TrTextContent("Fade Alpha", "Controls the opacity of camera occluding geometry when it is fully faded out."));

            PropertyField(m_DistanceMin, EditorGUIUtility.TrTextContent("Distance Min", "Controls the distance that camera occluding geometry is fully faded out."));
            PropertyField(m_DistanceMax, EditorGUIUtility.TrTextContent("Distance Max", "Controls the distance that camera occluding geometry begins to fade out."));

            if ((DissolveCameraOccluderVolume.DissolveCameraOccluderMode)m_Mode.value.intValue == DissolveCameraOccluderVolume.DissolveCameraOccluderMode.RadialFade)
            {
                PropertyField(m_RadiusMin, EditorGUIUtility.TrTextContent("Radius Min", "Controls the radius in [-1, 1] screen space that camera occluding geometry is fully faded out."));
                PropertyField(m_RadiusMax, EditorGUIUtility.TrTextContent("Radius Max", "Controls the radius in [-1, 1] screen space that camera occluding geometry begins to fade out."));
            }
        }
    }
}