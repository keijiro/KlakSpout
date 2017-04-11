using UnityEngine;

namespace Klak.Spout
{
    public class SpoutReceiver : MonoBehaviour
    {
        Texture2D _sharedTexture;
        int _receiverID;
        int _width, _height;

        void Start()
        {
            var senderCount = PluginEntry.CountSharedTextures();
            if (senderCount == 0)
            {
                Destroy(this);
                return;
            }

            _receiverID = PluginEntry.CreateReceiver(PluginEntry.GetSharedTextureNameString(0));
        }

        void OnDestroy()
        {
            PluginEntry.Destroy(_receiverID);
            Destroy(_sharedTexture);
        }

        void Update()
        {
            PluginEntry.Poll();

            if (_sharedTexture == null)
            {
                var ptr = PluginEntry.GetTexturePtr(_receiverID);
                if (ptr != System.IntPtr.Zero)
                {
                    _width = PluginEntry.GetTextureWidth(_receiverID);
                    _height = PluginEntry.GetTextureHeight(_receiverID);

                    _sharedTexture = Texture2D.CreateExternalTexture(
                        _width, _height, TextureFormat.ARGB32, false, false, ptr);


                    GetComponent<Renderer>().material.mainTexture = _sharedTexture;
                }
            }
        }
    }
}
