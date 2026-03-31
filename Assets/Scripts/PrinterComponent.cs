using System.Collections.Generic;
using UnityEngine;

public class PrinterComponent : CodeWorldObject
{
    private VariablePrinter _vPrinter;

    protected override void Awake()
    {
        base.Awake();
        _vPrinter = GetComponent<VariablePrinter>();

        // Register the "Printer" API for Java querying
        if (!((System.Collections.Generic.List<string>)ImplementedApis).Contains("Printer"))
        {
            ((System.Collections.Generic.List<string>)ImplementedApis).Add("Printer");
        }
    }

    public override void HandleRpcRequest(string requestType, object payload)
    {
        if (_vPrinter == null)
        {
            Debug.LogWarning($"[PrinterComponent] No VariablePrinter found on {gameObject.name} to handle {requestType}");
            return;
        }

        if (requestType == "Print")
        {
            // Map primitive print jobs to the visual PrintShape logic in VariablePrinter
            string type = "int";
            if (payload is double) type = "double";
            if (payload is bool) type = "boolean";
            
            _vPrinter.PrintShape(type, payload.ToString());
        }
        else
        {
            base.HandleRpcRequest(requestType, payload);
        }
    }
}
