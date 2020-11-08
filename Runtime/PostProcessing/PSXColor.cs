

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    public static class PSXColor
    {
        public static float RGBFromSRGBScalar(float x)
        {
            float linearRGBLo = x / 12.92f;
            float linearRGBHi = Mathf.Pow((x + 0.055f) / 1.055f, 2.4f);
            float linearRGB = (x <= 0.04045f) ? linearRGBLo : linearRGBHi;
            return linearRGB;
        }

        public static Vector3 RGBFromSRGB(Vector3 rgb)
        {
            return new Vector3(
                RGBFromSRGBScalar(rgb.x),
                RGBFromSRGBScalar(rgb.y),
                RGBFromSRGBScalar(rgb.z)
            );
        }

        public static float SRGBFromRGBScalar(float x)
        {
            float sRGBLo = x * 12.92f;
            float sRGBHi = (Mathf.Pow(x, 1.0f / 2.4f) * 1.055f) - 0.055f;
            float sRGB   = (x <= 0.0031308f) ? sRGBLo : sRGBHi;
            return sRGB;
        }

        public static Vector3 SRGBFromRGB(Vector3 rgb)
        {
            return new Vector3(
                SRGBFromRGBScalar(rgb.x),
                SRGBFromRGBScalar(rgb.y),
                SRGBFromRGBScalar(rgb.z)
            );
        }

        public static float TonemapperGenericScalar(float x, float contrast, float shoulder, Vector2 graypointCoefficients)
        {
            return Mathf.Clamp01(
                Mathf.Pow(x, contrast) 
                / (Mathf.Pow(x, contrast * shoulder) * graypointCoefficients.x + graypointCoefficients.y)
            );
        }

        // Improved crosstalk - maintaining saturation.
        // http://gpuopen.com/wp-content/uploads/2016/03/GdcVdrLottes.pdf
        // https://www.shadertoy.com/view/XljBRK
        public static Vector3 TonemapperGeneric(Vector3 rgb, float contrast, float shoulder, Vector2 graypointCoefficients, float crossTalk, float saturation, float crossTalkSaturation)
        {
            float peak = Mathf.Max(Mathf.Max(rgb.x, Mathf.Max(rgb.y, rgb.z)), 1.0f / (256.0f * 65536.0f));
            Vector3 ratio = rgb / peak;
            peak = TonemapperGenericScalar(peak, contrast, shoulder, graypointCoefficients);

            ratio = new Vector3(Mathf.Max(0.0f, ratio.x), Mathf.Max(0.0f, ratio.y), Mathf.Max(0.0f, ratio.z));

            float p0 = (saturation + contrast) / crossTalkSaturation;
            ratio = new Vector3(Mathf.Pow(ratio.x, p0), Mathf.Pow(ratio.y, p0), Mathf.Pow(ratio.z, p0));

            float a0 = Mathf.Clamp01(Mathf.Pow(peak, crossTalk));
            ratio = new Vector3(Mathf.Lerp(ratio.x, 1.0f, a0), Mathf.Lerp(ratio.y, 1.0f, a0), Mathf.Lerp(ratio.z, 1.0f, a0));
            ratio = new Vector3(Mathf.Pow(ratio.x, crossTalkSaturation), Mathf.Pow(ratio.y, crossTalkSaturation), Mathf.Pow(ratio.z, crossTalkSaturation));

            return ratio * peak;
        }
    }
}