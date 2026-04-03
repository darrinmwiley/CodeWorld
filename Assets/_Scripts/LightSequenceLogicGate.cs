using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Monitors a list of input LightSequenceControllers and triggers an output
/// sequence ONLY when all inputs are 100% finished.
/// </summary>
public class LightSequenceLogicGate : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The input sequences that must all be 100% complete.")]
    public List<LightSequenceController> inSequences;

    [Tooltip("The output sequence to trigger.")]
    public LightSequenceController outSequence;

    private bool lastEvaluation = false;

    void Update()
    {
        if (outSequence == null || inSequences == null || inSequences.Count == 0) return;

        // Condition: ALL input sequences must have their IsFinished flag set to true.
        bool allFinished = inSequences.All(s => s != null && s.IsFinished);

        // Only trigger on state change to avoid redundant calls
        if (allFinished != lastEvaluation)
        {
            lastEvaluation = allFinished;
            if (allFinished)
            {
                Debug.Log("[LightSequenceLogicGate] ALL inputs finished - Starting output sequence.");
                outSequence.StartSequence();
            }
            else
            {
                Debug.Log("[LightSequenceLogicGate] One or more inputs are no longer finished - Resetting output sequence.");
                outSequence.ResetAllLights();
            }
        }
    }
}
