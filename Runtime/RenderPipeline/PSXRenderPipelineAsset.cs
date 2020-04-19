using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    public partial class PSXRenderPipelineAsset : RenderPipelineAsset
    {
        PSXRenderPipelineAsset()
        {
        }

        protected override UnityEngine.Rendering.RenderPipeline CreatePipeline()
        {
            PSXRenderPipeline pipeline = null;

            try
            {
                pipeline = new PSXRenderPipeline(this);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            }

            return pipeline;
        }

        protected override void OnValidate()
        {
            //Do not reconstruct the pipeline if we modify other assets.
            //OnValidate is called once at first selection of the asset.
            if (GraphicsSettings.renderPipelineAsset == this)
                base.OnValidate();
        }

    #if UNITY_EDITOR
        private Shader _defaultShader = null;
        public override Shader defaultShader
        {
            get
            {
                if (_defaultShader != null) { return _defaultShader; }
                _defaultShader = Shader.Find("PSX/PSXLit");
                Debug.Assert(_defaultShader, "Error: PSXRenderPipelineAsset: Failed to find default shader at path: PSX/PSXLit");
                return _defaultShader;
            }
        }

        public override Material defaultMaterial
        {
            get { return renderPipelineResources?.materials.defaultOpaqueMat; }
        }
    #endif

        [SerializeField]
        public PSXRenderPipelineResources renderPipelineResources;

        [SerializeField]
        public int targetRasterizationResolutionWidth = 320;
        public int targetRasterizationResolutionHeight = 240;

        [SerializeField]
        public bool isFrameLimitEnabled = true;
        [SerializeField]
        public int frameLimit = 24;
        [SerializeField]
        public bool isSRPBatcherEnabled = false;
    }

#if UNITY_EDITOR
    [ExecuteInEditMode]
    public static class PSXRenderPipelineAssetFactory
    {
        static readonly string s_DefaultPath = "Assets/PSXRenderPipelineAsset.asset";

        [UnityEditor.MenuItem("HauntedPS1/Create HauntedPS1 Render Pipeline Asset")]
        public static void CreatePSXRenderPipelineAsset()
        {
            var newAsset = ScriptableObject.CreateInstance<PSXRenderPipelineAsset>();
            ResourceReloader.ReloadAllNullIn(newAsset, PSXStringConstants.s_PackagePath);
            UnityEditor.AssetDatabase.CreateAsset(newAsset, s_DefaultPath);
        }
    }
#endif
}
