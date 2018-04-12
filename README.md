# KlakSpout Submodule (for Unity)
## Feature
- Spout's sender texture doesn't depend on Camera (Screen) resolution
- Resizable texture size
- UnityEvent-based

## Usage
### Import this as submodule in your project:
```
git submodule add https://github.com/nobnak/KlakSpout.git Assets/Packages/KlakSpout
```

### Create (Spout) gather camera
 - Add a camera
   - ClearFlags : Don't clear
   - Culling musk : Nothing
   - Depth : 100 (or any number larger than main cameras')
 - Attach SpoutSender script to this camera
   - Set main cameras' targetTexture as "Event on update texture" event's target
   - Set name and size to spout texture
