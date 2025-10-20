using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    /// <summary>
    /// Scriptable renderer feature that exposes the camera sorting layer texture and copies the
    /// configured ranges into dedicated render targets for UI driven effects.
    /// </summary>
    public class SortingLayerCaptureFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        private sealed class SortingLayerCapturePass : ScriptableRenderPass
        {
            private static readonly int SortingRangeProperty = Shader.PropertyToID("_SortingLayerCaptureRange");

            private readonly ProfilingSampler profilingSampler = new ProfilingSampler("SortingLayerCapture");

            private SortingLayerCaptureConfig captureConfig;
            private RTHandle cameraSortingLayerTexture;
            private Material blitMaterial;

            /// <summary>
            /// Executes a blit from the camera sorting layer texture into every configured target.
            /// </summary>
            /// <param name="context">Render context used for submitting the blits.</param>
            /// <param name="renderingData">Frame rendering data (unused, but required by the signature).</param>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (captureConfig == null || cameraSortingLayerTexture == null)
                {
                    return;
                }

                IReadOnlyList<SortingLayerCaptureConfig.CaptureEffect> effects = captureConfig.Effects;
                if (effects == null || effects.Count == 0)
                {
                    return;
                }

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    foreach (SortingLayerCaptureConfig.CaptureEffect effect in effects)
                    {
                        if (effect == null || effect.TargetTexture == null)
                        {
                            continue;
                        }

                        SortingLayerRange range = effect.SortingLayerRange;
                        cmd.SetGlobalVector(SortingRangeProperty, new Vector4(range.lowerBound, range.upperBound, 0f, 0f));

                        RenderTargetIdentifier destination = new RenderTargetIdentifier(effect.TargetTexture);

                        if (blitMaterial != null)
                        {
                            Blitter.BlitTexture(cmd, cameraSortingLayerTexture, destination, blitMaterial, 0);
                        }
                        else
                        {
                            Blitter.BlitCameraTexture(cmd, cameraSortingLayerTexture, destination);
                        }
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// <summary>
            /// Filters the render pass to the supplied config and texture, restricting the copy to
            /// the selected sorting layer range for each entry.
            /// </summary>
            /// <param name="config">Capture description provided by designers.</param>
            /// <param name="sortingLayerTexture">Camera texture that contains sorting layer results.</param>
            /// <param name="materialOverride">Optional material that processes the copy.</param>
            public void Setup(SortingLayerCaptureConfig config, RTHandle sortingLayerTexture, Material materialOverride)
            {
                captureConfig = config;
                cameraSortingLayerTexture = sortingLayerTexture;
                blitMaterial = materialOverride;
            }
        }

        [SerializeField]
        [Tooltip("Configuration asset that defines which sorting layer ranges are copied and where they are stored.")]
        private SortingLayerCaptureConfig captureConfig;

        [SerializeField]
        [Tooltip("Optional blit material used when writing into the render textures (leave empty for a raw copy).")]
        private Material blitMaterial;

        private SortingLayerCapturePass capturePass;

        /// <summary>
        /// Designer facing configuration that exposes the capture entries to the renderer.
        /// </summary>
        public SortingLayerCaptureConfig CaptureConfig => captureConfig;

        /// <summary>
        /// Allocates the pass that will read the camera sorting layer texture and perform the copies.
        /// </summary>
        public override void Create()
        {
            capturePass = new SortingLayerCapturePass
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        /// <summary>
        /// Enqueues the capture pass so it can mirror the configured sorting layer ranges.
        /// </summary>
        /// <param name="renderer">Renderer owning the sorting layer texture.</param>
        /// <param name="renderingData">Information about the current frame.</param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (captureConfig == null)
            {
                return;
            }

            capturePass.Setup(captureConfig, renderer.cameraSortingLayerTexture, blitMaterial);
            renderer.EnqueuePass(capturePass);
        }
    }
}
