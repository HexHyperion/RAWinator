# RAWinator
A (relatively) simple ~and lightweight~ RAW file processing software with some additional functions, written in .NET using Magick.NET, as a project for AiTP lessons.

## Installation
To open the project for development, you need to clone it to your machine either using
```
git clone https://github.com/HexHyperion/RAWinator.git
```
or with the built-in Git integrations in your editor, and then open the **solution** (not the folder) with Visual Studio, Rider or other .NET IDE.

To try out the app, build and run the project with what's usually a big, green "play" button - all dependencies should get installed automatically and after a while the window should appear.

Maybe someday I'll compile it into an .exe for mortals, but for now I fail to create a file that will want to run on my 2 Windows devices, let alone _all_ Windows devices...

## Functionality
Honestly, I feared that the app won't be much and absolutely won't be suitable for "real" editing, and while obviously it's no competition for any serious processing software, it turned out quite nice, and can give really high quality effects, if you have ~patience~ a remotely good computer obviously :)

I took inspiration mainly from Lightroom Classic, as that's what I personally use and find most intuitive and well-thought. Probably you could notice some hints of XnView in here too.

At first I tried to use a C# port of LibRaw, but I was forced to switch to Magick.NET as the library turned out to be very poorly documented and not intended for this type of processing. Even though I use Magick for lower-level operations like rendering or pixel operations, I calculate the more advanced adjustments (like highlights/shadows or color-specific HSL) with my own logic, so don't think it's just a UI wrapper for the library ;)

### Library tab
This section is used for importing, managing and exporting the RAW files - RAWinator supports all common camera RAW formats from Nikon, Canon, Fujifilm and others. The photos' metadata and low-res thumbnails are stored for quick access, and the full-size images are computed on-demand to save memory.

The app shows the JPEG preview and essential metadata of the currently selected image, extracted from EXIF profiles. The "Export" button allows to save the selected images as JPEGs with all the edits.

There is a progress bar at the bottom, which shows the progress of the import/export operations, which are asynchronous, multi-thread and independent from the UI. Speaking of the UI, almost all dark-bordered panels across the app can be drag-resized.

Pressing the `Del` key removes the currently selected pictures from library, and double-clicking an image opens it in Develop mode.

### View tab
Next section on the tab bar functions as a quick image viewer, which can be used for culling, comparing and selecting the pictures to edit. It also allows to view all metadata directories of the image.

The pictures seen in there are the full-sized JPEG previews, which are always embedded in RAWs by almost every digital camera (which is the reason why JPEG+RAW mode is useless), as extracting those is much less costly than rendering the actual raw data, allowing fast browsing. This is common among practically all image viewers for photographers.

On the metadata panel, there is an "All metadata" expander, which shows a formatted list of all metadata tags grouped by directories upon click. The common metadata block from Library is displayed at the top, as users rarely need anything above that.

The `Del` key and double-click on the horizontal list work just like in Library.

### Develop tab
Moving to the most important section of the app, the currently selected image's raw pixel data is loaded to memory and cached until replaced with another image. This section is used for applying various detailed adjustments to the image, cropping it and viewing it close-up with the zoom function.

The image in the center is the actual rendered RAW file, on which the applied adjustments will be displayed in (almost) real-time. It can be zoomed either with buttons below or with the scroll wheel, and panned by dragging it with the mouse.

The right panel holds all tone, color and detail-related adjustment sliders, as well as the special effects' controls. All adjustments' functions are explained in the table below.

Dragging the sliders and switching other options triggers recomputation of the image, which, depending on PC performance and the specific adjustment, can take from about 0.5s to several seconds. The progress bar below indicates when the image data is being recalculated, and in such state all sliders are disabled.

On the left panel is a list of presets, which are named, saved states of adjustments, allowing to re-apply the same settings for many pictures. New presets can be added with the "Add preset" button, which opens a dialog prompting for the name of the new preset. Double-clicking on a preset applies it, and the `Del` key removes the selected presets.

