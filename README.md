KlakSpout
=========

Fork reason: Update sender to support also Direct3D9 Receivers.

![gif](http://i.imgur.com/LxjjcrY.gif)

**KlakSpout** is a Unity plugin that allows sharing rendered frames with other
applications with using the [Spout] protocol.

The Spout protocol is supported by several frameworks (Processing,
openFrameworks, etc.) and software packages (Resolume, AfterEffects, etc.).
The plugin allows Unity to interoperate with them in real time without
incurring much overhead.

[Spout]: http://spout.zeal.co/

System Requirements and Compatibilities
---------------------------------------

- KlakSpout requires Unity 5.6.0 or later.
- KlakSpout only supports Direct3D 11 (DX11) graphics API mode. Other APIs
  (DX9, DX12, OpenGL core, etc.) are not supported at the moment.

Features
--------

### Sending frames from a camera

You can send rendered frames from a camera in a scene with attaching the
**SpoutSender** component to it.

### Receiving frames from other applications

You can receive frames from other applications and store them into a render
texture, or set them to a material property as an animating texture.

Installation
------------

Download one of the unitypackage files from the [Releases] page and import it
to a project.

[Releases]: https://github.com/keijiro/KlakSpout/releases

Component Reference
-------------------

### SpoutSender component

![inspector](http://i.imgur.com/6oYHWpu.png)

**SpoutSender** is a component for sending rendered frames to other
Spout-compatible applications.

SpoutSender has only one property. **Clear Alpha** controls whether if contents
of the alpha channel are to be shared or discarded. When it's set to true, it
clears up the contents of the alpha channel with 1.0 (100% opacity). It's
useful when the alpha channel doesn't have any particular use.

### SpoutReceiver component

![inspector](http://i.imgur.com/0BWmM8i.png)

**SpoutReceiver** is a component for receiving frames sent from other
Spout-compatible applications.

**Name Filter** is used to select which Spout sender to connect to. The
receiver only tries to connect to a sender that has the given string in its
name. For instance, when Name Filter is set to "resolume", it doesn't connect
to "Processing 1" nor "maxSender", but "resolumeOut". When Name Filter is kept
empty, it tries to connect to the first found sender without name filtering.

SpoutReceiver supports two ways of storing received frames. When a render
texture is set to **Target Texture**, it updates the render texture with the
received frames. When Target Texture is kept null, it automatically allocates
a temporary render texture for storing frames. These render textures are
accessible from scripts with the `sharedTexture` property.

Received frames can be rendered with using material overriding. To override a
material, set a renderer to **Target Renderer**, then select a property to be
overridden from the drop-down list.

Sender List Window
------------------

![Sender List](http://i.imgur.com/XbN7RvC.png)

The **Spout Sender List** window is available from the menu "Window" -> "Spout"
-> "Spout Sender List". It shows the names of the senders that are currently
available.

License
-------

[MIT](LICENSE.md)
