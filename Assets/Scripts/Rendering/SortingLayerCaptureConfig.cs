using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SortingLayerCapture
{
    /// <summary>
    /// Scriptable configuration that stores sorting layer capture definitions referenced by the renderer feature.
    /// </summary>
    [CreateAssetMenu(menuName = "Rendering/Sorting Layer Capture Config", fileName = "SortingLayerCaptureConfig")]
    public class SortingLayerCaptureConfig : ScriptableObject
    {
        [System.Serializable]
        public class CaptureEntry
        {
            /// <summary>
            /// Defines which sorting layers should be drawn into the capture, ordered inclusive of the upper bound.
            /// </summary>
            [Tooltip("Inclusive range of sorting layers that should populate this capture.")]
            public SortingLayerRange sortingLayerRange = SortingLayerRange.all;

            /// <summary>
            /// Destination texture that receives the isolated color data for this capture.
            /// </summary>
            [Tooltip("RenderTexture that will be populated by the capture pass each frame.")]
            public RenderTexture targetTexture;

            /// <summary>
            /// Optional blit material override used when writing into the target texture.
            /// </summary>
            [Tooltip("Optional material for the blit; leave empty to use the feature's fallback material.")]
            public Material blitMaterialOverride;

            /// <summary>
            /// Free-form note that tells content authors how materials should consume this capture.
            /// </summary>
            [Tooltip("Design note describing which material slot samples the resulting texture.")]
            [TextArea]
            public string materialSlotNote;
        }

        /// <summary>
        /// Editable list of capture entries that the renderer feature will execute.
        /// </summary>
        [SerializeField]
        [Tooltip("Each element describes a capture: sorting layer span, output texture, and usage notes.")]
        private List<CaptureEntry> entries = new();

        /// <summary>
        /// Provides read-only access to the capture entries for runtime systems.
        /// </summary>
        public IReadOnlyList<CaptureEntry> Entries => entries;
    }
}
