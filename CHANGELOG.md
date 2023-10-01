---------------------------------------------------------------------------------------------------------------------------
New Unity Version Support: **2022.2**
---------------------------------------------------------------------------------------------------------------------------
Unity 2022.2 is now the newest version that the Haunted PSX Render Pipeline now supports. Previously it only supported up to 2021-LTS.

---------------------------------------------------------------------------------------------------------------------------
Bugfix 2022: Volume Editors **VolumeComponentEditor attribute deprecated**
---------------------------------------------------------------------------------------------------------------------------
VolumeComponentEditor attribute was deprecated in 2022. Simply use the CustomEditor attribute instead, which exists in all versions of unity that the haunted psx render pipeline supports.

---------------------------------------------------------------------------------------------------------------------------
Bugfix 2022: Shader Functions **GetViewToWorldMatrix Declaration**
---------------------------------------------------------------------------------------------------------------------------
In Core versions less than 14.0 (2021 and older), GetViewToWorldMatrix and TransformViewToWorld are not defined yet in SpaceTransforms.hlsl.
But in 14.0, 2022 and up, they are.
Currently we have no way of detecting the version of core and static branching in our shaders.
Instead, we simply define our own PSX variants that mimic the functions in newer versions of SpaceTransforms.hlsl, and use those everywhere so that we always have compatability.

---------------------------------------------------------------------------------------------------------------------------
New Unity Version Support: **2021-LTS**
---------------------------------------------------------------------------------------------------------------------------
Unity 2021 LTS is now the newest version that the Haunted PSX Render Pipeline now supports. Previously it only supported up to 2020-LTS.

---------------------------------------------------------------------------------------------------------------------------
Bugfix Fog Volume: **Color LUT Mode: Texture Cube**
---------------------------------------------------------------------------------------------------------------------------
Many platforms, such as WebGL 2.0 do not support performing texture cube samples or loads in a vertex shader. This caused shaders in Color Lut Mode: Texture Cube to not compile in WebGL 2.0.
Rather than splitting behavior on platforms, instead, we go for consistency, and now force fog to be evaluated per-pixel, if Color LUT Mode is set to Texture Cube.

---------------------------------------------------------------------------------------------------------------------------
Bugfix PSXLit: **MetaPass Uninitialized Vertex Color in Split Color and Lighting Mode**
---------------------------------------------------------------------------------------------------------------------------
Fix legitimate shader warning where the vertex color varying was not initialized when in vertex color mode split color and lighting.

---------------------------------------------------------------------------------------------------------------------------
Bugfix PSXLit: **Enable GPU Instancing**
---------------------------------------------------------------------------------------------------------------------------
Enable GPU Instancing on PSXLit materials no works as expected. Multiple instanced draws may occur in a single instanced draw call without flickering in and out, as it did before.

---------------------------------------------------------------------------------------------------------------------------
Bugfix UV Animation Mode: **Flipbook**
---------------------------------------------------------------------------------------------------------------------------
Fixed flipbook uv calculation bugs that caused unintentional drop of final flipbook frame. Also fixed bug where texture filtering (if enabled) could bleed across flipbook page boundaries. Thanks for reporting the bug Visuwyg!


---------------------------------------------------------------------------------------------------------------------------
New Volume Feature: **Terrain Grass**
---------------------------------------------------------------------------------------------------------------------------
The Terrain Grass Volume allows the Haunted PSX Render Pipeline to expose additional custom properties for the Terrain Grass shaders.
In the future, this Terrain Grass Volume will likely expose more HPSXRP material features that were previously unmodifiable on the Terrain Grass shaders.
Special thanks to joshuaskelly for the first pass at the implementation of terrain grass filtering.
**Texture Filter Mode**: Controls how the Terrain Grass textures are filtered.
TextureFilterMode.TextureImportSettings is the standard unity behavior. Textures will be filtered using the texture's import settings.
TextureFilterMode.Point will force PSX era nearest neighbor point sampling, regardless of texture import settings.
TextureFilterMode.PointMipmaps is the same as TextureFilterMode.Point but supports supports point sampled lods via the texture's mipmap chain.
TextureFilterMode.N64 will force N64 era 3-point barycentric texture filtering.
TextureFilterMode.N64MipMaps is the same as TextureFilterMode.N64 but supports N64 sampled lods via the texture's mipmap chain.

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **Compression compute shader compile on all supported platforms**
---------------------------------------------------------------------------------------------------------------------------
Compression.compute erroneously was flagged to only compile on dx11. Now flagged to compile on all platforms that support it.


