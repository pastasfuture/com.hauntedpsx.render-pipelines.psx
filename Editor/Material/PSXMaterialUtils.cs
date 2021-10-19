using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    public static class PSXMaterialUtils
    {
        public enum RenderQueueCategory
        {
            Main = 0,
            Background,
            UIOverlay
        }

        public enum TextureFilterMode
        {
            TextureImportSettings = 0,
            Point,
            PointMipmaps,
            N64,
            N64Mipmaps
        }

        public enum LightingMode
        {
            Lit = 0,
            Unlit
        }

        public enum ShadingEvaluationMode
        {
            PerVertex = 0,
            PerPixel,
            PerObject
        }

        public enum SurfaceType
        {
            Opaque,
            Transparent
        }

        public enum BlendMode
        {
            AlphaPostmultiply, // Old school alpha-blending mode.
            AlphaPremultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply
            Additive,
            Multiply,
            Subtractive
        }

        public enum VertexColorMode
        {
            Disabled = 0,
            Color,
            Lighting,
            ColorBackground,
            AlphaOnly,
            Emission,
            EmissionAndAlphaOnly,
            SplitColorAndLighting
        }

        public enum VertexColorBlendMode
        {
            Multiply = 0,
            Additive,
            Subtractive
        }

        public enum RenderFace
        {
            Front = 2,
            Back = 1,
            Both = 0
        }

        public enum DoubleSidedNormalMode
        {
            Front = 0,
            Flip
        }

        public enum ReflectionDirectionMode
        {
            Reflection = 0,
            Normal,
            View
        }

        public enum ReflectionBlendMode
        {
            Additive = 0,
            Subtractive,
            Multiply
        }

        public enum DrawDistanceOverrideMode
        {
            None = 0,
            Disabled,
            Override,
            Add,
            Multiply
        }

        public enum PrecisionGeometryOverrideMode
        {
            None = 0,
            Disabled,
            Override,
            Add,
            Multiply
        }

        public enum PrecisionColorOverrideMode
        {
            None = 0,
            Disabled,
            Override,
            Add,
            Multiply
        }

        public enum UVAnimationMode
        {
            None = 0,
            PanLinear,
            PanSin,
            Flipbook
        }

        public enum BRDFMode
        {
            Lambert = 0,
            WrappedLighting
        }

        public static class Tags
        {
            public static readonly string RenderType = "RenderType";
            public static readonly string Opaque = "Opaque";
            public static readonly string TransparentCutout = "TransparentCutout";
            public static readonly string Transparent = "Transparent";
        }

        public static class PropertyNames
        {
            public static readonly string _TextureFilterMode = "_TextureFilterMode";
            public static readonly string _VertexColorMode = "_VertexColorMode";
            public static readonly string _VertexColorBlendMode = "_VertexColorBlendMode";
            public static readonly string _RenderQueueCategory = "_RenderQueueCategory";
            public static readonly string _LightingMode = "_LightingMode";
            public static readonly string _LightingBaked = "_LightingBaked";
            public static readonly string _LightingDynamic = "_LightingDynamic";
            public static readonly string _ShadingEvaluationMode = "_ShadingEvaluationMode";
            public static readonly string _Surface = "_Surface";
            public static readonly string _Blend = "_Blend";
            public static readonly string _Cull = "_Cull";
            public static readonly string _BlendOp = "_BlendOp";
            public static readonly string _SrcBlend = "_SrcBlend";
            public static readonly string _DstBlend = "_DstBlend";
            public static readonly string _ZWrite = "_ZWrite";
            public static readonly string _ColorMask = "_ColorMask";
            public static readonly string _AlphaClip = "_AlphaClip";
            public static readonly string _AlphaClippingDitherIsEnabled = "_AlphaClippingDitherIsEnabled";
            public static readonly string _AlphaClippingScaleBiasMinMax = "_AlphaClippingScaleBiasMinMax";
            public static readonly string _AffineTextureWarpingWeight = "_AffineTextureWarpingWeight";
            public static readonly string _PrecisionGeometryWeightDeprecated = "_PrecisionGeometryWeight";
            public static readonly string _PrecisionGeometryOverrideMode = "_PrecisionGeometryOverrideMode";
            public static readonly string _PrecisionGeometryOverrideParameters = "_PrecisionGeometryOverrideParameters";
            public static readonly string _PrecisionColorOverrideMode = "_PrecisionColorOverrideMode";
            public static readonly string _PrecisionColorOverrideParameters = "_PrecisionColorOverrideParameters";
            public static readonly string _FogWeight = "_FogWeight";
            public static readonly string _DrawDistanceOverrideMode = "_DrawDistanceOverrideMode";
            public static readonly string _DrawDistanceOverride = "_DrawDistanceOverride";
            public static readonly string _UVAnimationMode = "_UVAnimationMode";
            public static readonly string _UVAnimationParameters = "_UVAnimationParameters";
            public static readonly string _UVAnimationParametersFrameLimit = "_UVAnimationParametersFrameLimit";
            public static readonly string _Cutoff = "_Cutoff";
            public static readonly string _ReceiveShadows = "_ReceiveShadows";
            public static readonly string _MainTex = "_MainTex";
            public static readonly string _MainColor = "_MainColor";
            public static readonly string _EmissionTexture = "_EmissionTexture";
            public static readonly string _EmissionColor = "_EmissionColor";
            public static readonly string _EmissionBakedMultiplier = "_EmissionBakedMultiplier";
            public static readonly string _Reflection = "_Reflection";
            public static readonly string _ReflectionCubemap = "_ReflectionCubemap";
            public static readonly string _ReflectionTexture = "_ReflectionTexture";
            public static readonly string _ReflectionColor = "_ReflectionColor";
            public static readonly string _ReflectionDirectionMode = "_ReflectionDirectionMode";
            public static readonly string _ReflectionBlendMode = "_ReflectionBlendMode";
            public static readonly string _DoubleSidedConstants = "_DoubleSidedConstants";
            public static readonly string _DoubleSidedNormalMode = "_DoubleSidedNormalMode";
            public static readonly string _BRDFMode = "_BRDFMode";
        }

        public static class PropertyIDs
        {
            public static readonly int _TextureFilterMode = Shader.PropertyToID(PropertyNames._TextureFilterMode);
            public static readonly int _VertexColorMode = Shader.PropertyToID(PropertyNames._VertexColorMode);
            public static readonly int _VertexColorBlendMode = Shader.PropertyToID(PropertyNames._VertexColorBlendMode);
            public static readonly int _RenderQueueCategory = Shader.PropertyToID(PropertyNames._RenderQueueCategory);
            public static readonly int _LightingMode = Shader.PropertyToID(PropertyNames._LightingMode);
            public static readonly int _LightingBaked = Shader.PropertyToID(PropertyNames._LightingBaked);
            public static readonly int _LightingDynamic = Shader.PropertyToID(PropertyNames._LightingDynamic);
            public static readonly int _ShadingEvaluationMode = Shader.PropertyToID(PropertyNames._ShadingEvaluationMode);
            public static readonly int _Surface = Shader.PropertyToID(PropertyNames._Surface);
            public static readonly int _Blend = Shader.PropertyToID(PropertyNames._Blend);
            public static readonly int _Cull = Shader.PropertyToID(PropertyNames._Cull);
            public static readonly int _BlendOp = Shader.PropertyToID(PropertyNames._BlendOp);
            public static readonly int _SrcBlend = Shader.PropertyToID(PropertyNames._SrcBlend);
            public static readonly int _DstBlend = Shader.PropertyToID(PropertyNames._DstBlend);
            public static readonly int _ZWrite = Shader.PropertyToID(PropertyNames._ZWrite);
            public static readonly int _ColorMask = Shader.PropertyToID(PropertyNames._ColorMask);
            public static readonly int _AlphaClip = Shader.PropertyToID(PropertyNames._AlphaClip);
            public static readonly int _AlphaClippingDitherIsEnabled = Shader.PropertyToID(PropertyNames._AlphaClippingDitherIsEnabled);
            public static readonly int _AlphaClippingScaleBiasMinMax = Shader.PropertyToID(PropertyNames._AlphaClippingScaleBiasMinMax);
            public static readonly int _AffineTextureWarpingWeight = Shader.PropertyToID(PropertyNames._AffineTextureWarpingWeight);
            public static readonly int _PrecisionGeometryWeightDeprecated = Shader.PropertyToID(PropertyNames._PrecisionGeometryWeightDeprecated);
            public static readonly int _FogWeight = Shader.PropertyToID(PropertyNames._FogWeight);
            public static readonly int _DrawDistanceOverrideMode = Shader.PropertyToID(PropertyNames._DrawDistanceOverrideMode);
            public static readonly int _DrawDistanceOverride = Shader.PropertyToID(PropertyNames._DrawDistanceOverride);
            public static readonly int _PrecisionGeometryOverrideMode = Shader.PropertyToID(PropertyNames._PrecisionGeometryOverrideMode);
            public static readonly int _PrecisionGeometryOverrideParameters = Shader.PropertyToID(PropertyNames._PrecisionGeometryOverrideParameters);
            public static readonly int _PrecisionColorOverrideMode = Shader.PropertyToID(PropertyNames._PrecisionColorOverrideMode);
            public static readonly int _PrecisionColorOverrideParameters = Shader.PropertyToID(PropertyNames._PrecisionColorOverrideParameters);
            public static readonly int _UVAnimationMode = Shader.PropertyToID(PropertyNames._UVAnimationMode);
            public static readonly int _UVAnimationParameters = Shader.PropertyToID(PropertyNames._UVAnimationParameters);
            public static readonly int _UVAnimationParametersFrameLimit = Shader.PropertyToID(PropertyNames._UVAnimationParametersFrameLimit);
            public static readonly int _Cutoff = Shader.PropertyToID(PropertyNames._Cutoff);
            public static readonly int _ReceiveShadows = Shader.PropertyToID(PropertyNames._ReceiveShadows);
            public static readonly int _MainTex = Shader.PropertyToID(PropertyNames._MainTex);
            public static readonly int _MainColor = Shader.PropertyToID(PropertyNames._MainColor);
            public static readonly int _EmissionTexture = Shader.PropertyToID(PropertyNames._EmissionTexture);
            public static readonly int _EmissionColor = Shader.PropertyToID(PropertyNames._EmissionColor);
            public static readonly int _EmissionBakedMultiplier = Shader.PropertyToID(PropertyNames._EmissionBakedMultiplier);
            public static readonly int _Reflection = Shader.PropertyToID(PropertyNames._Reflection);
            public static readonly int _ReflectionCubemap = Shader.PropertyToID(PropertyNames._ReflectionCubemap);
            public static readonly int _ReflectionTexture = Shader.PropertyToID(PropertyNames._ReflectionTexture);
            public static readonly int _ReflectionColor = Shader.PropertyToID(PropertyNames._ReflectionColor);
            public static readonly int _ReflectionDirectionMode = Shader.PropertyToID(PropertyNames._ReflectionDirectionMode);
            public static readonly int _ReflectionBlendMode = Shader.PropertyToID(PropertyNames._ReflectionBlendMode);
            public static readonly int _DoubleSidedConstants = Shader.PropertyToID(PropertyNames._DoubleSidedConstants);
            public static readonly int _DoubleSidedNormalMode = Shader.PropertyToID(PropertyNames._DoubleSidedNormalMode);
            public static readonly int _BRDFMode = Shader.PropertyToID(PropertyNames._BRDFMode);
        }

        public static class LegacyPropertyNames
        {
            public static readonly string _EmissiveColor = "_EmissiveColor";
            public static readonly string _EmissionEnabled = "_EmissionEnabled";
        }

        public static class LegacyPropertyIDs
        {
            public static readonly int _EmissiveColor = Shader.PropertyToID(LegacyPropertyNames._EmissiveColor);
            public static readonly int _EmissionEnabled = Shader.PropertyToID(LegacyPropertyNames._EmissionEnabled);
        }

        public static class Keywords
        {

            public static readonly string _TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS = "_TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS";
            public static readonly string _TEXTURE_FILTER_MODE_POINT = "_TEXTURE_FILTER_MODE_POINT";
            public static readonly string _TEXTURE_FILTER_MODE_POINT_MIPMAPS = "_TEXTURE_FILTER_MODE_POINT_MIPMAPS";
            public static readonly string _TEXTURE_FILTER_MODE_N64 = "_TEXTURE_FILTER_MODE_N64";
            public static readonly string _TEXTURE_FILTER_MODE_N64_MIPMAPS = "_TEXTURE_FILTER_MODE_N64_MIPMAPS";
            public static readonly string _VERTEX_COLOR_MODE_COLOR = "_VERTEX_COLOR_MODE_COLOR";
            public static readonly string _VERTEX_COLOR_MODE_LIGHTING = "_VERTEX_COLOR_MODE_LIGHTING";
            public static readonly string _VERTEX_COLOR_MODE_COLOR_BACKGROUND = "_VERTEX_COLOR_MODE_COLOR_BACKGROUND";
            public static readonly string _VERTEX_COLOR_MODE_ALPHA_ONLY = "_VERTEX_COLOR_MODE_ALPHA_ONLY";
            public static readonly string _VERTEX_COLOR_MODE_EMISSION = "_VERTEX_COLOR_MODE_EMISSION";
            public static readonly string _VERTEX_COLOR_MODE_EMISSION_AND_ALPHA_ONLY = "_VERTEX_COLOR_MODE_EMISSION_AND_ALPHA_ONLY";
            public static readonly string _VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING = "_VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING";
            public static readonly string _LIGHTING_BAKED_ON = "_LIGHTING_BAKED_ON";
            public static readonly string _LIGHTING_DYNAMIC_ON = "_LIGHTING_DYNAMIC_ON";
            public static readonly string _SHADING_EVALUATION_MODE_PER_VERTEX = "_SHADING_EVALUATION_MODE_PER_VERTEX";
            public static readonly string _SHADING_EVALUATION_MODE_PER_PIXEL = "_SHADING_EVALUATION_MODE_PER_PIXEL";
            public static readonly string _SHADING_EVALUATION_MODE_PER_OBJECT = "_SHADING_EVALUATION_MODE_PER_OBJECT";
            public static readonly string _EMISSION = "_EMISSION";
            public static readonly string _ALPHATEST_ON = "_ALPHATEST_ON";
            public static readonly string _ALPHAPREMULTIPLY_ON = "_ALPHAPREMULTIPLY_ON";
            public static readonly string _ALPHAMODULATE_ON = "_ALPHAMODULATE_ON";
            public static readonly string _BLENDMODE_TONEMAPPER_OFF = "_BLENDMODE_TONEMAPPER_OFF";
            public static readonly string _REFLECTION_ON = "_REFLECTION_ON";
            public static readonly string _FOG_ON = "_FOG_ON";
            public static readonly string _DOUBLE_SIDED_ON = "_DOUBLE_SIDED_ON";
            public static readonly string _LOD_REQUIRES_ADJUSTMENT = "_LOD_REQUIRES_ADJUSTMENT";
            public static readonly string _BRDF_MODE_LAMBERT = "_BRDF_MODE_LAMBERT";
            public static readonly string _BRDF_MODE_WRAPPED_LIGHTING = "_BRDF_MODE_WRAPPED_LIGHTING";
        }

        public static class Styles
        {
            // Categories
            public static readonly GUIContent SurfaceOptions =
                new GUIContent("Surface Options", "Controls how HPSXRP renders the Material on a screen.");

            public static readonly GUIContent SurfaceInputs = new GUIContent("Surface Inputs",
                "These settings describe the look and feel of the surface itself.");

            public static readonly GUIContent AdvancedLabel = new GUIContent("Advanced",
                "These settings affect behind-the-scenes rendering and underlying calculations.");

            public static readonly GUIContent TextureFilterMode = new GUIContent("Texture Filter Mode",
                "Controls how MainTex and EmissionTex are filtered.\nTextureFilterMode.TextureImportSettings is the standard unity behavior. Textures will be filtered using the texture's import settings.\nTextureFilterMode.Point will force PSX era nearest neighbor point sampling, regardless of texture import settings.\nTextureFilterMode.PointMipmaps is the same as TextureFilterMode.Point but supports supports point sampled lods via the texture's mipmap chain.\nTextureFilterMode.N64 will force N64 era 3-point barycentric texture filtering.\nTextureFilterMode.N64MipMaps is the same as TextureFilterMode.N64 but supports N64 sampled lods via the texture's mipmap chain.");

            public static readonly GUIContent VertexColorMode = new GUIContent("Vertex Color Mode",
                "Controls how vertex colors are interpreted by the shader. VertexColorMode.Color multiplies the vertex color data with the MainTex value. This is useful for adding variation to the MainTex color per vertex, such as in a particle sim. VertexColorMode.Lighting interprets the vertexColor data as per-vertex lighting. The result will be added to other lighting that may be present. VertexColorMode.ColorBackground blends between the vertex color data and the MainTex color based on the MainTex alpha. VertexColorMode.AlphaOnly multiplies only the alpha channel of the vertex color against the alpha of MainTex. VertexColorMode.Emission multiplies the vertex color with the Emission value. VertexColorMode.EmissionAndAlphaOnly multiplies the vertex color with the Emission value and multiplies the vertex color alpha channel with the MainTex alpha.\nVertexColorMode.SplitColorAndLighting is a hybrid of VertexColorMode.Color and VertexColorMode.Lighting. Vertex colors >= 0.5 are rescaled and treated as VertexColorMode.Lighting. Vertex colors < 0.5 are rescaled and treated as VertexColorMode.Color.");

            public static readonly GUIContent VertexColorBlendMode = new GUIContent("Vertex Color Blend Mode",
                "Controls how vertex colors are blended with MainColor|MainTex.\nMultiply is the standard vertex color blend mode, the vertexColor.rgba channels are multiplied against the MainColor.rgba channels.\nAdditive adds the vertexColor.rgb * vertexColor.a result to the MainColor.rgb channels. vertexColor.a is multiplied against MainColor.a to support alpha fade out.\nSubtractive subtracts the vertexColor.rgb * vertexColor.a result from the MainColor.rgb channels. vertexColor.a is multiplied against MainColor.a to support alpha fade out.");

            public static readonly GUIContent RenderQueueCategory =
                new GUIContent("Render Queue Category", "Controls when this object is rendered.\nMaterials set to Background are rendered first.\nMaterials set to Main are rendered second.\nMaterials set to UIOverlay are rendered last.\nThe CameraVolume override controls whether or not the depth buffer will be cleared between these stages.");

            public static readonly GUIContent LightingMode =
                new GUIContent("Lighting Mode", "Controls whether or not lighting is evaluated.");

            public static readonly GUIContent LightingBaked =
                new GUIContent("Baked Lighting Enabled", "Controls whether or not baked lighting is sampled from lightmaps and probes.");

            public static readonly GUIContent LightingVertexColor =
                new GUIContent("Vertex Color Lighting Enabled", "Controls whether or not lighting is sampled from vertex color data.");

            public static readonly GUIContent LightingDynamic =
                new GUIContent("Dynamic Lighting Enabled", "Controls whether or not lighting is evaluated from realtime light sources.");

            public static readonly GUIContent ShadingEvaluationMode =
                new GUIContent("Shading Evaluation Mode", "Controls whether shading is evaluated per object, per vertex or per pixel. This includes lighting, and fog.");

            public static readonly GUIContent surfaceType = new GUIContent("Surface Type",
                "Select a surface type for your texture. Choose between Opaque or Transparent.");

            public static readonly GUIContent blendingMode = new GUIContent("Blending Mode",
                "Controls how the color of the Transparent surface blends with the Material color in the background.");

            public static readonly GUIContent cullingText = new GUIContent("Render Face",
                "Specifies which faces to cull from your geometry. Front culls front faces. Back culls backfaces. None means that both sides are rendered.");

            public static readonly GUIContent doubleSidedNormalMode = new GUIContent("Double Sided Normal Mode",
                "Specifies how the surface normal is handled with backface geometry.\nFront means the front face surface normal will be used for the backface as is.\nFlip means the back face surface normal will point in the opposite direction.");

            public static readonly GUIContent alphaClipText = new GUIContent("Alpha Clipping",
                "Makes your Material act like a Cutout shader. Use this to create a transparent effect with hard edges between opaque and transparent areas.");

            public static readonly GUIContent alphaClippingDitherText = new GUIContent("Alpha Clipping Dither",
                "Makes your Material Cutout use the dither pattern specified in PSXRenderPipelineResources. Use this to create a transparent effect with approximate smooth transparency. The results are noiser than true transparency, but does not suffer from sorting problems.");

            public static readonly GUIContent alphaClippingScaleBiasMinMaxText = new GUIContent("Alpha Clipping Range",
                "Defines the range of alpha values that alpha clipping dither will affect. A range of [0, 1] will perform dithering across the entire range of the alpha texture. A range of [0.25, 0.75] will perform dithering between across the [0.25, 0.75] range of alpha values - where alpha values < 0.25 will be completely clipped (transparent) and alpha values > 0.75 will be completely opaque.");

            public static readonly GUIContent affineTextureWarpingWeight = new GUIContent("Affine Texture Warping Weight",
                "Allows you to decrease the amount of affine texture warping on your material. A value of 1.0 results in no change, and simply uses the Affine Texture Warping parameter from the Volume System. A value of 0.0 results in no affine texture warping. A value of 0.5 results in 50% of the affine texture warping from the Volume System.");

            public static readonly GUIContent precisionGeometryWeightDeprecated = new GUIContent("Precision Geometry Weight (deprecated)",
                "Allows you to decrease the amount of vertex snapping on your material. A value of 1.0 results in no change, and simply uses the PrecisionVolume.Geometry parameter from the Volume System. A value of 0.0 results in no vertex snapping. A value of 0.5 results in 50% blend between vertex snapping from the Volume System, and no vertex snapping.");

            public static readonly GUIContent precisionGeometryOverrideMode = new GUIContent("Precision Geometry Override Mode",
                "Allows you to override the precision geometry parameter that is specified on your Precision Volume. Useful glitch effects, or for completely disabling vertex snapping on a specific material.");

            public static readonly GUIContent precisionGeometryOverride = new GUIContent("Precision Geometry Override");

            public static readonly GUIContent precisionColorOverrideMode = new GUIContent("Precision Color Override Mode",
                "Allows you to override the precision color parameter that is specified on your Precision Volume.\nUseful for stylistic effects such as crushing the bit-depth of specific materials.\nCan also be used to reduce / remove banding + dither on specific materials.");

            public static readonly GUIContent precisionColorOverride = new GUIContent("Precision Color Override");

            public static readonly GUIContent fogWeight = new GUIContent("Fog Weight",
                "Specifies how much of the global Fog Volume is applied to this surface. In general this should be left at 1.0. Set to 0.0 to fully disable fog (and which avoids cost of evaluating fog). This parameter is particularly useful for tuning the look of skybox geometry.");

            public static readonly GUIContent drawDistanceOverrideMode = new GUIContent("Draw Distance Override Mode",
                "Allows you to override the draw distance that is specified on your Precision Volume. Useful for clipping specific materials earlier or later than the majority of your scene geometry.");

            public static readonly GUIContent drawDistanceOverride = new GUIContent("Draw Distance",
                "Controls the max distance (in meters) that triangles will render.");

            public static readonly GUIContent uvAnimationMode = new GUIContent("UV Animation Mode",
                "Allows you to apply simple procedural animation to your surfaces uvs.\nPan Linear: Useful for simple water scrolling animations, for example, a waterfall.\nPan Sin: Applies a Sin(x) scrolling animation. Useful for rocking back and forth animations.\nFlipbook: Frame by frame animation picked from the atlas texture, usable for textures like water or fire.");

            public static readonly GUIContent uvAnimationFrameLimitEnabled = new GUIContent("UV Animation Frame Limit",
                "Specifies whether or not to apply a frame limit to the uv animations.\nUseful for simulated lower frequency animation updates, i.e: retro 15 FPS animations.");

            public static readonly GUIContent uvAnimationFrameLimit = new GUIContent("UV Animation Frames Per Second",
                "Controls the frames per second that the uv animation is updated at.\nThis is a purely visual control, it does not affect performance.");
            
            public static readonly GUIContent uvAnimationPanLinearVelocity = new GUIContent("UV Animation Scroll Velocity",
                "Controls the distance per second (in uv space) that the uvs will pan.");

            public static readonly GUIContent uvAnimationPanSinFrequency = new GUIContent("UV Animation Oscillation Frequency",
                "Controls the frequency that the uvs will oscillate back and forth.");

            public static readonly GUIContent uvAnimationPanSinScale = new GUIContent("UV Animation Scale",
                "Controls the maximum distance (in uv space) that the uvs will pan (at the oscillation peak).");

            public static readonly GUIContent uvAnimationFlipbookFrequency = new GUIContent("UV Animation Flipbook Frequency",
                "Controls the speed of flipbook frame change.");
            
            public static readonly GUIContent uvAnimationFlipbookTilesX = new GUIContent("UV Animation Flipbook Tiles X",
                "Specifies amount of horizontal tiles of the flipbook.");
            
            public static readonly GUIContent uvAnimationFlipbookTilesY = new GUIContent("UV Animation Flipbook Tiles Y",
                "Specifies amount of vertical tiles of the flipbook.");

            public static readonly GUIContent mainTex = new GUIContent("Main Tex",
                "Specifies the base Material and/or Color of the surface. If you’ve selected Transparent or Alpha Clipping under Surface Options, your Material uses the Texture’s alpha channel or color.");

            public static readonly GUIContent emissionTex = new GUIContent("Emission Map",
                "Sets a Texture map to use for emission. You can also select a color with the color picker. Colors are multiplied over the Texture.");

            public static readonly GUIContent emissionBakedMultiplier = new GUIContent("Emission Baked Multiplier",
                "Multiplier for artificially increasing or decreasing emission intensity when captured in baked lighting. In general, this should kept at 1.0. Increasing or decreasing this value is not physically plausible. Values other than 1.0 can be useful when fine tuning the amount of light an emissive surface emits in the bake, without affecting the way the emissive surface appears.");
        
            public static readonly GUIContent reflection = new GUIContent("Reflection",
                "Specifies whether or not to apply cubemap reflections. Turn off when not in use to avoid performance cost.");

            public static readonly GUIContent reflectionCubemap = new GUIContent("Reflection Cubemap",
                "Specifies the cubemap used to simulate incoming reflections from the environment.");

            public static readonly GUIContent reflectionTexture = new GUIContent("Reflection Map",
                "Sets a Texture map to use for controlling how reflective the surface is. You can also select a color with the color picker. Colors are multiplied over the Texture.");

            public static readonly GUIContent reflectionDirectionMode = new GUIContent("Reflection Direction Mode",
                "Controls the direction reflections are sampled from.\nReflection is the standard, physically-based (for fully smooth materials) approach.\nNormal simply uses the surface normal as the reflection sample direction. Useful for emulating old school \"MatCap\" materials.\n View uses the direction from the camera to the surface as the reflection sample direction. Useful for rendering portals.");

            public static readonly GUIContent reflectionBlendMode = new GUIContent("Reflection Blend Mode",
                "Controls how reflections are blending with other incoming light at the surface. Additive is the standard, physically-based approach. Subtractive and Multiply blend modes are for special effects.");

            public static readonly GUIContent BRDFMode = new GUIContent("BRDF Mode",
                "Specifies how the material responds to lighting. BRDF stands for Bidirectional Reflectance Distribution Function.\nLambert: Standard, cheap diffuse-only response.\nWrapped Lighting: Same as Lambert, but lighting wraps to zero at 180 degrees (backface normal) instead of 90 degrees. Useful for approximating subsurface scattering, or for creating smoother lighting without needing to rely on baked data.\nWrapped Lighting is energy conserving so the reflection facing the light source will be dimmer when compared to lambert (the energy is redistributed over the sphere).");

            public const string textureFilterModeErrorBilinearDisabled = "Error: Texture Filter Mode {0} requires Bilinear filtering to be enabled on {1}.\nPlease select your texture and set Filter Mode to Bilinear in the Texture Import Settings in the inspector.";
            public const string textureFilterModeErrorMipmapsDisabled = "Error: Texture Filter Mode {0} requires Mipmaps to be generated on {1}.\nPlease select your texture and set Generate Mip Maps to true in the Texture Import Settings in the inspector.";

            public static readonly string[] textureFilterModeNames =
            {
                "Texture Import Settings",
                "Point",
                "Point Mip Maps",
                "N64",
                "N64 Mip Maps"
            };
        }

        // Copied from shaderGUI as it is a protected function in an abstract class, unavailable to others
        public static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties)
        {
            return FindProperty(propertyName, properties, true);
        }

        // Copied from shaderGUI as it is a protected function in an abstract class, unavailable to others
        public static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties, bool propertyIsMandatory)
        {
            for (int index = 0; index < properties.Length; ++index)
            {
                if (properties[index] != null && properties[index].name == propertyName)
                    return properties[index];
            }
            if (propertyIsMandatory)
                throw new ArgumentException("Could not find MaterialProperty: '" + propertyName + "', Num properties: " + (object) properties.Length);
            return null;
        }

        public static void SetMaterialKeywords(Material material)
        {
            // Clear all keywords for fresh start
            ClearMaterialKeywords(material);

            SetupMaterialTextureFilterMode(material);
            SetupMaterialLightingMode(material);
            SetupMaterialShadingEvaluationMode(material);
            SetupMaterialBRDFModeKeyword(material);
            SetupMaterialBlendMode(material);
            SetupMaterialFogKeyword(material);
            SetupMaterialReflectionKeyword(material);
            SetupMaterialEmissionKeyword(material);
            SetupDoubleSidedKeyword(material);
            SetupLodRequiresAdjustmentKeyword(material);
        }

        public static void ClearMaterialKeywords(Material material)
        {
            material.shaderKeywords = null;
        }

        public static void SetupMaterialEmissionKeyword(Material material)
        {
            if (material.HasProperty(LegacyPropertyIDs._EmissiveColor)) { MaterialEditor.FixupEmissiveFlag(material); }

            bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
            if (material.HasProperty(LegacyPropertyIDs._EmissionEnabled) && !shouldEmissionBeEnabled)
                shouldEmissionBeEnabled = material.GetFloat(LegacyPropertyIDs._EmissionEnabled) >= 0.5f;

            CoreUtils.SetKeyword(material, Keywords._EMISSION, shouldEmissionBeEnabled);
        }

        public static void SetupDoubleSidedKeyword(Material material)
        {
            if (material == null) { throw new ArgumentNullException("material"); }

            RenderFace renderFace = (RenderFace)material.GetFloat(PropertyIDs._Cull);
            DoubleSidedNormalMode doubleSidedNormalMode = (DoubleSidedNormalMode)material.GetFloat(PropertyIDs._DoubleSidedNormalMode);

            if ((renderFace != RenderFace.Front) && (doubleSidedNormalMode != DoubleSidedNormalMode.Front))
            {
                material.EnableKeyword(Keywords._DOUBLE_SIDED_ON);
            }
            else
            {
                material.DisableKeyword(Keywords._DOUBLE_SIDED_ON);
            }
        }

        public static void SetupLodRequiresAdjustmentKeyword(Material material)
        {
            if (material == null) { throw new ArgumentNullException("material"); }

            UVAnimationMode uvAnimationMode = (UVAnimationMode)material.GetFloat(PropertyIDs._UVAnimationMode);

            if (uvAnimationMode == UVAnimationMode.Flipbook)
            {
                material.EnableKeyword(Keywords._LOD_REQUIRES_ADJUSTMENT);
            }
            else
            {
                material.DisableKeyword(Keywords._LOD_REQUIRES_ADJUSTMENT);
            }
        }

        public static void SetupMaterialShadingEvaluationMode(Material material)
        {
            if (material == null) { throw new ArgumentNullException("material"); }
                
            ShadingEvaluationMode shadingEvaluationMode = (ShadingEvaluationMode)material.GetFloat(PropertyIDs._ShadingEvaluationMode);

            switch (shadingEvaluationMode)
            {
                case ShadingEvaluationMode.PerVertex:
                    material.EnableKeyword(Keywords._SHADING_EVALUATION_MODE_PER_VERTEX);
                    material.DisableKeyword(Keywords._SHADING_EVALUATION_MODE_PER_PIXEL);
                    material.DisableKeyword(Keywords._SHADING_EVALUATION_MODE_PER_OBJECT);
                    break;
                case ShadingEvaluationMode.PerPixel:
                    material.DisableKeyword(Keywords._SHADING_EVALUATION_MODE_PER_VERTEX);
                    material.EnableKeyword(Keywords._SHADING_EVALUATION_MODE_PER_PIXEL);
                    material.DisableKeyword(Keywords._SHADING_EVALUATION_MODE_PER_OBJECT);
                    break;
                case ShadingEvaluationMode.PerObject:
                    material.DisableKeyword(Keywords._SHADING_EVALUATION_MODE_PER_VERTEX);
                    material.DisableKeyword(Keywords._SHADING_EVALUATION_MODE_PER_PIXEL);
                    material.EnableKeyword(Keywords._SHADING_EVALUATION_MODE_PER_OBJECT);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }   
        }

        public static void SetupMaterialTextureFilterMode(Material material)
        {
            if (material == null) { throw new ArgumentNullException("material"); }

            TextureFilterMode textureFilterMode = (TextureFilterMode)material.GetFloat(PropertyIDs._TextureFilterMode);

            switch (textureFilterMode)
            {
                case TextureFilterMode.TextureImportSettings:
                    material.EnableKeyword(Keywords._TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_POINT);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_N64);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_N64_MIPMAPS);
                    break;

                case TextureFilterMode.Point:
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                    material.EnableKeyword(Keywords._TEXTURE_FILTER_MODE_POINT);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_N64);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_N64_MIPMAPS);
                    break;

                case TextureFilterMode.PointMipmaps:
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_POINT);
                    material.EnableKeyword(Keywords._TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_N64);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_N64_MIPMAPS);
                    break;

                case TextureFilterMode.N64:
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_POINT);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                    material.EnableKeyword(Keywords._TEXTURE_FILTER_MODE_N64);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_N64_MIPMAPS);
                    break;

                case TextureFilterMode.N64Mipmaps:
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_TEXTURE_IMPORT_SETTINGS);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_POINT);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_POINT_MIPMAPS);
                    material.DisableKeyword(Keywords._TEXTURE_FILTER_MODE_N64);
                    material.EnableKeyword(Keywords._TEXTURE_FILTER_MODE_N64_MIPMAPS);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }

        public static void SetupMaterialLightingMode(Material material, bool vertexColorSupported = true)
        {
            if (material == null)
                throw new ArgumentNullException("material");
                
            LightingMode lightingMode = (LightingMode)material.GetFloat(PropertyIDs._LightingMode);
            VertexColorMode vertexColorMode = vertexColorSupported
                ? (VertexColorMode)material.GetFloat(PropertyIDs._VertexColorMode)
                : VertexColorMode.Disabled;

            bool lightingBakedEnabled = material.GetFloat(PropertyIDs._LightingBaked) == 1;
            bool lightingVertexColorEnabled = vertexColorSupported && (vertexColorMode == VertexColorMode.Lighting);
            bool lightingDynamicEnabled = material.GetFloat(PropertyIDs._LightingDynamic) == 1;

            if (vertexColorMode == VertexColorMode.Color)
            {
                material.EnableKeyword(Keywords._VERTEX_COLOR_MODE_COLOR);
            }
            else
            {
                material.DisableKeyword(Keywords._VERTEX_COLOR_MODE_COLOR);
            }

            if (vertexColorMode == VertexColorMode.AlphaOnly)
            {
                material.EnableKeyword(Keywords._VERTEX_COLOR_MODE_ALPHA_ONLY);
            }
            else
            {
                material.DisableKeyword(Keywords._VERTEX_COLOR_MODE_ALPHA_ONLY);
            }

            if (vertexColorMode == VertexColorMode.ColorBackground)
            {
                material.EnableKeyword(Keywords._VERTEX_COLOR_MODE_COLOR_BACKGROUND);
            }
            else
            {
                material.DisableKeyword(Keywords._VERTEX_COLOR_MODE_COLOR_BACKGROUND);
            }

            if (vertexColorMode == VertexColorMode.Emission)
            {
                material.EnableKeyword(Keywords._VERTEX_COLOR_MODE_EMISSION);
            }
            else
            {
                material.DisableKeyword(Keywords._VERTEX_COLOR_MODE_EMISSION);
            }

            if (vertexColorMode == VertexColorMode.EmissionAndAlphaOnly)
            {
                material.EnableKeyword(Keywords._VERTEX_COLOR_MODE_EMISSION_AND_ALPHA_ONLY);
            }
            else
            {
                material.DisableKeyword(Keywords._VERTEX_COLOR_MODE_EMISSION_AND_ALPHA_ONLY);
            }

            if (vertexColorMode == VertexColorMode.SplitColorAndLighting)
            {
                material.EnableKeyword(Keywords._VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING);
            }
            else
            {
                material.DisableKeyword(Keywords._VERTEX_COLOR_MODE_SPLIT_COLOR_AND_LIGHTING);
            }

            switch (lightingMode)
            {
                case LightingMode.Unlit:
                {
                    material.DisableKeyword(Keywords._LIGHTING_BAKED_ON);
                    material.DisableKeyword(Keywords._VERTEX_COLOR_MODE_LIGHTING);
                    material.DisableKeyword(Keywords._LIGHTING_DYNAMIC_ON);
                    break;
                }
                case LightingMode.Lit:
                default: // Old versions of the material can have enum values serialized outside the range that we are currently supporting. Default these materials back to lit.
                {
                    if (lightingBakedEnabled)
                    {
                        material.EnableKeyword(Keywords._LIGHTING_BAKED_ON);
                    }
                    else
                    {
                        material.DisableKeyword(Keywords._LIGHTING_BAKED_ON);
                    }

                    if (lightingVertexColorEnabled)
                    {
                        material.EnableKeyword(Keywords._VERTEX_COLOR_MODE_LIGHTING);
                    }
                    else
                    {
                        material.DisableKeyword(Keywords._VERTEX_COLOR_MODE_LIGHTING);
                    }

                    if (lightingDynamicEnabled)
                    {
                        material.EnableKeyword(Keywords._LIGHTING_DYNAMIC_ON);
                    }
                    else
                    {
                        material.DisableKeyword(Keywords._LIGHTING_DYNAMIC_ON);
                    }
                    break;
                }
            }
        }

        public static void SetupMaterialLightingModeNoVertexColorSupported(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");
                
            LightingMode lightingMode = (LightingMode)material.GetFloat(PropertyIDs._LightingMode);

            bool lightingBakedEnabled = material.GetFloat(PropertyIDs._LightingBaked) == 1;
            bool lightingDynamicEnabled = material.GetFloat(PropertyIDs._LightingDynamic) == 1;

            switch (lightingMode)
            {
                case LightingMode.Unlit:
                {
                    material.DisableKeyword(Keywords._LIGHTING_BAKED_ON);
                    material.DisableKeyword(Keywords._LIGHTING_DYNAMIC_ON);
                    break;
                }
                case LightingMode.Lit:
                default: // Old versions of the material can have enum values serialized outside the range that we are currently supporting. Default these materials back to lit.
                {
                    if (lightingBakedEnabled)
                    {
                        material.EnableKeyword(Keywords._LIGHTING_BAKED_ON);
                    }
                    else
                    {
                        material.DisableKeyword(Keywords._LIGHTING_BAKED_ON);
                    }

                    if (lightingDynamicEnabled)
                    {
                        material.EnableKeyword(Keywords._LIGHTING_DYNAMIC_ON);
                    }
                    else
                    {
                        material.DisableKeyword(Keywords._LIGHTING_DYNAMIC_ON);
                    }
                    break;
                }
            }
        }

        public static void SetupMaterialBlendMode(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            SurfaceType surfaceType = (SurfaceType)material.GetFloat(PropertyIDs._Surface);
            RenderQueueCategory category = (RenderQueueCategory)(int)material.GetFloat(PropertyIDs._RenderQueueCategory);
            bool transparent = surfaceType == SurfaceType.Transparent;
            int renderQueueOffset = 0; // TODO: Expose options for user to offset within the queue.
            bool alphaClip = material.GetFloat(PropertyIDs._AlphaClip) == 1;

            material.renderQueue = GetRenderQueueFromCategory(category, transparent, renderQueueOffset, alphaClip);

            if (alphaClip)
            {
                material.EnableKeyword(Keywords._ALPHATEST_ON);
            }
            else
            {
                material.DisableKeyword(Keywords._ALPHATEST_ON);
            }

            
            if (surfaceType == SurfaceType.Opaque)
            {
                if (alphaClip)
                {
                    // TODO: Do I actually need to support these override tags? I'm not using them.
                    material.SetOverrideTag(Tags.RenderType, Tags.TransparentCutout);
                }
                else
                {
                    // TODO: Do I actually need to support these override tags? I'm not using them.
                    material.SetOverrideTag(Tags.RenderType, Tags.Opaque);
                }
                material.SetInt(PropertyIDs._BlendOp, (int)UnityEngine.Rendering.BlendOp.Add);
                material.SetInt(PropertyIDs._SrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt(PropertyIDs._DstBlend, (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt(PropertyIDs._ZWrite, 1);
                material.SetInt(PropertyIDs._ColorMask, (int)UnityEngine.Rendering.ColorWriteMask.All);
                material.DisableKeyword(Keywords._ALPHAPREMULTIPLY_ON);
                material.DisableKeyword(Keywords._BLENDMODE_TONEMAPPER_OFF);
                // material.SetShaderPassEnabled("ShadowCaster", true);
            }
            else // SurfaceType == SurfaceType.Transparent
            {
                PSXMaterialUtils.BlendMode blendMode = (PSXMaterialUtils.BlendMode)material.GetFloat(PropertyIDs._Blend);
                
                // Specific Transparent Mode Settings
                switch (blendMode)
                {
                    case PSXMaterialUtils.BlendMode.AlphaPostmultiply:
                        material.SetInt(PropertyIDs._BlendOp, (int)UnityEngine.Rendering.BlendOp.Add);
                        material.SetInt(PropertyIDs._SrcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt(PropertyIDs._DstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt(PropertyIDs._ColorMask, (int)UnityEngine.Rendering.ColorWriteMask.All);
                        material.DisableKeyword(Keywords._ALPHAPREMULTIPLY_ON);
                        material.DisableKeyword(Keywords._BLENDMODE_TONEMAPPER_OFF);
                        break;
                    case PSXMaterialUtils.BlendMode.AlphaPremultiply:
                        material.SetInt(PropertyIDs._BlendOp, (int)UnityEngine.Rendering.BlendOp.Add);
                        material.SetInt(PropertyIDs._SrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt(PropertyIDs._DstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt(PropertyIDs._ColorMask, (int)UnityEngine.Rendering.ColorWriteMask.All);
                        material.EnableKeyword(Keywords._ALPHAPREMULTIPLY_ON);
                        material.DisableKeyword(Keywords._BLENDMODE_TONEMAPPER_OFF);
                        break;
                    case PSXMaterialUtils.BlendMode.Additive:
                        material.SetInt(PropertyIDs._BlendOp, (int)UnityEngine.Rendering.BlendOp.Add);
                        material.SetInt(PropertyIDs._SrcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt(PropertyIDs._DstBlend, (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt(PropertyIDs._ColorMask, (int)UnityEngine.Rendering.ColorWriteMask.All);
                        material.DisableKeyword(Keywords._ALPHAPREMULTIPLY_ON);
                        material.DisableKeyword(Keywords._BLENDMODE_TONEMAPPER_OFF);
                        break;
                    case PSXMaterialUtils.BlendMode.Multiply:
                        material.SetInt(PropertyIDs._BlendOp, (int)UnityEngine.Rendering.BlendOp.Add);
                        material.SetInt(PropertyIDs._SrcBlend, (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt(PropertyIDs._DstBlend, (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt(PropertyIDs._ColorMask, (int)(UnityEngine.Rendering.ColorWriteMask.Red | UnityEngine.Rendering.ColorWriteMask.Green | UnityEngine.Rendering.ColorWriteMask.Blue));
                        material.DisableKeyword(Keywords._ALPHAPREMULTIPLY_ON);
                        material.EnableKeyword(Keywords._ALPHAMODULATE_ON);
                        material.EnableKeyword(Keywords._BLENDMODE_TONEMAPPER_OFF);
                        break;
                    case PSXMaterialUtils.BlendMode.Subtractive:
                        material.SetInt(PropertyIDs._BlendOp, (int)UnityEngine.Rendering.BlendOp.ReverseSubtract);
                        material.SetInt(PropertyIDs._SrcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt(PropertyIDs._DstBlend, (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt(PropertyIDs._ColorMask, (int)(UnityEngine.Rendering.ColorWriteMask.Red | UnityEngine.Rendering.ColorWriteMask.Green | UnityEngine.Rendering.ColorWriteMask.Blue));
                        material.DisableKeyword(Keywords._ALPHAPREMULTIPLY_ON);
                        material.DisableKeyword(Keywords._ALPHAMODULATE_ON);
                        material.EnableKeyword(Keywords._BLENDMODE_TONEMAPPER_OFF);
                        break;
                    default:
                        Debug.Assert(false, "Error: Encountered unsupported blendmode: " + blendMode);
                        break;
                }

                // General Transparent Material Settings
                // TODO: Do I actually need to support these override tags? I'm not using them.
                material.SetOverrideTag(Tags.RenderType, Tags.Transparent);
                material.SetInt(PropertyIDs._ZWrite, 0);
            }
        }

        public static void SetupMaterialFogKeyword(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            bool fog = material.GetFloat(PropertyIDs._FogWeight) > 0.0f;
            if (fog)
            {
                material.EnableKeyword(Keywords._FOG_ON);
            }
            else
            {
                material.DisableKeyword(Keywords._FOG_ON);
            }
        }

        public static void SetupMaterialReflectionKeyword(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            bool reflection = material.GetFloat(PropertyIDs._Reflection) == 1;
            if (reflection)
            {
                material.EnableKeyword(Keywords._REFLECTION_ON);
            }
            else
            {
                material.DisableKeyword(Keywords._REFLECTION_ON);
            }
        }

        public static void SetupMaterialBRDFModeKeyword(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            BRDFMode brdfMode = (BRDFMode)(int)material.GetFloat(PropertyIDs._BRDFMode);
            switch (brdfMode)
            {
                case BRDFMode.Lambert: material.EnableKeyword(Keywords._BRDF_MODE_LAMBERT); break;
                case BRDFMode.WrappedLighting: material.EnableKeyword(Keywords._BRDF_MODE_WRAPPED_LIGHTING); break;
                default: material.EnableKeyword(Keywords._BRDF_MODE_LAMBERT); break;
            }
        }

        public static void DoPopup(GUIContent label, MaterialProperty property, string[] options, MaterialEditor materialEditor)
        {
            if (property == null)
                throw new ArgumentNullException("property");

            EditorGUI.showMixedValue = property.hasMixedValue;

            var mode = property.floatValue;
            EditorGUI.BeginChangeCheck();
            mode = EditorGUILayout.Popup(label, (int)mode, options);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(label.text);
                property.floatValue = mode;
            }

            EditorGUI.showMixedValue = false;
        }

        // Helper to show texture and color properties
        public static Rect TextureColorProps(MaterialEditor materialEditor, GUIContent label, MaterialProperty textureProp, MaterialProperty colorProp, bool hdr = false)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.showMixedValue = textureProp.hasMixedValue;
            materialEditor.TexturePropertyMiniThumbnail(rect, textureProp, label.text, label.tooltip);
            EditorGUI.showMixedValue = false;

            if (colorProp != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = colorProp.hasMixedValue;
                int indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                Rect rectAfterLabel = new Rect(rect.x + EditorGUIUtility.labelWidth, rect.y,
                    EditorGUIUtility.fieldWidth, EditorGUIUtility.singleLineHeight);
                var col = EditorGUI.ColorField(rectAfterLabel, GUIContent.none, colorProp.colorValue, true,
                    false, hdr);
                EditorGUI.indentLevel = indentLevel;
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo(colorProp.displayName);
                    colorProp.colorValue = col;
                }
                EditorGUI.showMixedValue = false;
            }

            return rect;
        }

        public static int GetRenderQueueFromCategory(RenderQueueCategory category, bool transparent, int offset, bool alphaClip)
        {
            switch (category)
            {
                case RenderQueueCategory.Background: return PSXRenderQueue.ChangeType(transparent ? PSXRenderQueue.RenderQueueType.BackgroundTransparent : PSXRenderQueue.RenderQueueType.BackgroundOpaque, offset, alphaClip);
                case RenderQueueCategory.Main: return PSXRenderQueue.ChangeType(transparent ? PSXRenderQueue.RenderQueueType.MainTransparent : PSXRenderQueue.RenderQueueType.MainOpaque, offset, alphaClip);
                case RenderQueueCategory.UIOverlay: return PSXRenderQueue.ChangeType(transparent ? PSXRenderQueue.RenderQueueType.UIOverlayTransparent : PSXRenderQueue.RenderQueueType.UIOverlayOpaque, offset, alphaClip);
                default: throw new ArgumentException("category");
            }
        }

        public static void DrawAdvancedOptions(Material material, MaterialEditor materialEditor)
        {
            materialEditor.EnableInstancingField();
        }

        public static void DrawMainProperties(Material material, MaterialEditor materialEditor, MaterialProperty mainTexProp, MaterialProperty mainColorProp, MaterialProperty uvAnimationModeProp, MaterialProperty uvAnimationParametersFrameLimitProp, MaterialProperty uvAnimationParametersProp)
        {
            if (mainTexProp != null && mainColorProp != null) // Draw the mainTex, most shader will have at least a mainTex
            {
                materialEditor.TexturePropertySingleLine(Styles.mainTex, mainTexProp, mainColorProp);
                
                // TODO Temporary fix for lightmapping, to be replaced with attribute tag.
                if (material.HasProperty(PropertyIDs._MainTex))
                {
                    material.SetTexture(PropertyIDs._MainTex, mainTexProp.textureValue);
                    var mainTexTiling = mainTexProp.textureScaleAndOffset;
                    material.SetTextureScale(PropertyIDs._MainTex, new Vector2(mainTexTiling.x, mainTexTiling.y));
                    material.SetTextureOffset(PropertyIDs._MainTex, new Vector2(mainTexTiling.z, mainTexTiling.w));

                    DrawUVAnimationMode(uvAnimationModeProp, uvAnimationParametersFrameLimitProp, uvAnimationParametersProp);
                }

                DrawTextureFilterModeErrorMessagesForTexture(material, materialEditor, mainTexProp.textureValue, PropertyNames._MainTex);
            }
        }

        public static void DrawTextureFilterModeErrorMessagesForTexture(Material material, MaterialEditor materialEditor, Texture texture, string propertyName)
        {
            if (texture == null) { return; }

            TextureFilterMode textureFilterMode = (TextureFilterMode)material.GetFloat(PropertyIDs._TextureFilterMode);
            bool requiresBilinear = (textureFilterMode == TextureFilterMode.N64) || (textureFilterMode == TextureFilterMode.N64Mipmaps);
            bool requiresMipmaps = (textureFilterMode == TextureFilterMode.PointMipmaps) || (textureFilterMode == TextureFilterMode.N64Mipmaps);

            if (requiresBilinear && texture != null && texture.filterMode == FilterMode.Point)
            {
                EditorGUILayout.HelpBox(String.Format(Styles.textureFilterModeErrorBilinearDisabled, Styles.textureFilterModeNames[(int)textureFilterMode], propertyName), MessageType.Error);
            }

            if (requiresMipmaps && texture != null && texture.mipmapCount == 1 && (Mathf.Min(texture.width, texture.height) > 1))
            {
                EditorGUILayout.HelpBox(String.Format(Styles.textureFilterModeErrorMipmapsDisabled, Styles.textureFilterModeNames[(int)textureFilterMode], propertyName), MessageType.Error);
            }
        }

        public static void DrawEmissionProperties(
            Material material,
            MaterialEditor materialEditor,
            MaterialProperty emissionTextureProp,
            MaterialProperty emissionColorProp,
            MaterialProperty emissionBakedMultiplierProp)
        {
            // Emission for GI?
            bool emissive = materialEditor.EmissionEnabledProperty();
            bool hadEmissionTexture = emissionTextureProp.textureValue != null;

            EditorGUI.BeginDisabledGroup(!emissive);
            {
                // Texture and HDR color controls
                materialEditor.TexturePropertyWithHDRColor(Styles.emissionTex, emissionTextureProp,
                    emissionColorProp,
                    false);

                EditorGUI.BeginChangeCheck();
                float emissionBakedMultiplier = EditorGUILayout.FloatField(Styles.emissionBakedMultiplier, emissionBakedMultiplierProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                    emissionBakedMultiplierProp.floatValue = emissionBakedMultiplier;

                DrawTextureFilterModeErrorMessagesForTexture(material, materialEditor, emissionTextureProp.textureValue, PropertyNames._EmissionTexture);
            }
            EditorGUI.EndDisabledGroup();

            // If texture was assigned and color was black set color to white
            var brightness = emissionColorProp.colorValue.maxColorComponent;
            if (emissionTextureProp.textureValue != null && !hadEmissionTexture && brightness <= 0f)
                emissionColorProp.colorValue = Color.white;

            // HPSXRP does not support RealtimeEmissive. We set it to bake emissive and handle the emissive is black right.
            if (emissive)
            {
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                if (brightness <= 0f)
                    material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }
        }

        public static void DrawReflectionProperties(
            Material material,
            MaterialEditor materialEditor,
            MaterialProperty reflectionProp,
            MaterialProperty reflectionDirectionModeProp,
            MaterialProperty reflectionBlendModeProp,
            MaterialProperty reflectionCubemapProp,
            MaterialProperty reflectionTextureProp,
            MaterialProperty reflectionColorProp)
        {
            var reflection = true;
            var hadReflectionTexture = reflectionTextureProp.textureValue != null;

            EditorGUI.BeginChangeCheck();
            reflection = EditorGUILayout.Toggle(Styles.reflection, reflectionProp.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                reflectionProp.floatValue = reflection ? 1 : 0;

            EditorGUI.BeginDisabledGroup(!reflection);
            {
                DoPopup(Styles.reflectionDirectionMode, reflectionDirectionModeProp, Enum.GetNames(typeof(ReflectionDirectionMode)), materialEditor);
                DoPopup(Styles.reflectionBlendMode, reflectionBlendModeProp, Enum.GetNames(typeof(ReflectionBlendMode)), materialEditor);

                materialEditor.TexturePropertySingleLine(Styles.reflectionCubemap, reflectionCubemapProp);

                // Texture and HDR color controls
                materialEditor.TexturePropertyWithHDRColor(Styles.reflectionTexture, reflectionTextureProp, reflectionColorProp, false);
            }
            EditorGUI.EndDisabledGroup();

            // If texture was assigned and color was black set color to white
            var brightness = reflectionColorProp.colorValue.maxColorComponent;
            if (reflectionTextureProp.textureValue != null && !hadReflectionTexture && brightness <= 0f)
                reflectionColorProp.colorValue = Color.white;
        }

        public static void DrawTileOffset(MaterialEditor materialEditor, MaterialProperty textureProp)
        {
            materialEditor.TextureScaleOffsetProperty(textureProp);
        }

        public static void DrawRenderQueueCategory(MaterialEditor materialEditor, MaterialProperty renderQueueCategoryProp)
        {
            DoPopup(Styles.RenderQueueCategory, renderQueueCategoryProp, Enum.GetNames(typeof(RenderQueueCategory)), materialEditor);
        }

        public static void DrawTextureFilterMode(MaterialEditor materialEditor, MaterialProperty textureFilterModeProp)
        {
            DoPopup(Styles.TextureFilterMode, textureFilterModeProp, Enum.GetNames(typeof(TextureFilterMode)), materialEditor);
        }

        public static void DrawVertexColorMode(MaterialEditor materialEditor, MaterialProperty vertexColorModeProp, MaterialProperty vertexColorBlendModeProp)
        {
            DoPopup(Styles.VertexColorMode, vertexColorModeProp, Enum.GetNames(typeof(VertexColorMode)), materialEditor);

            VertexColorMode vertexColorMode = (VertexColorMode)(int)vertexColorModeProp.floatValue;
            if (vertexColorMode == VertexColorMode.Color)
            {
                DoPopup(Styles.VertexColorBlendMode, vertexColorBlendModeProp, Enum.GetNames(typeof(VertexColorBlendMode)), materialEditor);
            }
        }

        public static void DrawLightingMode(Material material, MaterialEditor materialEditor, MaterialProperty lightingModeProp, MaterialProperty lightingBakedProp, MaterialProperty lightingDynamicProp)
        {
            DoPopup(Styles.LightingMode, lightingModeProp, Enum.GetNames(typeof(LightingMode)), materialEditor);

            if ((lightingModeProp.floatValue != (float)LightingMode.Lit) && (lightingModeProp.floatValue != (float)LightingMode.Unlit))
            {
                // Old versions of the material can have enum values serialized outside the range that we are currently supporting. Default these materials back to lit.
                lightingModeProp.floatValue = (float)LightingMode.Lit;
            }

            if ((LightingMode)material.GetFloat(PropertyIDs._LightingMode) == LightingMode.Lit)
            {
                EditorGUI.BeginChangeCheck();
                bool lightingBakedEnabled = EditorGUILayout.Toggle(Styles.LightingBaked, lightingBakedProp.floatValue == 1);
                if (EditorGUI.EndChangeCheck())
                    lightingBakedProp.floatValue = lightingBakedEnabled ? 1 : 0;

                EditorGUI.BeginChangeCheck();
                bool lightingDynamicEnabled = EditorGUILayout.Toggle(Styles.LightingDynamic, lightingDynamicProp.floatValue == 1);
                if (EditorGUI.EndChangeCheck())
                    lightingDynamicProp.floatValue = lightingDynamicEnabled ? 1 : 0;
            }
        }

        public static void DrawShadingEvaluationMode(MaterialEditor materialEditor, MaterialProperty shadingEvaluationModeProp)
        {
            DoPopup(Styles.ShadingEvaluationMode, shadingEvaluationModeProp, Enum.GetNames(typeof(ShadingEvaluationMode)), materialEditor);
        }

        public static void DrawBRDFMode(MaterialEditor materialEditor, MaterialProperty brdfModeProp)
        {
            DoPopup(Styles.BRDFMode, brdfModeProp, Enum.GetNames(typeof(BRDFMode)), materialEditor);
        }

        public static void DrawSurfaceTypeAndBlendMode(Material material, MaterialEditor materialEditor, MaterialProperty surfaceTypeProp, MaterialProperty blendModeProp)
        {
            DoPopup(Styles.surfaceType, surfaceTypeProp, Enum.GetNames(typeof(SurfaceType)), materialEditor);

            if ((SurfaceType)material.GetFloat(PropertyIDs._Surface) == SurfaceType.Transparent)
            {
                DoPopup(Styles.blendingMode, blendModeProp, Enum.GetNames(typeof(PSXMaterialUtils.BlendMode)), materialEditor);
            }
        }

        public static void DrawCullingSettings(Material material, MaterialEditor materialEditor, MaterialProperty cullingProp, MaterialProperty doubleSidedNormalModeProp, MaterialProperty doubleSidedConstantsProp)
        {
            bool doubleSidedNormalModeNeedsUpdate = false;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = cullingProp.hasMixedValue;
            var culling = (RenderFace)cullingProp.floatValue;
            culling = (RenderFace)EditorGUILayout.EnumPopup(Styles.cullingText, culling);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(Styles.cullingText.text);
                cullingProp.floatValue = (float)culling;
                material.doubleSidedGI = (RenderFace)cullingProp.floatValue != RenderFace.Front;

                doubleSidedNormalModeNeedsUpdate = true;
            }

            EditorGUI.showMixedValue = false;

            if (culling == RenderFace.Back || culling == RenderFace.Both)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = doubleSidedNormalModeProp.hasMixedValue;
                DoubleSidedNormalMode doubleSidedNormalMode = (DoubleSidedNormalMode)doubleSidedNormalModeProp.floatValue;
                doubleSidedNormalMode = (DoubleSidedNormalMode)EditorGUILayout.EnumPopup(Styles.doubleSidedNormalMode, doubleSidedNormalMode);
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo(Styles.doubleSidedNormalMode.text);
                    doubleSidedNormalModeProp.floatValue = (float)doubleSidedNormalMode;

                    doubleSidedNormalModeNeedsUpdate = true;
                }
            }
            else
            {
                doubleSidedNormalModeProp.floatValue = (float)DoubleSidedNormalMode.Front;
            }

            if (doubleSidedNormalModeNeedsUpdate)
            {
                DoubleSidedNormalMode doubleSidedNormalMode = (culling == RenderFace.Front) ? DoubleSidedNormalMode.Front : (DoubleSidedNormalMode)doubleSidedNormalModeProp.floatValue;
                switch (doubleSidedNormalMode)
                {
                    case DoubleSidedNormalMode.Front: // Front mode (in tangent space)
                        doubleSidedConstantsProp.vectorValue = new Vector4(1.0f, 1.0f, 1.0f, 0.0f);
                        break;

                    case DoubleSidedNormalMode.Flip: // Flip mode (in tangent space)
                        doubleSidedConstantsProp.vectorValue = new Vector4(-1.0f, -1.0f, -1.0f, 0.0f);
                        break;

                    default:
                        Debug.Assert(false);
                        break;
                }
            }
        }

        public static void DrawAlphaClippingSettings(Material material, MaterialProperty alphaClipProp, MaterialProperty alphaClippingDitherIsEnabledProp, MaterialProperty alphaClippingScaleBiasMinMaxProp)
        {
            // Alpha Clipping can be applied to any blend mode.
            // Previously, it only worked for opaque surfaces, since technically you can achive higher quality results with regular transparency blend modes if those are already in use.
            // However, the community determined it was valuble for creative purposes to be able to apply dithered alpha clipping to transparent blend modes as well.
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = alphaClipProp.hasMixedValue;
            var alphaClipEnabled = EditorGUILayout.Toggle(Styles.alphaClipText, alphaClipProp.floatValue == 1);
            if (EditorGUI.EndChangeCheck())
                alphaClipProp.floatValue = alphaClipEnabled ? 1 : 0;
            EditorGUI.showMixedValue = false;

            bool alphaClippingDitherIsEnabledChanged = false;
            if (alphaClipProp.floatValue > 0.5f)
            {
                EditorGUI.BeginChangeCheck();
                bool alphaClippingDitherIsEnabled = EditorGUILayout.Toggle(Styles.alphaClippingDitherText, alphaClippingDitherIsEnabledProp.floatValue == 1);
                if (EditorGUI.EndChangeCheck())
                {
                    alphaClippingDitherIsEnabledProp.floatValue = alphaClippingDitherIsEnabled ? 1 : 0;
                    alphaClippingDitherIsEnabledChanged = true;
                }
            }

            if (alphaClipEnabled)
            {
                bool alphaClippingDitherIsEnabled = alphaClippingDitherIsEnabledProp.floatValue > 0.5f;
                if (alphaClippingDitherIsEnabled)
                {
                    // When alpha clipping is enabled, we draw a min/max range slider.
                    EditorGUI.BeginChangeCheck();
                    float alphaClippingMin = alphaClippingScaleBiasMinMaxProp.vectorValue.z;
                    float alphaClippingMax = alphaClippingDitherIsEnabledChanged ? Mathf.Min(1.0f, alphaClippingMin + 1e-5f) : alphaClippingScaleBiasMinMaxProp.vectorValue.w;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.MinMaxSlider(Styles.alphaClippingScaleBiasMinMaxText, ref alphaClippingMin, ref alphaClippingMax, 0.0f, 1.0f);
                    alphaClippingMin = EditorGUILayout.FloatField(alphaClippingMin, GUILayout.Width(50));
                    alphaClippingMax = EditorGUILayout.FloatField(alphaClippingMax, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                    if (EditorGUI.EndChangeCheck() || alphaClippingDitherIsEnabledChanged)
                    {
                        alphaClippingMin = Mathf.Clamp(alphaClippingMin, 0.0f, 1.0f);
                        alphaClippingMax = Mathf.Clamp(alphaClippingMax, Mathf.Min(1.0f, alphaClippingMin + 1e-5f), 1.0f);
                        alphaClippingScaleBiasMinMaxProp.vectorValue = new Vector4(1.0f / (alphaClippingMax - alphaClippingMin), -alphaClippingMin / (alphaClippingMax - alphaClippingMin), alphaClippingMin, alphaClippingMax);
                    }

                }
                else
                {
                    // When alpha clipping is disabled, we simply draw a regular slider, and assign it's value to the min value.
                    EditorGUI.BeginChangeCheck();
                    float alphaClippingMin = alphaClippingDitherIsEnabledChanged
                        ? ((alphaClippingScaleBiasMinMaxProp.vectorValue.z + alphaClippingScaleBiasMinMaxProp.vectorValue.w) * 0.5f)
                        : alphaClippingScaleBiasMinMaxProp.vectorValue.z;
                    alphaClippingMin = EditorGUILayout.Slider(Styles.alphaClippingScaleBiasMinMaxText, alphaClippingMin, 0.0f, 1.0f);
                    if (EditorGUI.EndChangeCheck() || alphaClippingDitherIsEnabledChanged)
                    {
                        float alphaClippingMax = Mathf.Min(alphaClippingMin + 1e-5f, 1.0f);
                        alphaClippingScaleBiasMinMaxProp.vectorValue = new Vector4(1.0f / (alphaClippingMax - alphaClippingMin), -alphaClippingMin / (alphaClippingMax - alphaClippingMin), alphaClippingMin, alphaClippingMax);
                    }
                }
            }
        }

        public static void DrawAffineTextureWarpingWeight(MaterialProperty affineTextureWarpingWeightProp)
        {
            EditorGUI.BeginChangeCheck();
            var affineTextureWarpingWeight = EditorGUILayout.Slider(Styles.affineTextureWarpingWeight, affineTextureWarpingWeightProp.floatValue, 0.0f, 1.0f);
            if (EditorGUI.EndChangeCheck())
            {
                affineTextureWarpingWeightProp.floatValue = affineTextureWarpingWeight;
            }
        }

        public static void DrawFogWeight(MaterialProperty fogWeightProp)
        {
            EditorGUI.BeginChangeCheck();
            var fogWeight = EditorGUILayout.Slider(Styles.fogWeight, fogWeightProp.floatValue, 0.0f, 1.0f);
            if (EditorGUI.EndChangeCheck())
            {
                fogWeightProp.floatValue = fogWeight;
            }
        }

        public static void DrawDrawDistanceOverride(MaterialProperty drawDistanceOverrideModeProp, MaterialProperty drawDistanceOverrideProp)
        {
            EditorGUI.BeginChangeCheck();
            var drawDistanceOverrideMode = (DrawDistanceOverrideMode)EditorGUILayout.EnumPopup(Styles.drawDistanceOverrideMode, (DrawDistanceOverrideMode)(int)drawDistanceOverrideModeProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                drawDistanceOverrideModeProp.floatValue = (float)(int)drawDistanceOverrideMode;
            }

            if (drawDistanceOverrideMode != DrawDistanceOverrideMode.None && drawDistanceOverrideMode != DrawDistanceOverrideMode.Disabled)
            {
                EditorGUI.BeginChangeCheck();
                var drawDistanceOverride = EditorGUILayout.FloatField(Styles.drawDistanceOverride, drawDistanceOverrideProp.vectorValue.x);
                if (EditorGUI.EndChangeCheck())
                {
                    drawDistanceOverrideProp.vectorValue = new Vector2(drawDistanceOverride, drawDistanceOverride * drawDistanceOverride);
                }
            }
        }

        public static void DrawPrecisionGeometryOverride(MaterialProperty precisionGeometryOverrideModeProp, MaterialProperty precisionGeometryOverrideParametersProp, MaterialProperty precisionGeometryWeightDeprecatedProp)
        {
            bool needsMigration = (precisionGeometryWeightDeprecatedProp.floatValue <= 0.999f);
            if (needsMigration)
            {
                // There isn't a perfect migration availible between our old weight parameterization, and our override parameterization.
                // The best we can do is detect if the weight was set to zero, and simply disable precision geometry.
                if (precisionGeometryWeightDeprecatedProp.floatValue < 1e-5f)
                {
                    precisionGeometryOverrideModeProp.floatValue = (float)(int)PrecisionGeometryOverrideMode.Disabled;
                    precisionGeometryOverrideParametersProp.vectorValue = new Vector2(0.0f, 0.0f);
                    precisionGeometryWeightDeprecatedProp.floatValue = 0.0f;
                }
                else
                {
                    precisionGeometryOverrideModeProp.floatValue = (float)(int)PrecisionGeometryOverrideMode.None;
                    precisionGeometryOverrideParametersProp.vectorValue = new Vector2(0.0f, 0.0f);
                    precisionGeometryWeightDeprecatedProp.floatValue = 0.0f;
                }
            }

            EditorGUI.BeginChangeCheck();
            bool modeChanged = false;
            var precisionGeometryOverrideMode = (PrecisionGeometryOverrideMode)EditorGUILayout.EnumPopup(Styles.precisionGeometryOverrideMode, (PrecisionGeometryOverrideMode)(int)precisionGeometryOverrideModeProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                modeChanged = true;
                precisionGeometryOverrideModeProp.floatValue = (float)(int)precisionGeometryOverrideMode;
            }

            if (precisionGeometryOverrideMode != PrecisionGeometryOverrideMode.None && precisionGeometryOverrideMode != PrecisionGeometryOverrideMode.Disabled)
            {
                EditorGUI.BeginChangeCheck();
                var precisionGeometryOverride = EditorGUILayout.FloatField(Styles.precisionGeometryOverride, precisionGeometryOverrideParametersProp.vectorValue.z);
                if (EditorGUI.EndChangeCheck() || modeChanged)
                {
                    switch (precisionGeometryOverrideMode)
                    {
                        case PrecisionGeometryOverrideMode.Override:
                        {
                            // Fast path: precompute the scale and scale inverse transformation cpu side since we will be fully overriding.
                            precisionGeometryOverride = Mathf.Clamp01(precisionGeometryOverride);
                            Vector2 precisionGeometryScaleAndScaleInverse = PSXRenderPipeline.ComputePrecisionGeometryParameters(precisionGeometryOverride);
                            precisionGeometryOverrideParametersProp.vectorValue = new Vector3(precisionGeometryScaleAndScaleInverse.x, precisionGeometryScaleAndScaleInverse.y, precisionGeometryOverride);
                            break;
                        }
                        case PrecisionGeometryOverrideMode.Multiply:
                        {
                            precisionGeometryOverride = Mathf.Max(1e-5f, precisionGeometryOverride);

                            // Scale and Scale Inverse transformation will happen in shader. Just need the raw precisionGeometryOverride value.
                            precisionGeometryOverrideParametersProp.vectorValue = new Vector3(1.0f, 1.0f, precisionGeometryOverride);
                            break;
                        }
                        default:
                        {
                            // Scale and Scale Inverse transformation will happen in shader. Just need the raw precisionGeometryOverride value.
                            precisionGeometryOverrideParametersProp.vectorValue = new Vector3(1.0f, 1.0f, precisionGeometryOverride);
                            break;
                        }
                    }
                }
            }
        }

        public static void DrawPrecisionColorOverride(MaterialProperty precisionColorOverrideModeProp, MaterialProperty precisionColorOverrideParametersProp)
        {
            EditorGUI.BeginChangeCheck();
            bool modeChanged = false;
            var precisionColorOverrideMode = (PrecisionColorOverrideMode)EditorGUILayout.EnumPopup(Styles.precisionColorOverrideMode, (PrecisionColorOverrideMode)(int)precisionColorOverrideModeProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                modeChanged = true;
                precisionColorOverrideModeProp.floatValue = (float)(int)precisionColorOverrideMode;

                if (precisionColorOverrideMode == PrecisionColorOverrideMode.Disabled)
                {
                    precisionColorOverrideParametersProp.vectorValue = new Vector3(255.0f, 1.0f / 255.0f, 1.0f);
                }
            }

            if (precisionColorOverrideMode != PrecisionColorOverrideMode.None && precisionColorOverrideMode != PrecisionColorOverrideMode.Disabled)
            {
                EditorGUI.BeginChangeCheck();
                var precisionColorOverride = EditorGUILayout.FloatField(Styles.precisionColorOverride, precisionColorOverrideParametersProp.vectorValue.z);
                if (EditorGUI.EndChangeCheck() || modeChanged)
                {
                    switch (precisionColorOverrideMode)
                    {
                        case PrecisionColorOverrideMode.Override:
                        {
                            precisionColorOverride = modeChanged ? 1.0f : precisionColorOverride; // Assign default.

                            // Fast path: precompute the scale and scale inverse transformation cpu side since we will be fully overriding.
                            precisionColorOverride = Mathf.Clamp01(precisionColorOverride);
                            precisionColorOverride = Mathf.Floor(precisionColorOverride * 7.0f + 0.5f) / 7.0f;
                            float precisionColorScale = Mathf.Pow(2.0f, precisionColorOverride * 7.0f + 1.0f) - 1.0f;
                            float precisionColorScaleInverse = 1.0f / precisionColorScale;
                            precisionColorOverrideParametersProp.vectorValue = new Vector3(precisionColorScale, precisionColorScaleInverse, precisionColorOverride);
                            break;
                        }
                        case PrecisionColorOverrideMode.Multiply:
                        {
                            precisionColorOverride = modeChanged ? 1.0f : precisionColorOverride; // Assign default.

                            precisionColorOverride = Mathf.Max(1e-5f, precisionColorOverride);

                            // Scale and Scale Inverse transformation will happen in shader. Just need the raw precisionGeometryOverride value.
                            precisionColorOverrideParametersProp.vectorValue = new Vector3(255.0f, 1.0f / 255.0f, precisionColorOverride);
                            break;
                        }
                        case PrecisionColorOverrideMode.Add:
                        {
                            precisionColorOverride = modeChanged ? 0.0f : Mathf.Clamp(precisionColorOverride, -1.0f, 1.0f); // Assign default.


                            // Scale and Scale Inverse transformation will happen in shader. Just need the raw precisionGeometryOverride value.
                            precisionColorOverrideParametersProp.vectorValue = new Vector3(255.0f, 1.0f / 255.0f, precisionColorOverride);
                            break;
                        }
                        default:
                        {
                            // Scale and Scale Inverse transformation will happen in shader. Just need the raw precisionGeometryOverride value.
                            precisionColorOverrideParametersProp.vectorValue = new Vector3(255.0f, 1.0f / 255.0f, precisionColorOverride);
                            break;
                        }
                    }
                }
            }
        }

        public static void DrawUVAnimationMode(MaterialProperty uvAnimationModeProp, MaterialProperty uvAnimationParametersFrameLimitProp, MaterialProperty uvAnimationParametersProp)
        {
            bool modeChanged = false;
            EditorGUI.BeginChangeCheck();
            var uvAnimationMode = (UVAnimationMode)EditorGUILayout.EnumPopup(Styles.uvAnimationMode, (UVAnimationMode)(int)uvAnimationModeProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                uvAnimationModeProp.floatValue = (float)(int)uvAnimationMode;
                modeChanged = true;
            }

            Vector4 uvAnimationParameters = uvAnimationParametersProp.vectorValue;
            bool needsUpdate = modeChanged;
            switch (uvAnimationMode)
            {
                case UVAnimationMode.None: break;
                case UVAnimationMode.PanLinear:
                {
                    Vector2 panLinearVelocity = modeChanged ? new Vector2(1.0f, 1.0f) : new Vector2(uvAnimationParameters.x, uvAnimationParameters.y);

                    EditorGUI.BeginChangeCheck();
                    panLinearVelocity = EditorGUILayout.Vector2Field(Styles.uvAnimationPanLinearVelocity, panLinearVelocity);
                    if (EditorGUI.EndChangeCheck() || modeChanged)
                    {
                        needsUpdate = true;
                        uvAnimationParameters = new Vector4(panLinearVelocity.x, panLinearVelocity.y, 0.0f, 0.0f);
                    }
                    break;
                }
                case UVAnimationMode.PanSin:
                {
                    uvAnimationParameters = modeChanged ? new Vector4(1.0f, 1.0f, 1.0f, 1.0f) : uvAnimationParameters;
                    Vector2 panSinFrequency = new Vector2(uvAnimationParameters.x, uvAnimationParameters.y);
                    EditorGUI.BeginChangeCheck();
                    panSinFrequency = EditorGUILayout.Vector2Field(Styles.uvAnimationPanSinFrequency, panSinFrequency);
                    if (EditorGUI.EndChangeCheck() || modeChanged)
                    {
                        needsUpdate = true;
                        uvAnimationParameters.x = panSinFrequency.x;
                        uvAnimationParameters.y = panSinFrequency.y;
                    }

                    Vector2 panSinScale = new Vector2(uvAnimationParameters.z, uvAnimationParameters.w);
                    EditorGUI.BeginChangeCheck();
                    panSinScale = EditorGUILayout.Vector2Field(Styles.uvAnimationPanSinScale, panSinScale);
                    if (EditorGUI.EndChangeCheck() || modeChanged)
                    {
                        needsUpdate = true;
                        uvAnimationParameters.z = panSinScale.x;
                        uvAnimationParameters.w = panSinScale.y;
                    }
                    break;
                }
                case UVAnimationMode.Flipbook:
                {

                    int flipbookTilesX = modeChanged ? 4 : (int)uvAnimationParameters.x;
                    int flipbookTilesY = modeChanged ? 4 : (int)uvAnimationParameters.y;
                    float flipbookFrequency = modeChanged ? 10 : uvAnimationParameters.z;
                    EditorGUI.BeginChangeCheck();

                    flipbookTilesX = EditorGUILayout.IntField(Styles.uvAnimationFlipbookTilesX, flipbookTilesX);
                    flipbookTilesY = EditorGUILayout.IntField(Styles.uvAnimationFlipbookTilesY, flipbookTilesY);
                    flipbookFrequency = EditorGUILayout.FloatField(Styles.uvAnimationFlipbookFrequency, flipbookFrequency);

                    if (EditorGUI.EndChangeCheck() || modeChanged)
                    {
                        needsUpdate = true;
                        uvAnimationParameters = new Vector4((float)flipbookTilesX, (float)flipbookTilesY, flipbookFrequency, 0);
                    }

                    break;
                }
                default: break;
            }

            if (needsUpdate)
            {
                uvAnimationParametersProp.vectorValue = uvAnimationParameters;
            }

            if (uvAnimationMode != UVAnimationMode.None)
            {
                bool frameLimitNeedsUpdate = false;
                bool frameLimitAssignDefault = false;
                EditorGUI.BeginChangeCheck();
                bool frameLimitEnabled = EditorGUILayout.Toggle(Styles.uvAnimationFrameLimitEnabled, uvAnimationParametersFrameLimitProp.vectorValue.x > 0.5f);
                if (EditorGUI.EndChangeCheck())
                {
                    frameLimitNeedsUpdate = true;
                    if (frameLimitEnabled)
                    {
                        // Frame limit was just toggled on. Assign the default frame limit value.
                        frameLimitAssignDefault = true;
                    }
                }

                int frameLimit = 60;
                if (frameLimitEnabled)
                {
                    EditorGUI.BeginChangeCheck();
                    frameLimit = frameLimitAssignDefault ? 60 : (int)uvAnimationParametersFrameLimitProp.vectorValue.y;
                    frameLimit = EditorGUILayout.DelayedIntField(Styles.uvAnimationFrameLimit, frameLimit);
                    if (EditorGUI.EndChangeCheck() || frameLimitAssignDefault)
                    {
                        frameLimitNeedsUpdate = true;
                        frameLimit = Mathf.Clamp(frameLimit, 1, 60);
                    }
                }
                
                if (frameLimitNeedsUpdate)
                {
                    uvAnimationParametersFrameLimitProp.vectorValue = new Vector2(frameLimitEnabled ? 1.0f : 0.0f, (float)frameLimit);
                }
            }
        }

        static public bool TextureHasAlpha(Texture2D inTex)
        {
            if (inTex != null)
            {
                return GraphicsFormatUtility.HasAlphaChannel(GraphicsFormatUtility.GetGraphicsFormat(inTex.format, true));
            }
            return false;
        }
    }
}