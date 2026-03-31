using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class CodeWorldObject : MonoBehaviour
{
    [SerializeField] private string _objectId;
    [SerializeField] private List<string> _implementedApis = new List<string>();

    public string ObjectId => _objectId;
    public IReadOnlyList<string> ImplementedApis => _implementedApis;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(_objectId))
        {
            _objectId = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Base method to handle generic RPC requests for this object.
    /// Derived components override this to do something useful.
    /// </summary>
    public virtual void HandleRpcRequest(string requestType, object payload)
    {
        Debug.Log($"[{gameObject.name}:{_objectId}] Received RPC '{requestType}' with payload: {payload}");
    }
}
