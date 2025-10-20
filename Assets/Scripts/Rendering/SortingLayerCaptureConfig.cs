using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SortingLayerCaptures
{
    /// <summary>
    /// ScriptableObject that lists every sorting layer capture effect so designers can tweak
    /// ranges, targets, and notes without touching code.
    /// </summary>
    [CreateAssetMenu(menuName = "Rendering/Sorting Layer Capture Config", fileName = "SortingLayerCaptureConfig")]
    public sealed class SortingLayerCaptureConfig : ScriptableObject
    {
        [Serializable]
        public class EffectEntry
        {
            /// <summary>
            /// Sorting layer range that will be copied from the camera sorting layer texture.
            /// </summary>
            [SerializeField]
            private SortingLayerRange sortingLayerRange = SortingLayerRange.all; // Range of layers to capture.

            /// <summary>
            /// Target texture that receives the camera sorting layer blit.
            /// </summary>
            [SerializeField]
            private RenderTexture targetTexture = null; // RenderTexture written by the capture pass.

            /// <summary>
            /// Optional note that indicates which material slot or UI element expects this texture.
            /// </summary>
            [SerializeField]
            private string materialSlotNote = ""; // Designer-facing hint describing usage of the texture.

            public SortingLayerRange SortingLayerRange => sortingLayerRange;
            public RenderTexture TargetTexture => targetTexture;
            public string MaterialSlotNote => materialSlotNote;

            /// <summary>
            /// Retrieves the minimum and maximum sorting layer values for easy GPU consumption.
            /// </summary>
            public Vector2 GetSortingLayerValues()
            {
                var lower = SortingLayer.GetLayerValueFromID(sortingLayerRange.lowerBound);
                var upper = SortingLayer.GetLayerValueFromID(sortingLayerRange.upperBound);
                return new Vector2(lower, upper);
            }
        }

        [SerializeField]
        private List<EffectEntry> effects = new(); // Ordered list of capture instructions executed every frame.

        /// <summary>
        /// Provides read-only access to the configured capture effects.
        /// </summary>
        public IReadOnlyList<EffectEntry> Effects => effects;
    }
}
