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
            root?.UpdateRootConstraints(GetMinimumReportedMinimumSize());
            return;
        }

        foreach (var map in _subComponents)
        {
            if (string.IsNullOrEmpty(map.slotName))
                continue;

            var slot = localRoot.Q<VisualElement>(map.slotName);
            if (slot == null)
                continue;

            if (map.controller == null)
                continue;

            map.controller.Initialize(slot, root);
        }

        root?.UpdateRootConstraints(GetMinimumReportedMinimumSize());
    }

    protected Vector2 GetAggregatedSubComponentMinimumSize()
    {
        if (_subComponents == null || _subComponents.Count == 0)
            return Vector2.zero;

        Vector2 result = Vector2.zero;
        foreach (var map in _subComponents)
        {
            if (map.controller == null)
                continue;

            Vector2 childMin = map.controller.GetMinimumSize();
            result.x = Mathf.Max(result.x, childMin.x);
            result.y = Mathf.Max(result.y, childMin.y);
        }

        return result;
    }

    protected Vector2 GetMinimumReportedMinimumSize()
    {
        Vector2 own = GetMinimumSize();
        Vector2 child = GetAggregatedSubComponentMinimumSize();
        return new Vector2(Mathf.Max(own.x, child.x), Mathf.Max(own.y, child.y));
    }
}
