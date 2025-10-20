using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    /// <summary>
    /// Renderer feature that renders specific sorting layer ranges into designer supplied render textures.
    /// The feature looks at the shared configuration asset to know which captures to perform each frame.
    /// </summary>
    public class SortingLayerCaptureFeature : ScriptableRendererFeature
    {
        /// <summary>
        /// Shared data asset that specifies which sorting layers should be captured and where the results live.
        /// </summary>
        [Tooltip("List of capture definitions that map sorting layer ranges to render textures.")]
        [SerializeField]
        private SortingLayerCaptureConfig _config;

        private readonly List<SortingLayerCapturePass> _passes = new();

        /// <inheritdoc />
        public override void Create()
        {
            _passes.Clear();
            if (_config == null || _config.Effects == null)
            {
                return;
            }

            EnsurePassListSize(_config.Effects.Count);
        }

        /// <inheritdoc />
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_config == null || _config.Effects == null || _config.Effects.Count == 0)
            {
                return;
            }

            EnsurePassListSize(_config.Effects.Count);

            for (var i = 0; i < _config.Effects.Count; i++)
            {
                var effect = _config.Effects[i];
                var pass = _passes[i];
                if (!pass.ConfigureForEffect(effect))
                {
                    continue;
                }

                renderer.EnqueuePass(pass);
            }
        }

        private void EnsurePassListSize(int requiredCount)
        {
            while (_passes.Count < requiredCount)
            {
                _passes.Add(new SortingLayerCapturePass());
            }

            // Remove extra passes if the configuration shrinks so we do not keep stale references alive.
            if (_passes.Count > requiredCount)
            {
                _passes.RemoveRange(requiredCount, _passes.Count - requiredCount);
            }
        }

        /// <summary>
        /// Render pass that copies draw calls restricted by the configured sorting layer range into a render texture.
        /// </summary>
        private sealed class SortingLayerCapturePass : ScriptableRenderPass
        {
            private static readonly ShaderTagId[] ShaderTagIds =
            {
                new("UniversalForward"),
                new("UniversalForwardOnly"),
                new("SRPDefaultUnlit")
            };

            private readonly ProfilingSampler _profilingSampler = new("SortingLayerCapture");

            /// <summary>
            /// Filtering settings used to restrict draw calls to the requested sorting layer range.
            /// </summary>
            private FilteringSettings _filteringSettings;
            private SortingLayerCaptureConfig.EffectEntry _effect;
            private RenderTargetIdentifier _targetIdentifier;

            public SortingLayerCapturePass()
            {
                // We render after the main transparent pass so sprites are fully composed when we re-draw them.
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            }

            /// <summary>
            /// Updates the pass state for the provided configuration entry.
            /// Returns <c>false</c> when the entry is incomplete and the pass should be skipped for the frame.
            /// </summary>
            public bool ConfigureForEffect(SortingLayerCaptureConfig.EffectEntry effect)
            {
                _effect = effect;
                if (_effect == null || _effect.TargetTexture == null)
                {
                    return false;
                }

                _filteringSettings = new FilteringSettings(RenderQueueRange.all)
                {
                    sortingLayerRange = _effect.LayerRange
                };
                _targetIdentifier = new RenderTargetIdentifier(_effect.TargetTexture);
                return true;
            }

            /// <inheritdoc />
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (_effect?.TargetTexture == null)
                {
                    return;
                }

                var cameraData = renderingData.cameraData;
                var cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, _profilingSampler))
                {
                    var descriptor = new RenderTextureDescriptor(
                        _effect.TargetTexture.width,
                        _effect.TargetTexture.height,
                        _effect.TargetTexture.graphicsFormat,
                        depthBufferBits: 0)
                    {
                        msaaSamples = 1,
                        mipCount = 1
                    };

                    // Clear the destination so only the current sorting layer contribution remains visible.
                    cmd.SetRenderTarget(_targetIdentifier, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    cmd.ClearRenderTarget(true, true, Color.clear);

                    // Ensure the draw calls use the same camera matrices as the base 2D renderer pass.
                    cmd.SetViewProjectionMatrices(cameraData.GetViewMatrix(), cameraData.GetProjectionMatrix());
                    cmd.SetViewport(new Rect(0, 0, descriptor.width, descriptor.height));
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortingSettings = new SortingSettings(cameraData.camera)
                {
                    criteria = SortingCriteria.CommonTransparent
                };
                var drawingSettings = CreateDrawingSettings(ShaderTagIds, ref renderingData, sortingSettings.criteria);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);

                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
        }
    }

    /// <summary>
    /// ScriptableObject container that lists all the sorting layer captures the renderer feature should perform.
    /// </summary>
    [CreateAssetMenu(menuName = "Rendering/Sorting Layer Capture Config")]
    public sealed class SortingLayerCaptureConfig : ScriptableObject
    {
        [Tooltip("Collection of effect entries that map sorting layers to render textures for UI sampling.")]
        [SerializeField]
        private List<EffectEntry> _effects = new();

        /// <summary>
        /// Immutable view of the configured capture definitions.
        /// </summary>
        public IReadOnlyList<EffectEntry> Effects => _effects;

        /// <summary>
        /// Describes a single capture, including the sorting layer range, the destination texture, and usage notes.
        /// </summary>
        [Serializable]
        public sealed class EffectEntry
        {
            [Tooltip("Sorting layer range that should be drawn into the capture texture (inclusive).")]
            [SerializeField]
            private SortingLayerRange _layerRange = SortingLayerRange.all;

            [Tooltip("RenderTexture that receives the blit so materials can sample the isolated content.")]
            [SerializeField]
            private RenderTexture _targetTexture;

            [Tooltip("Designer facing note about which UI/material slot consumes this capture.")]
            [SerializeField]
            private string _materialSlotNotes = "";

            /// <summary>
            /// Inclusive sorting layer range that is drawn into the render texture.
            /// </summary>
            public SortingLayerRange LayerRange => _layerRange;

            /// <summary>
            /// RenderTexture that stores the filtered output.
            /// </summary>
            public RenderTexture TargetTexture => _targetTexture;

            /// <summary>
            /// Optional note that explains where the capture is consumed (UI element, material slot, etc.).
            /// </summary>
            public string MaterialSlotNotes => _materialSlotNotes;
        }
    }
}
