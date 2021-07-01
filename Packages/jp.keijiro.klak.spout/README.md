KlakSpout
=========

![gif](http://i.imgur.com/LxjjcrY.gif)
![screenshot](https://i.imgur.com/8ywrjLB.png)

**KlakSpout** is a Unity plugin that allows sharing video frames with other
applications using the [Spout] system.

[Spout]: http://spout.zeal.co/

System requirements
-------------------

- Unity 2020.3 or later
- Direct3D 11 and 12

How to install
--------------

This package uses the [scoped registry] feature to resolve package dependencies.
Please add the following sections to the manifest file (Packages/manifest.json).

[scoped registry]: https://docs.unity3d.com/Manual/upm-scoped.html

To the `scopedRegistries` section:

```
{
  "name": "Keijiro",
  "url": "https://registry.npmjs.com",
  "scopes": [ "jp.keijiro" ]
}
```

To the `dependencies` section:

```
"jp.keijiro.klak.spout": "2.0.0"
```

After changes, the manifest file should look like below:

```
{
  "scopedRegistries": [
    {
      "name": "Keijiro",
      "url": "https://registry.npmjs.com",
      "scopes": [ "jp.keijiro" ]
    }
  ],
  "dependencies": {
    "jp.keijiro.klak.spout": "2.0.0",
...
```
