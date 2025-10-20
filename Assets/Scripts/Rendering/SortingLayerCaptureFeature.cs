using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SortingLayerCaptures
{
    /// <summary>
    /// Renderer feature that schedules a capture pass for every configured sorting layer range,
    /// allowing camera sorting layer textures to be written into designer-supplied render textures.
    /// </summary>
    public sealed class SortingLayerCaptureFeature : ScriptableRendererFeature
    {
        private const string k_PassName = "Sorting Layer Capture";
        private static readonly int s_CameraSortingLayerTextureId = Shader.PropertyToID("_CameraSortingLayerTexture");
        private static readonly int s_SortingLayerRangeProperty = Shader.PropertyToID("_SortingLayerCaptureRange");

        /// <summary>
        /// Asset that holds the capture entries executed by this feature.
        /// </summary>
        [SerializeField]
        private SortingLayerCaptureConfig configuration = null;

        /// <summary>
        /// Hook point for the capture blit within the renderer schedule.
        /// </summary>
        [SerializeField]
        private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        /// <summary>
        /// When enabled, every destination texture is cleared before copying the sorting layer data.
        /// </summary>
        [SerializeField]
        private bool clearTargetsBeforeBlit = true;

        private SortingLayerCapturePass capturePass;

        /// <summary>
        /// Pass that blits the selected camera sorting layer texture slices into their render targets.
        /// </summary>
        private sealed class SortingLayerCapturePass : ScriptableRenderPass
        {
            private readonly List<SortingLayerCaptureConfig.EffectEntry> entries = new();
            private readonly bool clearBeforeBlit;
            private readonly ProfilingSampler profilingSampler;

            public SortingLayerCapturePass(bool clearBeforeBlit)
            {
                this.clearBeforeBlit = clearBeforeBlit;
                profilingSampler = new ProfilingSampler(k_PassName);
                ConfigureInput(ScriptableRenderPassInput.Color);
            }

            public void Configure(IReadOnlyList<SortingLayerCaptureConfig.EffectEntry> configuredEntries, RenderPassEvent passEvent)
            {
                renderPassEvent = passEvent;

                entries.Clear();
                if (configuredEntries == null)
                {
                    return;
                }

                foreach (var entry in configuredEntries)
                {
                    if (entry == null || entry.TargetTexture == null)
                    {
                        continue;
                    }

                    entries.Add(entry);
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (entries.Count == 0 || renderingData.renderer is not Renderer2D)
                {
                    return;
                }

                var cmd = CommandBufferPool.Get(k_PassName);
                using (new ProfilingScope(cmd, profilingSampler))
                {
                    var source = new RenderTargetIdentifier(s_CameraSortingLayerTextureId);
                    foreach (var entry in entries)
                    {
                        var sortingValues = entry.GetSortingLayerValues();
                        // Communicate the selected sorting layer window so dependent shaders can react if needed.
                        cmd.SetGlobalVector(s_SortingLayerRangeProperty, new Vector4(sortingValues.x, sortingValues.y, 0f, 0f));

                        var destination = new RenderTargetIdentifier(entry.TargetTexture);
                        if (clearBeforeBlit)
                        {
                            cmd.SetRenderTarget(destination);
                            cmd.ClearRenderTarget(false, true, Color.clear);
                        }

                        cmd.Blit(source, destination);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        /// <inheritdoc/>
        public override void Create()
        {
            capturePass = new SortingLayerCapturePass(clearTargetsBeforeBlit);
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (configuration == null || configuration.Effects == null || configuration.Effects.Count == 0)
            {
                return;
            }

            capturePass.Configure(configuration.Effects, renderPassEvent);
            renderer.EnqueuePass(capturePass);
        }
    }
}
