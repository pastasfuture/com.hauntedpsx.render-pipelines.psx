using UnityEditor.ShaderGraph;

namespace HauntedPSX.RenderPipelines.PSX.ShaderGraph
{
    static class PSXStructs
    {
        public static StructDescriptor Varyings = new StructDescriptor()
        {
            name = "Varyings",
            packFields = true,
            fields = new FieldDescriptor[]
            {
                StructFields.Varyings.positionCS,
                StructFields.Varyings.positionWS,
                StructFields.Varyings.normalWS,
                StructFields.Varyings.tangentWS,
                StructFields.Varyings.texCoord0,
                StructFields.Varyings.texCoord1,
                StructFields.Varyings.texCoord2,
                StructFields.Varyings.texCoord3,
                StructFields.Varyings.color,
                StructFields.Varyings.viewDirectionWS,
                StructFields.Varyings.screenPosition,
                PSXStructFields.Varyings.lightmapUV,
                PSXStructFields.Varyings.sh,
                PSXStructFields.Varyings.fogFactorAndVertexLight,
                PSXStructFields.Varyings.shadowCoord,
                StructFields.Varyings.instanceID,
                PSXStructFields.Varyings.stereoTargetEyeIndexAsBlendIdx0,
                PSXStructFields.Varyings.stereoTargetEyeIndexAsRTArrayIdx,
                StructFields.Varyings.cullFace,
            }
        };
    }
}
