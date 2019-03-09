KlakSpout
=========

![gif](http://i.imgur.com/LxjjcrY.gif)
![screenshot](https://i.imgur.com/8ywrjLB.png)

**KlakSpout** is a Unity plugin that allows sharing video frames with other
applications using the [Spout] system.

[Spout]: http://spout.zeal.co/

Spout is a video sharing system for Windows that allows applications to share
frames in real time without incurring significant performance overhead. It's
supported by several applications (MadMapper, Resolume, etc.) and frameworks
(Processing, openFrameworks, etc.). It works in a similar way to [Syphon] for
Mac, and it's similarly useful for projection mapping and VJing.

[Syphon]: http://syphon.v002.info/

System requirements
-------------------

- Unity 2017.4 or later.
- KlakSpout only supports Direct3D 11 (DX11) graphics API mode. Other APIs
  (DX9, DX12, OpenGL, etc.) are not supported at the moment.

Installation
------------

Download and import one of the unitypackage files from the [Releases] page.

[Releases]: https://github.com/keijiro/KlakSpout/releases

Spout Sender component
----------------------

The **Spout Sender component** (`SpoutSender`) is used to send frames to other
Spout compatible applications.

There are two modes in Spout Sender:

### Camera capture mode

![inspector](https://i.imgur.com/2QL6G8P.png)

The Spout Sender component runs in the **camera capture mode** when attached to
a camera object. It automatically captures frames rendered by the camera and
publishes them via Spout. The dimensions of the frames are dependent on the
screen/game view size.

Note that the camera capture mode is not compatible with [scriptable render
pipelines]; The render texture mode should be applied in case of using SRP.

[scriptable render pipelines]: https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html

### Render texture mode

![inspector](https://i.imgur.com/ZnqC6jr.png)

The Spout Sender component runs in the **render texture mode** when it's
independent from any camera. In this mode, the sender publishes content of a
render texture that is specified in the **Source Texture** property. This
render texture should be updated in some way -- by attaching to a camera as a
target texture, by [custom render texture], etc.

[render texture]: https://docs.unity3d.com/Manual/class-RenderTexture.html
[custom render texture]: https://docs.unity3d.com/Manual/CustomRenderTextures.html

### Alpha channel support

This controls if the sender includes alpha channel to published frames. In most
use-cases of Unity, the alpha channel in rendered frames is not used and only
contains garbage data. It's generally recommended to turn off the **Alpha
Channel Support** option to prevent causing wrong effects on a receiver side.

Spout Receiver component
------------------------

![inspector](https://i.imgur.com/C3O1RDy.png)

The **Spout Receiver component** (`SpoutReceiver`) is used to receive frames
published by other Spout compatible applications.

### Source Name property

The Spout Receiver tries to connect to a sender which has a name specified in
the **Source Name** property. Note that the search is done with exact match
(case-sensitive). It can be manually edited with the text field, or selected
from the drop-down labelled "Select" that shows currently available Spout
senders.

### Target Texture property

The Spout Receiver updates a render texture specified in the **Target Texture**
property every frame. Note that the Spout Receiver doesn't care about aspect
ratio; the dimensions of the render texture should be manually adjusted to
avoid stretching.

### Target Renderer property

When a renderer component (in most cases it may be a mesh renderer component)
is specified in the **Target Renderer** property, the Spout Receiver sets the
received frames to one of the texture properties of the material used in the
renderer. This is a convenient way to display received frames when they're only
used in a single renderer instance.

### Script interface

The received frames are also accessible via the `receivedTexture` property of
the `SpoutReceiver` class. Note that the `receivedTexture` object is
destroyed/recreated when the settings (e.g. screen size) are changed. It's
recommended to update the reference every frame.

Spout Manager class
-------------------

The **Spout Manager class** (`SpoutManager`) only has one function: getting the
list of sender names that are currently available in the system
(`GetSourceNames`). This is useful for implementing a sender selection UI
for run time use.

![gif](https://i.imgur.com/C4XUzLk.gif)

Please check the [Source Selector example] for detailed use of this function.

[Source Selector example]: Assets/Test/SourceSelector.cs

Frequently Asked Questions
--------------------------

### Can't send/receive more than 10 sources

See [issue #33] in the Spout SDK. You can download the SpoutSettings app from
the thread that allows changing the maximum number of Spout senders.

[issue #33]: https://github.com/leadedge/Spout2/issues/33

### Spout vs NDI: Which one is better?

The answer is simple: If you're going to use a single computer, use Spout. If
you need connecting multiple computers, use NDI.

Spout is a superior solution for local interoperation. It's faster, low latency,
more memory efficient and better quality. It's recommended using Spout unless
multiple computers are involved.

License
-------

[MIT](LICENSE.md)
