# 2D Rigging Testing

## Sorting layer capture workflow
The project now includes a renderer feature that isolates specific sorting layer ranges into render textures for UI-driven effects.
Follow the steps below when extending or adjusting the capture pipeline.

### Renderer setup
- `Assets/Settings/Renderer2D.asset`
  - `Use Camera Sorting Layers Texture` is enabled so the feature can access sprite data per layer.
  - `SortingLayerCaptureFeature` is registered as the last renderer feature (order matters because it must run after the transparent pass to sample fully composited sprites).
- `Assets/Settings/UniversalRP.asset` references the 2D renderer so the capture feature is active in play mode.

### Capture configuration asset
- `Assets/Settings/SortingLayerCaptureConfig.asset` lists the capture effects.
  - `Layer Range` selects which sorting layer index (inclusive) to render.
  - `Target Texture` points to the destination render texture (e.g., `OutlineCapture`, `GlowCapture`).
  - `Material Slot Notes` records which UI/material consumes the texture so designers can keep track of references.
- Designers can duplicate an existing entry to add new captures—ensure the range corresponds to a dedicated sorting layer.

### UI preview and materials
- `Canvas > RawImage_Outline` uses `Materials/Outline.mat` and samples `OutlineCapture.renderTexture`.
- `Canvas > RawImage_Glow` uses `Materials/GlowOverlay.mat` and samples `GlowCapture.renderTexture`.
- Each RawImage shows the isolated render texture directly in the inspector so material tweaks update in real time.

### Sorting layer conventions
- New layers live under **Project Settings ▸ Tags and Layers**: `Capture_Characters` and `Capture_Props`.
- Use the `Capture_` prefix for any layer that should be blitted into a render texture—this keeps capture layers grouped in the inspector.
- Assign characters or VFX sprites that need isolated treatments to `Capture_Characters` and prop/background silhouettes to `Capture_Props` so the capture config can reference them cleanly.
