# KlakSpout

![gif](https://user-images.githubusercontent.com/343936/124232423-993f6c00-db4c-11eb-80d3-4c660a2025d9.gif)
![gif](https://user-images.githubusercontent.com/343936/124217164-c4b55d00-db32-11eb-88f1-735a04bfb235.gif)

**KlakSpout** is a Unity plugin that lets Unity send and receive video streams
through the [Spout] system.

[Spout]: http://spout.zeal.co/

## System Requirements

- Unity 2022.3 or later
- Windows system with Direct3D 11/12 support

KlakSpout currently supports only Direct3D 11 and 12; other graphics APIs such
as OpenGL or Vulkan aren't available.

## Pixel Format Compatibility

KlakSpout currently supports receiving the following pixel formats:

- R8G8B8A8 UNorm (sRGB/linear)
- B8G8R8A8 UNorm (sRGB/linear)
- R16G16B16A16 Half Float
- R32G32B32A32 Float

Most applications use R8G8B8A8 or B8G8R8A8, so you can receive frames without
extra steps. When using [TouchDesigner], choose the appropriate pixel format in
the Spout Out TOP.

[TouchDesigner]: https://derivative.ca/

For now, KlakSpout only supports sending the R8G8B8A8 UNorm format.

## How to Install

Install the KlakSpout package (`jp.keijiro.klak.spout`) from the "Keijiro"
scoped registry in Package Manager. Follow [these instructions] to add the
registry to your project.

[these instructions]:
  https://gist.github.com/keijiro/f8c7e8ff29bfe63d86b888901b82644c

## Spout Sender Component

![Sender](https://github.com/user-attachments/assets/ef1f7388-fe06-4054-9ddb-dd379f96dc61)

Use the **Spout Sender** component to send a video stream. It provides three
capture methods:

- **Game View**: Captures the content of the Game View.
- **Camera**: Captures a specified camera.
- **Texture**: Captures a 2D texture or a Render Texture.

The Camera capture method is available only on URP and HDRP—you can't use it on
the built-in render pipeline.

The **KeepAlpha** property controls whether the alpha channel is preserved or
cleared. Enable [alpha output] when using HDRP. On URP, select the Texture
capture method to output alpha.

[alpha output]:
  https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/manual/Alpha-Output.html

## Spout Receiver Component

![Receiver](https://github.com/user-attachments/assets/469c535a-2917-4dc8-9b04-8ee74d342fd6)

Use the **Spout Receiver** component to receive a video stream. It stores
incoming frames in the Target Texture and overrides the material property set
in the Target Renderer.

You can also access the received texture via the
`SpoutReceiver.receivedTexture` property.

## Scripting Interface

Enumerate available Spout senders with the `SpoutManager` class; see the
[SourceSelector example] for details.

[SourceSelector example]:
  https://github.com/keijiro/KlakSpout/blob/main/Assets/Scripts/SourceSelector.cs

You can create Spout senders or receivers at runtime, but you must assign the
`SpoutResources` asset (which holds references to package assets) after
instantiation.

## Frequently Asked Questions

### What's the difference between NDI and Spout?

- NDI: Video-over-IP codec/protocol
- Spout: Interprocess GPU memory sharing on DirectX

NDI consumes CPU, memory, and network bandwidth but is highly versatile.

Spout adds virtually no CPU load, though its applications are more limited.

If you need to share video between applications on a single Windows PC, Spout
is usually the better option.