---------------------------------------------------------------------------------------------------------------------------
Bugfix: **Legacy Canvas UI no longer drawing**
---------------------------------------------------------------------------------------------------------------------------
Legacy Canvas UI broken from the **Canvas order is not considered for legacy Canvas UI** commit. Both are now fixed.

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **Fix Precision issues with UV Animation Pan Linear and Pan Sin Modes**
---------------------------------------------------------------------------------------------------------------------------

---------------------------------------------------------------------------------------------------------------------------
new Material Feature: **Reflection Direction Mode**
---------------------------------------------------------------------------------------------------------------------------
Controls the direction reflections are sampled from.
**Reflection** is the standard, physically-based (for fully smooth materials) approach.
**Normal** simply uses the surface normal as the reflection sample direction. Useful for emulating old school "MatCap" materials.
**View** uses the direction from the camera to the surface as the reflection sample direction. Useful for rendering portals.

---------------------------------------------------------------------------------------------------------------------------
Feature: **Dynamic Lights respect Camera Culling Mask**
---------------------------------------------------------------------------------------------------------------------------
If a dynamic light's layer is not included in a camera's culling mask, that light will be correctly ignored / hidden.

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **Errors when using any other color LUT modes in the fog volume**
---------------------------------------------------------------------------------------------------------------------------
Fixed Per-Vertex Shading variant of Color LUT modes to correctly sample LOD 0 in the vertex shader (where gradients do not exist).
Also fixed a few misc shader warnings.

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **Do not compile Compression.compute on platforms that do not support it**
---------------------------------------------------------------------------------------------------------------------------

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **Grass is unaffected by realtime lights**
---------------------------------------------------------------------------------------------------------------------------
Terrain-based grass details did not receive light due to undefined _BRDF_MODE_LAMBERT keyword - which is now statically defined for grass. Thanks for the tip Mika Notarnicola @thebeardphantom !

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **Lightmap baking errors when using vertex colors and per-pixel lighting**
---------------------------------------------------------------------------------------------------------------------------
Fixed bug where meta pass failed to compile with PSXLit variant where vertex colors were enabled, and per-pixel lighting was in use.

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **Canvas order is not considered for legacy Canvas UI**
---------------------------------------------------------------------------------------------------------------------------
When rendering legacy canvas UI geometry, the sorting criteria was not set, so transparent UI was not sorted correctly. Additionally, the current cameras layer mask was not taken into consideration, so canvas UI geometry that was requested to be excluded from rendering from specific cameras was still rendered. Thanks for the tip Mika Notarnicola @thebeardphantom !

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **Failed to present D3D11 swapchain due to device reset/removed.**
---------------------------------------------------------------------------------------------------------------------------
Fixed bug where editor crashed for some users when editor was setup with multiple viewports (i.e: scene view, game view, material preview).
The bug turned out to be a bug within the SRP Batcher, triggered (but not directly caused) by some UnityPerMaterial layout changes in PSXLitInputs.hlsl.
The SRP Batcher has been manually disabled in HPSXRP until the Unity engine bug is tracked down / resolved.

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **CRT Shader Scanline Size and Vignette**
---------------------------------------------------------------------------------------------------------------------------
Fixed bug where scanline size and vignette was incorrect when scaled render targets were encountered.

---------------------------------------------------------------------------------------------------------------------------
Bugfix: **CRT Shader Vertical Flip Logic**
---------------------------------------------------------------------------------------------------------------------------
Fixed bug where some CameraVolume aspect modes did not render correctly on hardware that requires flipped Y in the CRT shader.

