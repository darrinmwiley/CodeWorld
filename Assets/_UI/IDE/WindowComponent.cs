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

/// <summary>
/// The supertype for any controller that injects UI into a window slot.
/// Handles the recursive injection of child components.
/// </summary>
public abstract class WindowComponent : MonoBehaviour
{
    [Header("Child Configuration")]
    [Tooltip("Map child controllers to specific VisualElement slots inside this component.")]
    [SerializeField] protected List<WindowMapping> _subComponents;

    /// <summary>
    /// The entry point for UI injection. 
    /// 1. Instantiates its own UI into the provided container.
    /// 2. Iterates through _subComponents to trigger their initialization.
    /// </summary>
    public abstract void Initialize(VisualElement container, IBaseWindow root);

    /// <summary>
    /// Finds the designated slots in the local UXML and tells the assigned child components to initialize themselves there.
    /// </summary>
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
            else
            {
                Debug.LogWarning($"[{gameObject.name}] Could not find slot '{map.slotName}' to inject {map.controller.gameObject.name}. Check UXML naming.");
            }
        }
    }
}