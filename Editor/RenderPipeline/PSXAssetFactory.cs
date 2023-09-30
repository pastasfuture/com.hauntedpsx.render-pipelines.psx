using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using HauntedPSX.RenderPipelines.PSX.Runtime;
using UnityEditor;

namespace HauntedPSX.RenderPipelines.PSX.Editor
{
    [ExecuteInEditMode]
    static class PSXRenderPipelineAssetFactory
    {
    	static readonly string s_PackagePath = "Packages/com.hauntedpsx.render-pipelines.psx/";

#if UNITY_2021_2_OR_NEWER
        [MenuItem("HauntedPS1/Create HauntedPS1 Render Pipeline Asset", priority = CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
#else
        [MenuItem("HauntedPS1/Create HauntedPS1 Render Pipeline Asset", priority = CoreUtils.assetCreateMenuPriority1)]
#endif
        static void CreatePSXRenderPipelineAsset()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetPSXRenderPipelineAsset>(), "PSXRenderPipelineAsset.asset", icon, null);
        }

        class DoCreateNewAssetPSXRenderPipelineAsset : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<PSXRenderPipelineAsset>();
                newAsset.name = Path.GetFileName(pathName);

                ResourceReloader.ReloadAllNullIn(newAsset, s_PackagePath);

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

#if UNITY_2021_2_OR_NEWER
        [MenuItem("HauntedPS1/Create HauntedPS1 Render Pipeline Resources", priority = CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
#else
        [MenuItem("HauntedPS1/Create HauntedPS1 Render Pipeline Resources", priority = CoreUtils.assetCreateMenuPriority1)]
#endif
        static void CreatePSXRenderPipelineResources()
        {
            var icon = EditorGUIUtility.FindTexture("ScriptableObject Icon");
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreateNewAssetPSXRenderPipelineResources>(), "PSXRenderPipelineResources.asset", icon, null);
        }

        class DoCreateNewAssetPSXRenderPipelineResources : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var newAsset = CreateInstance<PSXRenderPipelineResources>();
                newAsset.name = Path.GetFileName(pathName);

                ResourceReloader.ReloadAllNullIn(newAsset, s_PackagePath);

                AssetDatabase.CreateAsset(newAsset, pathName);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }
    }
}
