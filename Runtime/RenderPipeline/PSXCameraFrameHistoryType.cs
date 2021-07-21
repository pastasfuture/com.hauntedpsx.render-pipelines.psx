namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    /// <summary>
    /// History buffers available in HDCamera.
    /// </summary>
    public enum PSXCameraFrameHistoryType
    {
        Rasterization = 0,
        RasterizationDepthStencil,
        RasterizationPreUICopy
    }
}
