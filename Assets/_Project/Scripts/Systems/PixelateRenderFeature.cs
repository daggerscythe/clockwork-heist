using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// draws the game at a low resolution and scales it back up with hard square pixels
public class PixelateRenderFeature : ScriptableRendererFeature
{
    [SerializeField, Min(1)] private int pixelHeight = 270; // px tall the game is drawn at

    private PixelatePass pass;

    // makes the pass once when the renderer loads
    public override void Create()
    {
        pass = new PixelatePass();
        pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        // the camera has to draw into a texture that can be read and not straight to the screen
        pass.requiresIntermediateTexture = true;
    }

    // hands the pass to the renderer every frame
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        pass.pixelHeight = pixelHeight;
        renderer.EnqueuePass(pass);
    }

    // shrinks the camera image then stretches it back up
    private class PixelatePass : ScriptableRenderPass
    {
        public int pixelHeight;

        // tells the render graph which textures to make and which copies to do
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resources = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // only the game camera so the scene view stays sharp for editing
            if (cameraData.cameraType != CameraType.Game || resources.isActiveTargetBackBuffer) return;

            // full size texture to hold the final blocky image
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;
            TextureHandle full = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_PixelateFull", false);

            // whole number scale so every big pixel is the same size on screen
            int scale = Mathf.Max(1, desc.height / pixelHeight);
            desc.width = Mathf.Max(1, desc.width / scale);
            desc.height = Mathf.Max(1, desc.height / scale);
            TextureHandle low = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_PixelateLow", false);

            // nearest filtering both ways keeps the pixel edges hard
            renderGraph.AddBlitPass(resources.activeColorTexture, low, Vector2.one, Vector2.zero, filterMode: RenderGraphUtils.BlitFilterMode.ClampNearest, passName: "Pixelate Down");
            renderGraph.AddBlitPass(low, full, Vector2.one, Vector2.zero, filterMode: RenderGraphUtils.BlitFilterMode.ClampNearest, passName: "Pixelate Up");

            // everything after this point draws on top of the blocky image
            resources.cameraColor = full;
        }
    }
}