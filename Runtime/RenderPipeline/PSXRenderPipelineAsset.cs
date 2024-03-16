using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    public partial class PSXRenderPipelineAsset : RenderPipelineAsset
    {
        #if UNITY_2022_3_OR_NEWER
        // Shader stripping changed in Core RP 14.0.8 which used by Unity 2022 LTS and later
        public override string renderPipelineShaderTag => PSXStringConstants.s_GlobalRenderPipelineStr;
        #endif

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
                return _defaultShader;
            }
        }

        public override Material defaultMaterial
        {
            get { return renderPipelineResources?.materials.defaultOpaqueMat; }
        }

        public override Shader terrainDetailLitShader
        {
            get { return renderPipelineResources?.shaders.terrainDetailLitPS; }
        }

        public override Shader terrainDetailGrassShader
        {
            get { return renderPipelineResources?.shaders.terrainDetailGrassPS; }
        }

        public override Shader terrainDetailGrassBillboardShader
        {
            get { return renderPipelineResources?.shaders.terrainDetailGrassBillboardPS; }
        }

        public override Shader defaultSpeedTree7Shader
        {
            get { return null; } // TODO
        }

        public override Shader defaultSpeedTree8Shader
        {
            get { return null; } // TODO
        }
    #endif

        [SerializeField]
        public PSXRenderPipelineResources renderPipelineResources;

        // TODO: Currently the SRP Batcher is forced off due to a D3D11 swap chain crash in HPSXRP.
        // Expose this option to users once the SRP Batcher stabilizes / once the engine is fixed.
        //
        // [SerializeField]
        // public bool isSRPBatcherEnabled = false;
    }
}
