---------------------------------------------------------------------------------------------------------------------------
Bugfix UV Animation Mode: **Flipbook**
---------------------------------------------------------------------------------------------------------------------------
Fixed flipbook uv calculation bugs that caused unintentional jerky animation and frame drop outs. Special thanks to Fever Dream Johnny who noticed the bug and provided the first iteration of the bugfix.

---------------------------------------------------------------------------------------------------------------------------
Bugfix Material Inspector: **UV Animation Mode**
---------------------------------------------------------------------------------------------------------------------------
Fixed bug where changing UV Animation Mode.Pan Sin.UV Animation Oscillation Frequency would erroneously zero out UV Animation Scale.

---------------------------------------------------------------------------------------------------------------------------
New Fog Volume Feature: **Height Falloff Mirrored**
---------------------------------------------------------------------------------------------------------------------------
**Height Falloff Mirrored**: If enabled, height fog will be mirrored about Height Max, creating fog that fades in below and above. Especially useful when combined with ColorLUTMode.Texture2DDistanceAndHeight, which can be used to give the mirrored fog a different color.
**Height Falloff Mirrored Secondary**: If enabled, height fog secondary layer will be mirrored about Height Max, creating fog that fades in below and above. Especially useful when combined with ColorLUTMode.Texture2DDistanceAndHeight, which can be used to give the mirrored fog a different color.

---------------------------------------------------------------------------------------------------------------------------
New Fog Volume Feature: **Blend Mode**
---------------------------------------------------------------------------------------------------------------------------
**Blend Mode**: Selects the function used to blend Fog with the underlying geometry color.
	- **Over**: Blends fog with the underlying color using the Over operator. This is the same behavior that existed before and results in the most physically plausible fog.
	- **Additive**: Blends fog with with the underlying color using the Additive operator.
	- **Subtractive**: Blends fog with the underlying color using the Subtractive operator.
	- **Multiply**: Blends fog with the underlying color using the Multiply operator.

---------------------------------------------------------------------------------------------------------------------------
New Fog Volume Feature: **Color LUT Texture**
---------------------------------------------------------------------------------------------------------------------------
**Color LUT Mode**: Controls whether or not a Color Look-Up-Texture is used, and the format used.
	- Disabled: No Color LUT is used.
	- Texture 2D Distance and Height: Uses a Texture2D where the horizontal axis stores the color along distance, and the vertical axis stores the color along height.
	- TextureCube: Uses a TextureCube that is sampled using view direction (direction from camera to surface).
**Color LUT Texture**:
	- When Color LUT Mode is set to Texture 2D Distance and Height: Specifies the texture used as the Color Look-Up-Texture. Expects a Texture2D where the horizontal axis stores the color along distance, and the vertical axis stores the color along height.
	- When Color LUT Mode is set to Texture Cube: Specifies the texture used as the Color Look-Up-Texture. Expects a TextureCube that is sampled using view direction (direction from camera to surface).
**Color LUT Weight**: Specifies the amount of influence the Color LUT Texture has on the final fog color. A value of 1.0 is full influence, meaning Color LUT Texture is multiplied against the Fog Color. A value of 0.0 is no influence, meaning the final color is simply the Fog Color.
**Color LUT Weight Secondary**: Specifies the amount of influence the Color LUT Texture has on the Secondary Fog Layer.

---------------------------------------------------------------------------------------------------------------------------
New Material Feature: **Shading Evaluation Mode: Per-Object**
---------------------------------------------------------------------------------------------------------------------------
**Shading Evaluation Mode: Per-Object**: Evaluates lighting and fog at the object origin, rather than per-vertex or per-pixel. This is useful for replicating lighting and fog artifacts that would occur when per-vertex or per-pixel lighting could not be afforded. Note, this has approximately the same performance cost as per-vertex, it is not optimization. This is due to the fact that in our render pipeline (in the context of unity SRP), it is more convinient and likely more efficient to still perform lighting in the vertex shader, rather than running say a CPU-side job to calculate lighting per-object. 

