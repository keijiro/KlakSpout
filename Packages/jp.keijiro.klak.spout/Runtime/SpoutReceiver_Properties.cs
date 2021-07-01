using UnityEngine;

namespace Klak.Spout {

//
// Spout receiver class (properties)
//
partial class SpoutReceiver
{
    #region Spout source

    [SerializeField] string _sourceName = null;

    public string sourceName
      { get => _sourceName;
        set => ChangeSourceName(value); }

    void ChangeSourceName(string name)
    {
        // Receiver refresh on source changes
        if (_sourceName == name) return;
        _sourceName = name;
        ReleaseReceiver();
    }

    #endregion

    #region Destination settings

    [SerializeField] RenderTexture _targetTexture = null;

    public RenderTexture targetTexture
      { get => _targetTexture;
        set => _targetTexture = value; }

    [SerializeField] Renderer _targetRenderer = null;

    public Renderer targetRenderer
      { get => _targetRenderer;
        set => _targetRenderer = value; }

    [SerializeField] string _targetMaterialProperty = null;

    public string targetMaterialProperty
      { get => _targetMaterialProperty;
        set => _targetMaterialProperty = value; }

    #endregion

    #region Runtime property

    public RenderTexture receivedTexture
      => _buffer != null ? _buffer : _targetTexture;

    #endregion

    #region Resource asset reference

    [SerializeField, HideInInspector] SpoutResources _resources = null;

    public void SetResources(SpoutResources resources)
      => _resources = resources;

    #endregion
}

} // namespace Klak.Spout