---------------------------------------------------------------------------------------------------------------------------
New Material Feature: **Vertex Color Blend Modes**
---------------------------------------------------------------------------------------------------------------------------
Controls how vertex colors are blended with MainColor|MainTex.
**Multiply** The standard vertex color blend mode, the vertexColor.rgba channels are multiplied against the MainColor.rgba channels.
**Additive** adds the vertexColor.rgb * vertexColor.a result to the MainColor.rgb channels. vertexColor.a is multiplied against MainColor.a to support alpha fade out.
**Subtractive** subtracts the vertexColor.rgb * vertexColor.a result from the MainColor.rgb channels. vertexColor.a is multiplied against MainColor.a to support alpha fade out.

---------------------------------------------------------------------------------------------------------------------------
New Material Feature: **Vertex Color Mode: Split Color And Lighting**
---------------------------------------------------------------------------------------------------------------------------
**Split Color And Lighting**: A hybrid of VertexColorMode.Color and VertexColorMode.Lighting. Vertex colors >= 0.5 are rescaled and treated as VertexColorMode.Lighting. Vertex colors < 0.5 are rescaled and treated as VertexColorMode.Color. This mode is experimental. Looking for feedback from users. Expect potential modifications to the underlying implementation in future releases.

---------------------------------------------------------------------------------------------------------------------------
New Precision Volume Feature: **Dither Size**
---------------------------------------------------------------------------------------------------------------------------
**Dither Size**: Controls the size (in rasterization resolution pixels) of screen space dither.
A value of 1 results in standard, 1:1 mapping between rasterization resolution pixels and dither pattern pixels.
Values > 1 result a dither pattern that covers multiple rasterization resolution pixels.
Values > 1 are useful for aesthetic purposes, particularly with higher rasterization resolutions, where you want the dither pattern to be more noticable / clear.

---------------------------------------------------------------------------------------------------------------------------
Bugfix Render Target + Viewport Scaling Issue
---------------------------------------------------------------------------------------------------------------------------
Fix up multiple bugs introduced by new RTHandleSystem use. The RTHandleSystem will only allocate new Render Textures if the requested size is greater then the previously max allocated size.
On the first frame, all Render Textures exactly match the rasterization resolution. However, if resolution changes occur, such as from window resizing, toggling of PSX Quality, or changes to target rasterization resolution, multiple bugs existed due to incorrect viewport setting and / or UV calculation.
Viewports are now correctly set, and all existing affected passes (blit, AccumulationMotionBlur, CRT) have been fixed up to correctly handle these cases.
For context, multiple users reported seeing this issue with incorrect dither size, incorrect geometry precision, and incorrect aspect ratios. These have all been fixed.

---------------------------------------------------------------------------------------------------------------------------
Bugfix Compression Volume
---------------------------------------------------------------------------------------------------------------------------
Compression volume rendering code was erroneously disabled in 1.4.0. Reenabled it.

---------------------------------------------------------------------------------------------------------------------------
New Lighting Feature: **Shadow Mask**
---------------------------------------------------------------------------------------------------------------------------
**Shadow Mask** baked shadows are now supported. Shadow Mask allows baking shadows for up to 4 lights per surface. Static surfaces recieve high quality Shadow Mask lightmap textures. These Shadow mask textures are sampled per-vertex if **Shading Evaluation Mode** is set to **Per Vertex**, or sampled per-pixel if **Shading Evaluation Mode** is set to **Per Pixel**. Dynamic objects will sample Shadow Mask values from the nearby Light Probe Group.

To use Shadow Mask shadows:
1) Under Window->Rendering->Lighting: Go to the **Mixed Lighting** section, and turn ON **Baked Global Illumination**. Also set **Lighting Mode** to **Shadow Mask**.
2) Make sure any Game Objects that you would like to receive Shadow Mask Lightmaps are set to **Static**. This should automatically set the following settings:
	- Mesh Renderer->Lighting->Receive Shadows: True
	- Mesh Renderer->Lighting->Contribute Global Illumination: True
	- Mesh Renderer->Lighting->Receive Global Illumination: Lightmaps
	- Make sure **Generate Lightmap UVs** is enabled in your model import settings
		- Make sure the **Min Lightmap Resolution** is correct for your Window->rendering->Lighting->Lightmap Resolution setting.
