using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Klak.Spout;

public class SourceSelector : MonoBehaviour
{
    [SerializeField] Dropdown _dropdown = null;

    SpoutReceiver _receiver;
    List<string> _sourceNames;
    bool _disableCallback;

    // HACK: Assuming that the dropdown has more than
    // three child objects only while it's opened.
    bool IsOpened => _dropdown.transform.childCount > 3;

    void Start() => _receiver = GetComponent<SpoutReceiver>();

    void Update()
    {
        // Do nothing if the menu is opened.
        if (IsOpened) return;

        // Spout source name retrieval
        _sourceNames = SpoutManager.GetSourceNames().ToList();

        // Currect selection
        var index = _sourceNames.IndexOf(_receiver.sourceName);

        // Append the current name to the list if it's not found.
        if (index < 0)
        {
            index = _sourceNames.Count;
            _sourceNames.Add(_receiver.sourceName);
        }

        // Disable the callback while updating the menu options.
        _disableCallback = true;

        // Menu option update
        _dropdown.ClearOptions();
        _dropdown.AddOptions(_sourceNames);
        _dropdown.value = index;
        _dropdown.RefreshShownValue();

        // Resume the callback.
        _disableCallback = false;
    }

    public void OnChangeValue(int value)
    {
        if (_disableCallback) return;
        _receiver.sourceName = _sourceNames[value];
    }
}