RAWinator saves the edit history after each adjustment, allowing "Undo" and "Redo" actions with `Ctrl+Z` and `Ctrl+Shift+Z`/`Ctrl+Y`. Applying presets and cropping can be un- and re-done this way, too.

Buttons below the preset list are used for applying automatic adjustments like general Enhance or Denoise, and for toggling the crop (transformation) mode.

In the crop mode, an image without edits, crop or zoom is shown, and it can be rotated or flipped along the X or Y axis with the buttons on the left panel. Dragging across the image creates a rectangular selection, which will be the area the image will be cropped to on crop mode exit. The aspect ratio of the crop area can be set in the boxes above the buttons. After coming back to normal mode, adjustments made before will be reapplied on the image.

The rotate and flip transformations are applied directly to the cached image, as those are reversible and usually applied only once. They are also not saved in presets or undo stack.

The edits done in this tab are saved and bound to the image object until it's deleted from the app, so switching back to a previously edited photo will bring back all changes done to it. Exported images will contain all edits as well.

## Develop adjustments
### Basic adjustments
| Name | Explanation |
|------|-------------|
| Exposure | Adjusts the brightness of the picture in an exponential manner, simulating in-camera exposure |
| Brightness | Adjusts the brightness of the image linearly |
| Contrast | Makes the difference between the dark and light tones less or more pronounced |
| White Balance (Temp) | Adjusts the bias of the image towards cold or warm tones |
| White Balance (Tint) | Adjusts the bias of the image towards magenta or green tones |
| Highlights | Adjusts the brightness of bright areas of the image |
| Shadows | Adjusts the brightness of dark areas of the image |
| Saturation | Adjusts the saturation of the whole image |
| Hue | Shifts each color on the image by a certain angle on the color wheel |

### Detail adjustments
| Name | Explanation |
|------|-------------|
| Sharpness | Increases or decreases contrast on the edges of colors to visually "sharpen" the image | 
| Vignette | Brightens or darkens the corners of the image in an elliptical manner |
| Noise | Introduces artificial Gaussian noise to simulate high ISO |

### Adjust by color
| Name | Explanation |
|------|-------------|
| Hue per color | Shifts the color of pixels in a specific color range by a certain amount |
| Saturation p.c. | Adjusts the saturation of pixels in a specific color range |
| Luminance p.c. | Adjusts the brightness of pixels in a specific color range |

### Special effects
| Name | Explanation |
|------|-------------|
| Border color | Sets the color of the image border, accepts 3-, 6- and 8- digit hex codes |
| Border size | Sets the width of the image border, accepts positive values in pixels |
| Grayscale | Turns the image black and white |
| Sepia | Applies a yellowish sepia-like filter|
| Solarize | Inverts the highlights similarly to photographic film left in the sun |
| Invert | Inverts all color values of the image |
| Oilpaint | Makes the image look like oil painting |
| Charcoal | Makes the image look like drawn with charcoal |
| Posterize | Reduces the gamut of the picture to make it look like a comic/poster |
| Sketch | Makes the image look like a sketch picture |

## Shortcuts and hidden actions
| Key combination | Function | Where? |
|-------------|----------|----------|
| Double-click | Open the image in Develop | Library/View image list |
| Arrow keys | Move between images | Library/View image list |
| `Del` | Remove image(s) from library | Library/View image list |
| Double-click | Apply the preset | Develop |
| `Del` | Remove the selected preset(s) | Develop |
| Scroll wheel | Zoom the image in/out | Develop |
| Mouse drag | Pan the image / draw crop area | Develop |
| `Ctrl+Z` | Undo the last action | Develop |
| `Ctrl+Y` or `Ctrl+Shift+Z` | Redo the previous action | Develop |
| `Ctrl+O` | Import new pictures | Everywhere |
| `Ctrl+E` | Export current/selected picture(s) | Everywhere |
| `Ctrl+I` | Open the "About" window | Everywhere |

## Dependencies
- [Magick.NET-Q16](https://github.com/dlemstra/Magick.NET) for RAW file import and processing (higher quality, 16 bits/channel)
- [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet) for metadata extraction
