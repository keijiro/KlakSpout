using UnityEngine;

namespace Klak.Spout {

// Sender capture methods
public enum CaptureMethod { GameView, Camera, Texture }

//
// Spout sender class (properties)
//
partial class SpoutSender
{
    #region Spout source

    [SerializeField] string _spoutName = "Spout Sender";

    public string spoutName
      { get => _spoutName;
        set => ChangeSpoutName(value); }

    void ChangeSpoutName(string name)
    {
        // Sender refresh on renaming
        if (_spoutName == name) return;
        _spoutName = name;
        ReleaseSender();
    }

    #endregion

    #region Format option

    [SerializeField] bool _keepAlpha = false;

    public bool keepAlpha
      { get => _keepAlpha;
        set => _keepAlpha = value; }

    #endregion

    #region Capture target

    [SerializeField] CaptureMethod _captureMethod = CaptureMethod.GameView;

    public CaptureMethod captureMethod
      { get => _captureMethod;
        set => _captureMethod = value; }

    [SerializeField] Camera _sourceCamera = null;

    public Camera sourceCamera
      { get => _sourceCamera;
        set => _sourceCamera = value; }

    [SerializeField] Texture _sourceTexture = null;

    public Texture sourceTexture
      { get => _sourceTexture;
        set => _sourceTexture = value; }

    #endregion

    #region Resource asset reference

    [SerializeField, HideInInspector] SpoutResources _resources = null;

    public void SetResources(SpoutResources resources)
      => _resources = resources;

    #endregion
}

} // namespace Klak.Spout