---------------------------------------------------------------------------------------------------------------------------
Workflow Improvement: **Precision Volume->Geometry Pushback Disabled by Default**
---------------------------------------------------------------------------------------------------------------------------
**Geometry Pushback**: Disable Geometry Pushback by default. Artifacts from this technique can be suprising / unexpected - it makes more sense to opt into it rather than opting out. This also improves prefab workflows as it avoids geometry pushback in that context.

---------------------------------------------------------------------------------------------------------------------------
New Camera Volume Feature: **Aspect Ratio Mode: Native**
---------------------------------------------------------------------------------------------------------------------------
**Native**: Rasterizes at native camera resolution and aspect ratio. No scaling is performed. Useful with secondary cameras used for rendering UI to an offscreen render target. Allows the UI and camera to fully drive the resolution, rather than needing to duplicate the resolution values inside the volume and the camera and the UI.

---------------------------------------------------------------------------------------------------------------------------
New Camera Volume Feature: **Aspect Ratio Mode: Locked Stretch**
---------------------------------------------------------------------------------------------------------------------------
**Locked Stretch**: Brings back legacy pixel-imperfect locked aspect ratio mode. This mode will enforce the rasterization resolution defined and perform naive upscaling to native resolution. Can result in pixel dropouts / doubling. Useful when you want to enforce a specific aspect ratio, but you need to align your rasterization with non-HPSXRP UI.

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **VSync**
---------------------------------------------------------------------------------------------------------------------------
- Turn ON Vsync when Frame Limit is disabled. Previously, vsync would be forced off when Frame Limit was disabled, which was not the desired behavior.

---------------------------------------------------------------------------------------------------------------------------
New Material Feature: **BRDF Modes**
---------------------------------------------------------------------------------------------------------------------------
**Lambert**: Standard, cheap diffuse-only response. This is the same behavior that was built in before.
**Wrapped Lighting**: Wrapped Lighting: Same as Lambert, but lighting wraps to zero at 180 degrees (backface normal) instead of 90 degrees. Useful for approximating subsurface scattering, or for creating smoother lighting without needing to rely on baked data. Wrapped Lighting is energy conserving so the reflection facing the light source will be dimmer when compared to lambert (the energy is redistributed over the sphere).

---------------------------------------------------------------------------------------------------------------------------
New Material Feature: **New Vertex Color Modes**
---------------------------------------------------------------------------------------------------------------------------
**Alpha-Only**: Multiplies only the alpha channel of the vertex color against the alpha of MainTex.
**Emission**: Multiplies the vertex color with the Emission value.
**Emission And Alpha-Only**: Multiplies the vertex color with the Emission value and multiplies the vertex color alpha channel with the MainTex alpha.

---------------------------------------------------------------------------------------------------------------------------
New Material Feature: **UV Animation Mode: Flipbook**
---------------------------------------------------------------------------------------------------------------------------
Adds support for animated flipbook textures.
**UV Animation Flipbook Tiles X**: Specifies the number of horizontal tiles in the flipbook.
**UV Animation Flipbook Tiles Y**: Specifies the number of vertical tiles in the flipbook.
**UV Animation Flipbook Frequency**: Specifies the frames per second of the flipbook animation.

---------------------------------------------------------------------------------------------------------------------------
New Material Feature: **UV Animation Mode**
---------------------------------------------------------------------------------------------------------------------------
Allows you to apply simple procedural animation to your surfaces uvs.
**Pan Linear**: Useful for simple water scrolling animations, for example, a waterfall.
**Pan Sin**: Applies a Sin(x) scrolling animation. Useful for rocking back and forth animations.
**UV Animation Frame Limit**: Specifies whether or not to apply a frame limit to the uv animations. Useful for simulated lower frequency animation updates, i.e: retro 15 FPS animations.
**UV Animation Frames Per Second**: Controls the frames per second that the uv animation is updated at. This is a purely visual control, it does not affect performance.
**UV Animation Scroll Velocity**: Controls the distance per second (in uv space) that the uvs will pan.
**UV Animation Oscillation Frequency**: Controls the frequency that the uvs will oscillate back and forth.
**UV Animation Scale**: Controls the maximum distance (in uv space) that the uvs will pan (at the oscillation peak).

