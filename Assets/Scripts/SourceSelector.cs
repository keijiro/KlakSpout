using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Properties;
using Klak.Spout;

public sealed class SourceSelector : MonoBehaviour
{
    [SerializeField] SpoutReceiver _receiver = null;

    [CreateProperty]
    public List<string> SourceList => SpoutManager.GetSourceNames().ToList();

    VisualElement UIRoot
      => GetComponent<UIDocument>().rootVisualElement;

    DropdownField UISelector
      => UIRoot.Q<DropdownField>("source-selector");

    void SelectSource(string name)
      => _receiver.sourceName = name;

    void Start()
    {
        UISelector.dataSource = this;
        UISelector.RegisterValueChangedCallback
          (evt => _receiver.sourceName = evt.newValue);
    }
}
