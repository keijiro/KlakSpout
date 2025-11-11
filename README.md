# KlakSpout

![gif](https://user-images.githubusercontent.com/343936/124232423-993f6c00-db4c-11eb-80d3-4c660a2025d9.gif)
![gif](https://user-images.githubusercontent.com/343936/124217164-c4b55d00-db32-11eb-88f1-735a04bfb235.gif)

**KlakSpout** is a Unity plugin that lets Unity send and receive video streams
through the [Spout] system.

[Spout]: http://spout.zeal.co/

## System Requirements

- Unity 2022.3 or later
- Windows system with DirectX 11/12 support

KlakSpout currently supports only Direct3D 11 and 12; other graphics APIs such
as OpenGL or Vulkan aren't available.

## How to Install

Install the KlakSpout package (`jp.keijiro.klak.spout`) from the "Keijiro"
scoped registry in Package Manager. Follow [these instructions] to add the
registry to your project.

[these instructions]:
  https://gist.github.com/keijiro/f8c7e8ff29bfe63d86b888901b82644c

## Spout Sender component

![Sender](https://user-images.githubusercontent.com/343936/124219895-e2d18c00-db37-11eb-8f96-0829bb757968.png)

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

## Spout Receiver component

![Receiver](https://user-images.githubusercontent.com/343936/124220011-1f9d8300-db38-11eb-985a-2f5bebe4c058.png)

Use the **Spout Receiver** component to receive a video stream. It stores
incoming frames in the Target Texture and overrides the material property set
in the Target Renderer.

You can also access the received texture via the
`SpoutReceiver.receivedTexture` property.

## Scripting interface

Enumerate available Spout senders with the `SpoutManager` class; see the
[SourceSelector example] for details.

[SourceSelector example]:
  https://github.com/keijiro/KlakSpout/blob/main/Assets/Scripts/SourceSelector.cs

You can create Spout senders or receivers at runtime, but you must assign the
`SpoutResources` asset (which holds references to package assets) after
instantiation.

## Frequently asked questions

### What's the difference between NDI and Spout?

- NDI: Video-over-IP codec/protocol
- Spout: Interprocess GPU memory sharing on DirectX

NDI consumes CPU, memory, and network bandwidth but is highly versatile.

Spout adds virtually no CPU load, though its applications are more limited.

If you need to share video between applications on a single Windows PC, Spout
is usually the better option.
