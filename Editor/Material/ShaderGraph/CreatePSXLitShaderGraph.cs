using System;
using UnityEditor;
using UnityEditor.ShaderGraph;

namespace HauntedPSX.RenderPipelines.PSX.ShaderGraph
{
    static class CreatePSXLitShaderGraph
    {
        [MenuItem("Assets/Create/Shader/HauntedPS1/PSXLit Shader Graph", false, 208)]
        public static void CreateHDUnlitGraph()
        {
            var target = (PSXTarget)Activator.CreateInstance(typeof(PSXTarget));
            target.TrySetActiveSubTarget(typeof(PSXLitSubTarget));

            var blockDescriptors = new[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
            };

            GraphUtil.CreateNewGraphWithOutputs(new[] {target}, blockDescriptors);
        }
    }
}