---------------------------------------------------------------------------------------------------------------------------
New Material Feature: **Precision Geometry Override Mode**
---------------------------------------------------------------------------------------------------------------------------
Allows you to override the precision geometry parameter that is specified on your Precision Volume. Useful glitch effects, or for completely disabling vertex snapping on a specific material. Note: **Precision Geometry Weight** has been replaced.
**None**: No override, use value specified on the PrecisionVolume. Default behavior.
**Disabled**: Disable precision geometry effect on this material.
**Override**: Use value specified on the material, rather than the value specified on the PrecisionVolume.
**Add**: Add the value specified on the material to the value specified on the PrecisionVolume.
**Multiply**: Multiply the value specified on the material with the value specified on the PrecisionVolume.

---------------------------------------------------------------------------------------------------------------------------
New Material Feature: **Precision Color Override Mode**
---------------------------------------------------------------------------------------------------------------------------
Allows you to override the precision color parameter that is specified on your Precision Volume. Useful for stylistic effects such as crushing the bit-depth of specific materials. Can also be used to reduce / remove banding + dither on specific materials.
**None**: No override, use value specified on the PrecisionVolume. Default behavior.
**Disabled**: Disable precision geometry effect on this material.
**Override**: Use value specified on the material, rather than the value specified on the PrecisionVolume.
**Add**: Add the value specified on the material to the value specified on the PrecisionVolume.
**Multiply**: Multiply the value specified on the material with the value specified on the PrecisionVolume.

---------------------------------------------------------------------------------------------------------------------------
New PrecisionVolume Feature: **Precision Geometry Enabled**
---------------------------------------------------------------------------------------------------------------------------
Can now globally toggle off precision geometry (vertex snapping) effects.

---------------------------------------------------------------------------------------------------------------------------
New PrecisionVolume Feature: **Geometry Pushback**
---------------------------------------------------------------------------------------------------------------------------
**Geometry Pushback Enabled**: Controls whether or not geometry close to the camera is artificially pushed back. This emulates a PSX-era technique used to reduce affine texture warping artifacts. Can be useful for glitch effects.
**Geometry Pushback Min / Max**: Controls the distance range from the camera that geometry is artifically pushed back. The Min value specifies the distance geometry will start to be pushed back. The Max value specifies the distance geometry will be pushed back to.

---------------------------------------------------------------------------------------------------------------------------
New CameraVolume Feature: **Aspect Ratio Mode: Revised**
---------------------------------------------------------------------------------------------------------------------------
Controls how the aspect ratio of the camera is handled. Previously, only **Free** and **Locked** modes existed. Modes have now been expanded to add more options around how upscaling occurs. The main goal being stylistic choices around how to ensure pixel-perfect upscaling, to avoid pixel drop out and moire patterns.
**Free Stretch**: Naive upscale from rasterization resolution to screen resolution. Results in pixel drop-out and moire interference patterns when screen resolution is not an even multiple of rasterization resolution. Not reccomended.
**Free Fit Pixel Perfect**: Upscale rasterization resolution to screen resolution by the max round multiple of rasterization resolution that can be contained within the screen. Perfectly preserves all pixels and dither patterns. Maintains aspect ratio. Results in black border when screen resolution is not an even multiple of rasterization resolution.
**Free Crop Pixel Perfect**: Upscale rasterization resolution to screen resolution by the max round multiple of rasterization resolution that can completely fill the screen. Perfectly preserves all pixels and dither patterns. Maintains aspect ratio. Results in zoomed in image / change of aspect ration when screen resolution is not an even multiple of rasterization resolution.
**Free Bleed Pixel Perfect**: Same as Free Fit Pixel Perfect, but the camera field of view is automatically expanded to fill areas that would otherwise require black borders.
**Locked Fit Pixel Perfect**: Same as Free Fit Pixel Perfect, but enforces the aspect ratio defined by the rasterization resolution X and Y parameters. Useful for forcing a retro 4:3 aspect ratio on any screen.
