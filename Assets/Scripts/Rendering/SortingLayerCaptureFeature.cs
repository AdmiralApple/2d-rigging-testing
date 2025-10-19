using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SortingLayerCapture
{
    /// <summary>
    /// Renderer feature that spins up per-layer capture passes and blits the filtered results into designer-specified targets.
    /// </summary>
    public class SortingLayerCaptureFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// Shared material used whenever an entry does not override the blit shader.
        /// </summary>
        [SerializeField]
        [Tooltip("Default material applied while copying the capture into its render texture.")]
        private Material fallbackBlitMaterial;

        /// <summary>
        /// Asset that lists every capture definition to be executed.
        /// </summary>
        [SerializeField]
        [Tooltip("ScriptableObject providing sorting layer ranges, target textures, and notes.")]
        private SortingLayerCaptureConfig configuration;

        /// <summary>
        /// When during the frame the capture passes should execute.
        /// </summary>
        [SerializeField]
        [Tooltip("Render pass event for the capture blits; after transparents keeps UI intact.")]
        private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        private readonly List<SortingLayerCapturePass> _capturePasses = new();

        /// <inheritdoc />
        public override void Create()
        {
            BuildPassCache();
        }

        /// <inheritdoc />
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (configuration == null || configuration.Entries == null || configuration.Entries.Count == 0)
            {
                return;
            }

            BuildPassCache();

            for (var index = 0; index < configuration.Entries.Count; index++)
            {
                var entry = configuration.Entries[index];
                if (entry == null || entry.targetTexture == null)
                {
                    continue;
                }

                if (index >= _capturePasses.Count)
                {
                    break;
                }

                var pass = _capturePasses[index];
                pass.Setup(entry, fallbackBlitMaterial);
                pass.renderPassEvent = renderPassEvent;
                renderer.EnqueuePass(pass);
            }
        }

        private void BuildPassCache()
        {
            var desiredCount = configuration != null && configuration.Entries != null ? configuration.Entries.Count : 0;
            if (_capturePasses.Count == desiredCount)
            {
                return;
            }

            while (_capturePasses.Count < desiredCount)
            {
                _capturePasses.Add(new SortingLayerCapturePass());
            }

            while (_capturePasses.Count > desiredCount)
            {
                _capturePasses.RemoveAt(_capturePasses.Count - 1);
            }
        }

        /// <summary>
        /// Internal render pass that draws only the requested sorting layer span into a temporary target and blits it out.
        /// </summary>
        private class SortingLayerCapturePass : ScriptableRenderPass
        {
            /// <summary>
            /// Shader pass names rendered during the capture so sprites, unlit, and custom 2D shaders participate.
            /// </summary>
            private static readonly ShaderTagId[] ShaderTagIds =
            {
                new("Universal2D"),
                new("UniversalForward"),
                new("UniversalForwardOnly"),
                new("SRPDefaultUnlit")
            };

            /// <summary>
            /// Filters draw calls to the transparent queue so sorting layer comparisons remain authoritative.
            /// </summary>
            private readonly FilteringSettings _filteringSettings = new(RenderQueueRange.transparent);
            private readonly ProfilingSampler _profilingSampler = new("SortingLayerCapturePass");
            private readonly int _temporaryColorTargetId = Shader.PropertyToID("_SortingLayerCaptureTemp");

            private SortingLayerCaptureConfig.CaptureEntry _entry;
            private Material _blitMaterial;
            private RenderTextureDescriptor _descriptor;

            /// <summary>
            /// Stores the data for the incoming capture entry and selects the correct blit material.
            /// </summary>
            /// <param name="entry">Capture definition with sorting range and output texture.</param>
            /// <param name="fallbackMaterial">Material used when the entry does not provide an override.</param>
            public void Setup(SortingLayerCaptureConfig.CaptureEntry entry, Material fallbackMaterial)
            {
                _entry = entry;
                _blitMaterial = entry.blitMaterialOverride != null ? entry.blitMaterialOverride : fallbackMaterial;
            }

            /// <inheritdoc />
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                if (_entry == null || _entry.targetTexture == null)
                {
                    return;
                }

                _descriptor = renderingData.cameraData.cameraTargetDescriptor;
                _descriptor.msaaSamples = 1;
                _descriptor.depthBufferBits = 0;
                cmd.GetTemporaryRT(_temporaryColorTargetId, _descriptor, FilterMode.Bilinear);
            }

            /// <inheritdoc />
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_entry == null || _entry.targetTexture == null)
                {
                    return;
                }

                var cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, _profilingSampler))
                {
                    var tempTarget = new RenderTargetIdentifier(_temporaryColorTargetId);
                    cmd.SetRenderTarget(tempTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    cmd.ClearRenderTarget(true, true, Color.clear);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    var sortingSettings = new SortingSettings(renderingData.cameraData.camera)
                    {
                        criteria = SortingCriteria.CommonTransparent,
                        sortingLayerRange = _entry.sortingLayerRange
                    };

                    var drawingSettings = CreateDrawingSettings(ShaderTagIds[0], ref renderingData, sortingSettings.criteria);
                    drawingSettings.sortingSettings = sortingSettings;
                    for (var i = 1; i < ShaderTagIds.Length; i++)
                    {
                        drawingSettings.SetShaderPassName(i, ShaderTagIds[i]);
                    }

                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);

                    var targetIdentifier = new RenderTargetIdentifier(_entry.targetTexture);
                    if (_blitMaterial != null)
                    {
                        cmd.Blit(tempTarget, targetIdentifier, _blitMaterial);
                    }
                    else
                    {
                        cmd.Blit(tempTarget, targetIdentifier);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// <inheritdoc />
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (_entry == null || _entry.targetTexture == null)
                {
                    return;
                }

                cmd.ReleaseTemporaryRT(_temporaryColorTargetId);
            }
        }
    }
}
