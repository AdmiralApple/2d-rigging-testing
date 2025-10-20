using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rendering
{
    /// <summary>
    /// Scriptable object that lists the sorting layer capture entries so designers can maintain
    /// multiple effect outputs without code changes.
    /// </summary>
    [CreateAssetMenu(fileName = "SortingLayerCaptureConfig", menuName = "Rendering/Sorting Layer Capture Config")]
    public sealed class SortingLayerCaptureConfig : ScriptableObject
    {
        [System.Serializable]
        public sealed class CaptureEffect
        {
            [SerializeField]
            [Tooltip("Inclusive sorting layer range copied from the camera's sorting layer texture.")]
            private SortingLayerRange sortingLayerRange = SortingLayerRange.all;

            [SerializeField]
            [Tooltip("Render texture that receives the copy of the selected sorting layer range.")]
            private RenderTexture targetTexture = null;

            [SerializeField]
            [Tooltip("Notes for the material or UI element that will sample this texture in the scene.")]
            private string materialNotes = string.Empty;

            /// <summary>
            /// Inclusive sorting layer range copied from the camera's sorting layer texture.
            /// </summary>
            public SortingLayerRange SortingLayerRange => sortingLayerRange;

            /// <summary>
            /// Render texture that receives the copy of the selected sorting layer range.
            /// </summary>
            public RenderTexture TargetTexture => targetTexture;

            /// <summary>
            /// Notes for the material or UI element that will sample this texture in the scene.
            /// </summary>
            public string MaterialNotes => materialNotes;
        }

        [SerializeField]
        [Tooltip("Ordered list of capture effects that will be processed after transparents.")]
        private List<CaptureEffect> effects = new List<CaptureEffect>();

        /// <summary>
        /// Ordered list of capture effects that will be processed after transparents.
        /// </summary>
        public IReadOnlyList<CaptureEffect> Effects => effects;
    }
}
