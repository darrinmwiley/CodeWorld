using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LineLogicGate : MonoBehaviour
{
    public enum GateType { AND, OR, NOT }

    [Header("Configuration")]
    public GateType gateType;
    public List<LineController> inputLines;
    public LineController outputLine;

    [Header("Colors")]
    public Color trueColor = Color.green;
    public Color falseColor = Color.red;
    public Color inactiveColor = Color.black;

    [Header("Settings")]
    [Tooltip("How much difference is allowed between colors to consider them equal.")]
    public float colorTolerance = 0.05f;

    private Color lastEvaluatedColor = Color.black;

    void Update()
    {
        if (outputLine == null || inputLines.Count == 0) return;

        // 1. Check if all relevant inputs are finished transitioning
        if (inputLines.All(line => line.IsTransitionComplete))
        {
            EvaluateGate();
        }
        else
        {
            // If inputs are still moving, the gate output stays inactive
            UpdateOutput(inactiveColor);
        }
    }

    private void EvaluateGate()
    {
        Color resultColor = inactiveColor;
            
        switch (gateType)
        {
            case GateType.AND:
                // AND: TRUE if all are Green, FALSE if any are Red
                if (inputLines.All(l => ColorsAreSimilar(l.CurrentColor, trueColor)))
                    resultColor = trueColor;
                else if (inputLines.Any(l => ColorsAreSimilar(l.CurrentColor, falseColor)))
                    resultColor = falseColor;
                break;

            case GateType.OR:
                // OR: TRUE if any are Green, FALSE if all are Red
                if (inputLines.Any(l => ColorsAreSimilar(l.CurrentColor, trueColor)))
                    resultColor = trueColor;
                else if (inputLines.All(l => ColorsAreSimilar(l.CurrentColor, falseColor)))
                    resultColor = falseColor;
                break;

            case GateType.NOT:
                // NOT: Inverts the first input
                if (ColorsAreSimilar(inputLines[0].CurrentColor, trueColor))
                    resultColor = falseColor;
                else if (ColorsAreSimilar(inputLines[0].CurrentColor, falseColor))
                    resultColor = trueColor;
                break;
        }

        UpdateOutput(resultColor);
    }

    private void UpdateOutput(Color newColor)
    {
        // Use the similarity check to avoid redundant restarts
        if (!ColorsAreSimilar(newColor, lastEvaluatedColor))
        {
            lastEvaluatedColor = newColor;
            outputLine.UpdateLineColors(newColor);
            outputLine.RestartTransition();
        }
    }

    /// <summary>
    /// Compares two colors using a tolerance (fudge factor) to account for float precision.
    /// </summary>
    private bool ColorsAreSimilar(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < colorTolerance &&
               Mathf.Abs(a.g - b.g) < colorTolerance &&
               Mathf.Abs(a.b - b.b) < colorTolerance &&
               Mathf.Abs(a.a - b.a) < colorTolerance;
    }
}