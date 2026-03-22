using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public struct WindowMapping
{
    [Tooltip("The exact string name of the VisualElement slot in this component's UXML.")]
    public string slotName;
    [Tooltip("The controller responsible for injecting content into this slot.")]
    public WindowComponent controller;
}

public abstract class WindowComponent : MonoBehaviour
{
    [Header("Child Configuration")]
    [Tooltip("Map child controllers to specific VisualElement slots inside this component.")]
    [SerializeField] protected List<WindowMapping> _subComponents;

    public abstract void Initialize(VisualElement container, IBaseWindow root);

    /// <summary>
    /// Returns the minimum size required by this component. 
    /// </summary>
    public virtual Vector2 GetMinimumSize() => Vector2.zero;

    protected void InitializeSubComponents(VisualElement localRoot, IBaseWindow root)
    {
        if (_subComponents == null) return;

        foreach (var map in _subComponents)
        {
            if (string.IsNullOrEmpty(map.slotName) || map.controller == null) continue;

            var slot = localRoot.Q<VisualElement>(map.slotName);
            if (slot != null)
            {
                map.controller.Initialize(slot, root);
            }
        }

        // Notify the root shell of the new aggregated constraints
        root?.UpdateRootConstraints(GetMinimumSize());
    }
}