3) Make sure any dynamic Game Objects that you would like to receive Shadow Mask Probe data have the following settings:
	- Mesh Renderer->Lighting->Receive Shadows: True
	- Mesh Renderer->Lighting->Receive Global Illumination: Light Probes
4) If your scene contains dynamic objects: make sure you have a **Light Probe Group** in your scene. GameObject->Light->Light Probe Group
	- Make sure your scene has fairly good probe coverage. A probe every few meters is a reasonable choice for typically scaled games.
5) For any light that you would like to case Shadow Mask shadows:
	- Light->Mode: Mixed
	- Light->Shadow Type: Hard Shadows or Soft Shadows
	- Light->Realtime Shadows->Strength: This controls the amount lighting from this light will be darkened by Shadow Mask shadows. A value of 1.0 should be used for fully opaque shadows. Values < 1.0 can be used for stylistic purposes, but have no physical basis. Values < 1.0 are often used to cheaply approximate bounce light.
6) Under Window->Rendering->Lighting: Click **Generate Lighting** and wait for the bake to complete.

For more information on the Shadow Mask feature in unity, visit: https://docs.unity3d.com/Manual/LightMode-Mixed-Shadowmask.html

Note: If ANY light source in your scene requests Shadow Mask, HPSXRP will use Shadow Mask mode for ALL Mixed light sources. You cannot have some lights set to Mixed and some lights set to baked. Of NO light sources request Shadow Mask, HPSXRP will automatically configure itself to expect Lighting Mode->Baked Indirect data.

---------------------------------------------------------------------------------------------------------------------------
Bugfix Fog Volume: **Fog Color LUT Modes**
---------------------------------------------------------------------------------------------------------------------------
Fixed WebGL compatibility issue with Fog Color LUT Modes. WebGL doesn't support SAMPLE_TEXTURE2D_LOD or SAMPLE_CUBE_LOD - replaced with SAMPLE_TEXTURE2D and SAMPLE_CUBE. The LOD call was unnecessary.

---------------------------------------------------------------------------------------------------------------------------
New Volume Feature: **Accumulation Motion Blur Volume**
---------------------------------------------------------------------------------------------------------------------------
**Weight**: Controls the amount of motion blur. A value of 0.0 completely disables motion blur. A value of 1.0 is the maxium amount of motion blur. Rather than using per-pixel motion vectors to render motion blur in a physically plausible way as is done in a contemporary PBR render pipeline, motion blur in HPSXRP is implemented by simply blending the previous frame with the current frame. This accumulation-based motion blur was the common implementation in the PSX / N64 era. Lerping between the the current frame and the previous frame is called an Exponential Moving Average. An Exponential Moving Average creates a gaussian-shaped falloff over time. An Exponential Moving Average has a non-linear response to the Weight variable. In particular, values between [0.0, 0.5] have a fairly small effect, compared to values between [0.9, 0.95] which have a relatively strong effect.
**Vignette**: Controls the amount the effect fades out toward the center of the screen. A value of 0.0 creates uniform zoom across the entire screen, no fade out. A value of 1.0 removes zoom from the center of the screen. A value of -1.0 removes zoom from the edges of the screen.
**Dither**: Controls the amount of dither to apply to the weight when compositing the frame history with the current frame. The history is composited with an 8 bit per pixel alpha value. Dither is required to appoximately capture very low history weight pixels.
**Zoom**: Controls the amount of zoom applied to the history before it is blended. Values > 0.0 create a outward radial blur effect. Values < 0.0 create an inward pincushion blur effect.
**Zoom Dither**: Controls how much dither to apply to break up banding artifacts that occur when zooming more than 1 pixel. A value of 0.0 causes maximum banding, which is what is often seen in PSX / N64 zoom blur effects. A value of 1.0 removes all banding but introduces dither noise.
**Zoom Anisotropy**: Controls the directionality of the zoom. A value of 0.0 blurs uniformly in all directions. A value of 1.0 blurs only horizontally. A value of -1.0 blurs only vertically.
**Apply to UI Overlay**: When enabled, motion blur will be applied to UI Overlay geometry as well as background and main geometry. When disabled motion blur is only applied to background and main geometry. When disabled, an additional render target is allocated and blitted to in order to capture the pre-ui state of the rasterization render target.

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
