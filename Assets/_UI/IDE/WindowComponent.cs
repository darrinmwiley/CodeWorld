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
        if (_subComponents == null) 
        {
            Debug.LogWarning($"[DEBUG] No sub-components defined on {gameObject.name}");
            return;
        }

        foreach (var map in _subComponents)
        {
            if (string.IsNullOrEmpty(map.slotName))
            {
                Debug.LogError($"[DEBUG] Empty slot name found in mapping on {gameObject.name}");
                continue;
            }

            var slot = localRoot.Q<VisualElement>(map.slotName);
            
            if (slot == null)
            {
                Debug.LogError($"[DEBUG] FAILED to find VisualElement slot '{map.slotName}' inside {gameObject.name}. Check your UXML names!");
                continue;
            }

            if (map.controller == null)
            {
                Debug.LogError($"[DEBUG] Slot '{map.slotName}' found, but the Controller reference is MISSING on {gameObject.name}");
                continue;
            }

            Debug.Log($"[DEBUG] Successfully located slot '{map.slotName}'. Initializing controller: {map.controller.gameObject.name}");
            map.controller.Initialize(slot, root);
        }

        root?.UpdateRootConstraints(GetMinimumSize());
    }
}