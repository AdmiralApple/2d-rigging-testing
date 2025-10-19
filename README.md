# 2D Rigging Testing

This sandbox now includes a URP 2D renderer feature that captures specific sorting layer bands into dedicated render textures for UI-driven previews.

## Sorting layer capture overview
- The **SortingLayerCaptureFeature** renderer feature is added to `Assets/Settings/Renderer2D.asset` and executes after transparents so the capture receives lit sprite data before UI draws.
- Captures are described in the `Assets/Settings/SortingLayerCaptureConfig.asset` ScriptableObject; the renderer feature iterates that asset at runtime, creating one pass per entry.
- Each entry exposes a sorting layer range, a target render texture, an optional blit material, and a material usage note so designers can document how downstream shaders consume the capture.

## Authoring capture entries
1. Open `Assets/Settings/SortingLayerCaptureConfig.asset`.
2. For each desired effect, set the **Sorting Layer Range** to the inclusive layer span you want to isolate (e.g., `Capture_Subject`).
3. Assign a **Target Texture** (RenderTexture asset) that matches the consuming material's expectations.
4. Optionally assign a **Blit Material Override** if the default copy is insufficient; otherwise the feature will use a direct blit.
5. Fill out **Material Slot Note** with clear instructions (e.g., "Outline material uses this as _MainTex"), which is mirrored in documentation for quick reference.

## Scene and inspector wiring
- The `Canvas` now contains three `RawImage_*` children so each effect preview samples its render texture:
  - `RawImage_Backdrop` → `CaptureBackdrop.renderTexture`
  - `RawImage_Subject` → `CaptureSubjects.renderTexture`
  - `RawImage_Overlay` → `CaptureOverlay.renderTexture`
- All RawImages share the Outline material so UI authors only need to swap textures when previewing additional captures.

## Sorting layer conventions
- New sorting layers live in **Project Settings → Tags and Layers**:
  - `Capture_Backdrop` (background geometry)
  - `Capture_Subject` (primary characters or rigs needing outlines)
  - `Capture_Overlay` (VFX, highlights, and additive accents)
- Keep layer names in the `Capture_*` format so the configuration asset stays readable and consistent.
- Place sprites on the layer that matches the capture they require; the sample scene assigns the square background to `Capture_Backdrop`, the white sprite to `Capture_Subject`, and accent strips to `Capture_Overlay`.
- Add new layers sparingly—when a capture is required—so the renderer feature executes only meaningful passes.
