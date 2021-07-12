using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;
using static HauntedPSX.RenderPipelines.PSX.Editor.PSXMaterialUtils;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    // internal class PSXTerrainShaderGUI : UnityEditor.ShaderGUI, ITerrainLayerCustomUI
    internal class PSXTerrainShaderGUI : UnityEditor.ShaderGUI, ITerrainLayerCustomUI
    {
        private class StylesLayer
        {
            public readonly GUIContent diffuseTexture = new GUIContent("Diffuse");
            public readonly GUIContent colorTint = new GUIContent("Color Tint");
            public readonly GUIContent opacityAsDensity = new GUIContent("Opacity as Density", "Enable Density Blend (if unchecked, opacity is used as Smoothness)");
        }

        protected MaterialEditor materialEditor { get; set; }

        protected MaterialProperty textureFilterModeProp { get; set; }

        // protected MaterialProperty vertexColorModeProp { get; set; }

        protected MaterialProperty renderQueueCategoryProp { get; set; }

        protected MaterialProperty lightingModeProp { get; set; }

        protected MaterialProperty lightingBakedProp { get; set; }

        protected MaterialProperty lightingDynamicProp { get; set; }

        protected MaterialProperty shadingEvaluationModeProp { get; set; }

        protected MaterialProperty brdfModeProp { get; set; }

        // protected MaterialProperty surfaceTypeProp { get; set; }

        // protected MaterialProperty blendModeProp { get; set; }

        protected MaterialProperty cullingProp { get; set; }

        // protected MaterialProperty alphaClipProp { get; set; }

        // protected MaterialProperty alphaClippingDitherIsEnabledProp { get; set; }

        protected MaterialProperty affineTextureWarpingWeightProp { get; set; }

        protected MaterialProperty precisionGeometryWeightDeprecatedProp { get; set; }

        protected MaterialProperty precisionGeometryOverrideModeProp { get; set; }

        protected MaterialProperty precisionGeometryOverrideParametersProp { get; set; }

        protected MaterialProperty precisionColorOverrideModeProp { get; set; }

        protected MaterialProperty precisionColorOverrideParametersProp { get; set; }

        protected MaterialProperty fogWeightProp { get; set; }

        // // Common Surface Input properties

        // protected MaterialProperty mainTexProp { get; set; }

        // protected MaterialProperty mainColorProp { get; set; }

        // protected MaterialProperty emissionTextureProp { get; set; }

        // protected MaterialProperty emissionColorProp { get; set; }

        // protected MaterialProperty emissionBakedMultiplierProp { get; set; }

        // protected MaterialProperty reflectionProp { get; set; }

        // protected MaterialProperty reflectionCubemapProp { get; set; }

        // protected MaterialProperty reflectionTextureProp { get; set; }

        // protected MaterialProperty reflectionColorProp { get; set; }

        // protected MaterialProperty reflectionBlendModeProp { get; set; }

        public bool m_FirstTimeApply = true;

        private bool m_SurfaceOptionsFoldout = false;
        // private bool m_SurfaceInputsFoldout = false;
        // private bool m_AdvancedFoldout = false;

        static StylesLayer s_Styles = null;
        private static StylesLayer styles { get { if (s_Styles == null) s_Styles = new StylesLayer(); return s_Styles; } }

        public PSXTerrainShaderGUI()
        {
        }

        public virtual void FindProperties(MaterialProperty[] properties)
        {
            textureFilterModeProp = FindProperty(PropertyNames._TextureFilterMode, properties);
            // vertexColorModeProp = FindProperty(PropertyNames._VertexColorMode, properties);
            renderQueueCategoryProp = FindProperty(PropertyNames._RenderQueueCategory, properties);
            lightingModeProp = FindProperty(PropertyNames._LightingMode, properties);
            lightingBakedProp = FindProperty(PropertyNames._LightingBaked, properties);
            lightingDynamicProp = FindProperty(PropertyNames._LightingDynamic, properties);
            shadingEvaluationModeProp = FindProperty(PropertyNames._ShadingEvaluationMode, properties);
            brdfModeProp = FindProperty(PropertyNames._BRDFMode, properties);
            // surfaceTypeProp = FindProperty(PropertyNames._Surface, properties);
            // blendModeProp = FindProperty(PropertyNames._Blend, properties);
            cullingProp = FindProperty(PropertyNames._Cull, properties);
            // alphaClipProp = FindProperty(PropertyNames._AlphaClip, properties);
            // alphaClippingDitherIsEnabledProp = FindProperty(PropertyNames._AlphaClippingDitherIsEnabled, properties);
            affineTextureWarpingWeightProp = FindProperty(PropertyNames._AffineTextureWarpingWeight, properties);
            precisionGeometryWeightDeprecatedProp = FindProperty(PropertyNames._PrecisionGeometryWeightDeprecated, properties);
            precisionGeometryOverrideModeProp = FindProperty(PropertyNames._PrecisionGeometryOverrideMode, properties);
            precisionGeometryOverrideParametersProp = FindProperty(PropertyNames._PrecisionGeometryOverrideParameters, properties);
            precisionColorOverrideModeProp = FindProperty(PropertyNames._PrecisionColorOverrideMode, properties);
            precisionColorOverrideParametersProp = FindProperty(PropertyNames._PrecisionColorOverrideParameters, properties);
            fogWeightProp = FindProperty(PropertyNames._FogWeight, properties);
            // mainTexProp = FindProperty(PropertyNames._MainTex, properties, false);
            // mainColorProp = FindProperty(PropertyNames._MainColor, properties, false);
            // emissionTextureProp = FindProperty(PropertyNames._EmissionTexture, properties, false);
            // emissionColorProp = FindProperty(PropertyNames._EmissionColor, properties, false);
            // emissionBakedMultiplierProp = FindProperty(PropertyNames._EmissionBakedMultiplier, properties, false);
            // reflectionProp = FindProperty(PropertyNames._Reflection, properties, false);
            // reflectionCubemapProp = FindProperty(PropertyNames._ReflectionCubemap, properties, false);
            // reflectionTextureProp = FindProperty(PropertyNames._ReflectionTexture, properties, false);
            // reflectionColorProp = FindProperty(PropertyNames._ReflectionColor, properties, false);
            // reflectionBlendModeProp = FindProperty(PropertyNames._ReflectionBlendMode, properties, false);
        }

        // material changed check
        public void MaterialChanged(Material material)
        {
            if (material == null) { throw new ArgumentNullException("material"); }

            // PSXMaterialUtils.SetMaterialKeywords(material);

            // Clear all keywords for fresh start
            PSXMaterialUtils.ClearMaterialKeywords(material);

            PSXMaterialUtils.SetupMaterialTextureFilterMode(material);
            PSXMaterialUtils.SetupMaterialLightingModeNoVertexColorSupported(material);
            PSXMaterialUtils.SetupMaterialShadingEvaluationMode(material);
            PSXMaterialUtils.SetupMaterialBRDFModeKeyword(material);
            // PSXMaterialUtils.SetupMaterialBlendMode(material);
            PSXMaterialUtils.SetupMaterialFogKeyword(material);
            // PSXMaterialUtils.SetupMaterialReflectionKeyword(material);
            // PSXMaterialUtils.SetupMaterialEmissionKeyword(material);
        }

        public override void OnGUI(MaterialEditor materialEditorIn, MaterialProperty[] properties)
        {
            if (materialEditorIn == null)
                throw new ArgumentNullException("materialEditorIn");

            FindProperties(properties); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
            materialEditor = materialEditorIn;
            Material material = materialEditor.target as Material;

            // Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
            // material to a hpsx shader.
            if (m_FirstTimeApply)
            {
                OnOpenGUI(material, materialEditorIn);
                m_FirstTimeApply = false;
            }

            ShaderPropertiesGUI(material);
        }

        public virtual void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            // Foldout states
            m_SurfaceOptionsFoldout = true;
            // m_SurfaceInputsFoldout = true;
            // m_AdvancedFoldout = false;

            foreach (var obj in  materialEditor.targets)
                MaterialChanged((Material)obj);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            EditorGUI.BeginChangeCheck();

            m_SurfaceOptionsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_SurfaceOptionsFoldout, Styles.SurfaceOptions);
            if(m_SurfaceOptionsFoldout)
            {
                DrawSurfaceOptions(material);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            // m_SurfaceInputsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_SurfaceInputsFoldout, Styles.SurfaceInputs);
            // if (m_SurfaceInputsFoldout)
            // {
            //     DrawSurfaceInputs(material);
            //     EditorGUILayout.Space();
            // }
            // EditorGUILayout.EndFoldoutHeaderGroup();

            // m_AdvancedFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_AdvancedFoldout, Styles.AdvancedLabel);
            // if (m_AdvancedFoldout)
            // {
            //     DrawAdvancedOptions(material);
            //     EditorGUILayout.Space();
            // }
            // EditorGUILayout.EndFoldoutHeaderGroup();

            // DrawAdditionalFoldouts(material);

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in  materialEditor.targets)
                    MaterialChanged((Material)obj);
            }
        }

        public void DrawSurfaceOptions(Material material)
        {
            PSXMaterialUtils.DrawRenderQueueCategory(materialEditor, renderQueueCategoryProp);
            PSXMaterialUtils.DrawTextureFilterMode(materialEditor, textureFilterModeProp);
            // PSXMaterialUtils.DrawVertexColorMode(materialEditor, vertexColorModeProp);
            PSXMaterialUtils.DrawLightingMode(material, materialEditor, lightingModeProp, lightingBakedProp, lightingDynamicProp);
            PSXMaterialUtils.DrawShadingEvaluationMode(materialEditor, shadingEvaluationModeProp);
            PSXMaterialUtils.DrawBRDFMode(materialEditor, brdfModeProp);
            // PSXMaterialUtils.DrawSurfaceTypeAndBlendMode(material, materialEditor, surfaceTypeProp, blendModeProp);
            // PSXMaterialUtils.DrawCullingSettings(material, materialEditor, cullingProp); // HACK FIXME: 
            // PSXMaterialUtils.DrawAlphaClippingSettings(material, alphaClipProp, alphaClippingDitherIsEnabledProp);

            PSXMaterialUtils.DrawAffineTextureWarpingWeight(affineTextureWarpingWeightProp);
            PSXMaterialUtils.DrawFogWeight(fogWeightProp);
            PSXMaterialUtils.DrawPrecisionGeometryOverride(precisionGeometryOverrideModeProp, precisionGeometryOverrideParametersProp, precisionGeometryWeightDeprecatedProp);
            PSXMaterialUtils.DrawPrecisionColorOverride(precisionColorOverrideModeProp, precisionColorOverrideParametersProp);
        }

        bool ITerrainLayerCustomUI.OnTerrainLayerGUI(TerrainLayer terrainLayer, Terrain terrain)
        {
            var terrainLayers = terrain.terrainData.terrainLayers;

            terrainLayer.diffuseTexture = EditorGUILayout.ObjectField(styles.diffuseTexture, terrainLayer.diffuseTexture, typeof(Texture2D), false) as Texture2D;
            // TerrainLayerUtility.ValidateDiffuseTextureUI(terrainLayer.diffuseTexture);

            var diffuseRemapMin = terrainLayer.diffuseRemapMin;
            var diffuseRemapMax = terrainLayer.diffuseRemapMax;
            EditorGUI.BeginChangeCheck();

            bool enableDensity = false;
            if (terrainLayer.diffuseTexture != null)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.y += 16 + 4;
                rect.width = EditorGUIUtility.labelWidth + 64;
                rect.height = 16;

                ++EditorGUI.indentLevel;

                var diffuseTint = new Color(diffuseRemapMax.x, diffuseRemapMax.y, diffuseRemapMax.z);
                diffuseTint = EditorGUI.ColorField(rect, styles.colorTint, diffuseTint, true, false, false);
                diffuseRemapMax.x = diffuseTint.r;
                diffuseRemapMax.y = diffuseTint.g;
                diffuseRemapMax.z = diffuseTint.b;
                diffuseRemapMin.x = diffuseRemapMin.y = diffuseRemapMin.z = 0;

                // enableDensity = diffuseRemapMin.w > 0;

                // For now, disable the enableDensity toggle, until we fully understand the feature and can test against expectation.
                // if (!heightBlend)
                // {
                //     rect.y = rect.yMax + 2;
                //     enableDensity = EditorGUI.Toggle(rect, styles.opacityAsDensity, diffuseRemapMin.w > 0);
                // }

                if (materialEditor != null)
                {
                    Material material = materialEditor.target as Material;
                    DrawTextureFilterModeErrorMessagesForTexture(material, materialEditor, terrainLayer.diffuseTexture, "Terrain Layer Diffuse Texture");
                }
                
                --EditorGUI.indentLevel;
            }
            diffuseRemapMax.w = 1;
            diffuseRemapMin.w = enableDensity ? 1 : 0;

            if (EditorGUI.EndChangeCheck())
            {
                terrainLayer.diffuseRemapMin = diffuseRemapMin;
                terrainLayer.diffuseRemapMax = diffuseRemapMax;
            }

            EditorGUILayout.Space();
            TerrainLayerUtility.TilingSettingsUI(terrainLayer);

            return true;
        }
    }
}
