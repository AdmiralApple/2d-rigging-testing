A place for playing around with albertferras https://github.com/albertferras/unity-2d-rigging-shader

## Sorting layer capture workflow

The new **SortingLayerCaptureFeature** copies camera sorting layer ranges into designer-authored
render textures so UI and materials can sample isolated sprite groups.

### Renderer configuration

* `Assets/Settings/Renderer2D.asset`
  * Enable **Use Camera Sorting Layer Texture**.
  * Append the `SortingLayerCaptureFeature.asset` renderer feature (keep it after other 2D
    features so it blits the final transparent stack).
* `Assets/Settings/SortingLayerCaptureConfig.asset`
  * Add one element per effect with a `SortingLayerRange`, the target `RenderTexture`, and any
    notes for the material that consumes the capture.

```text
Element 0 (Characters)
  Sorting Layer Range : Capture_Characters → Capture_Characters
  Target Texture      : SortingLayerCharacters.renderTexture
  Material Notes      : Characters outline RawImage
Element 1 (VFX)
  Sorting Layer Range : Capture_VFX → Capture_VFX
  Target Texture      : SortingLayerVFX.renderTexture
  Material Notes      : VFX distortion panel
```

### UI hookups

* `Assets/Scenes/OutlineRigScene.unity`
  * `Canvas/RawImage` → Material `Outline.mat`, Texture `SortingLayerCharacters.renderTexture`.
  * `Canvas/RawImage_VFX` → Material `OutlineVFX.mat`, Texture `SortingLayerVFX.renderTexture`.

### Sorting layer conventions

Dedicated capture layers live under **Project Settings ▸ Tags and Layers** with a `Capture_`
prefix (for example `Capture_Characters`, `Capture_VFX`). Assign sprites that require isolated
captures to these layers so their ranges line up with the config entries.