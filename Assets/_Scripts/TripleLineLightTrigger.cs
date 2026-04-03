using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Monitors 3 LineControllers and triggers a LightSequenceController when all three
/// are at 100% (transition complete) and match the target color (green).
/// </summary>
public class TripleLineLightTrigger : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The 3 lines that must be 100% and green.")]
    public List<LineController> inputLines;
    
    [Tooltip("The light sequence to start/stop.")]
    public LightSequenceController lightSequence;

    [Header("Logic Settings")]
    public Color targetColor = Color.green;
    public float colorTolerance = 0.05f;

    private bool lastGateResult = false;

    void Update()
    {
        if (lightSequence == null || inputLines == null || inputLines.Count == 0) return;

        bool currentResult = EvaluateGate();

        // Only trigger changes on state transition
        if (currentResult != lastGateResult)
        {
            lastGateResult = currentResult;
            if (currentResult)
            {
                Debug.Log("[TripleLineLightTrigger] Gate TRUE - Starting sequence.");
                lightSequence.StartSequence();
            }
            else
            {
                Debug.Log("[TripleLineLightTrigger] Gate FALSE - Resetting lights.");
                lightSequence.ResetAllLights();
            }
        }
    }

    private bool EvaluateGate()
    {
        // User requested 3 LineControllers specifically
        if (inputLines.Count < 3) return false;

        // Condition: All 3 must be finished transitioning AND match the target color
        return inputLines.All(l => l != null && l.IsTransitionComplete && ColorsAreSimilar(l.CurrentColor, targetColor));
    }

    private bool ColorsAreSimilar(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < colorTolerance &&
               Mathf.Abs(a.g - b.g) < colorTolerance &&
               Mathf.Abs(a.b - b.b) < colorTolerance &&
               Mathf.Abs(a.a - b.a) < colorTolerance;
    }
}
