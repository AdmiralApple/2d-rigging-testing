# 2D Rigging Testing

## Sorting layer capture workflow
- The **Renderer2D** asset now enables the camera sorting layer texture and registers the new `SortingLayerCaptureFeature`. Feature order is important because the capture must complete before any other renderer feature can reuse the textures for UI previews.
- Use the shared [`SortingLayerCaptureConfig`](Assets/Settings/SortingLayerCaptureConfig.asset) to author capture entries without code changes. Each entry defines:
  - The sorting layer range to grab.
  - The destination `RenderTexture` that will be written by the feature.
  - A designer note that calls out which material slot or UI element consumes the texture.
- Two render textures ship by default:
  - `OutlineSubject` holds content from the **OutlineSubject** sorting layer and feeds the outline material / RawImage preview.
  - `BackdropMask` mirrors the **OutlineBackdrop** layer so UI can mask or debug the captured background slice.

## Scene and material bindings
- `Canvas/RawImage` continues to display the outline preview but now reads from `RenderTextures/OutlineSubject` via the existing Outline material.
- `Canvas/BackdropRawImage` is a secondary UI preview that targets `RenderTextures/BackdropMask` so designers can confirm the captured backdrop mask.
- The outline material (`Materials/Outline.mat`) references the same `OutlineSubject` render texture to keep shader and UI previews in sync.

## Sorting layer conventions
- **OutlineSubject** — assign sprites that need the stylised outline or similar capture driven effects (e.g., the character prefab).
- **OutlineBackdrop** — reserve for background sprites that participate in mask/dilate style captures. The coloured ground quads use this layer.
- **OutlineUI** — spare layer for future UI specific captures; keep unused unless a feature explicitly calls for it.
- Maintain this naming and assignment scheme so the renderer feature config stays predictable.
