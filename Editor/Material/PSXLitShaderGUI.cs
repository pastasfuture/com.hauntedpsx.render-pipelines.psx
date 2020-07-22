using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    internal class PSXLitShaderGUI : BaseShaderGUI
    {
        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
        }

        // material changed check
        public override void MaterialChanged(Material material)
        {
            if (material == null) { throw new ArgumentNullException("material"); }

            PSXMaterialUtils.SetMaterialKeywords(material);
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            base.DrawSurfaceOptions(material);
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            DrawEmissionProperties(material);
            DrawReflectionProperties(material);
            PSXMaterialUtils.DrawTileOffset(materialEditor, mainTexProp);
        }

        // material main advanced options
        public override void DrawAdvancedOptions(Material material)
        {
            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                PSXMaterialUtils.SetupMaterialBlendMode(material);
                return;
            }

            PSXMaterialUtils.SurfaceType surfaceType = PSXMaterialUtils.SurfaceType.Opaque;
            PSXMaterialUtils.BlendMode blendMode = PSXMaterialUtils.BlendMode.AlphaPostmultiply;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = PSXMaterialUtils.SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = PSXMaterialUtils.SurfaceType.Transparent;
                blendMode = PSXMaterialUtils.BlendMode.AlphaPostmultiply;
            }
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);

            MaterialChanged(material);
        }
    }
}
