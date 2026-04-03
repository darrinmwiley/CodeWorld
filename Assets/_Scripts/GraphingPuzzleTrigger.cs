using UnityEngine;

/// <summary>
/// Monitors a Puzzle1GraphingUtility and triggers separate left/right LightSequenceControllers
/// when the generated y = mx + b line intersects the target points.
/// </summary>
public class GraphingPuzzleTrigger : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The graphing utility that calculates the y = mx + b line.")]
    public Puzzle1GraphingUtility graphingUtil;

    [Tooltip("The light sequence to trigger for the first target point (targetPointA).")]
    public LightSequenceController leftSequence;

    [Tooltip("The light sequence to trigger for the second target point (targetPointB).")]
    public LightSequenceController rightSequence;

    private bool lastLeftHit = false;
    private bool lastRightHit = false;

    void Update()
    {
        if (graphingUtil == null || leftSequence == null || rightSequence == null) return;
        if (graphingUtil.resultLine == null) return;

        // "Wait for the line to be drawn before going"
        // Condition: The laser must be active AND the LineController must have finished its transition.
        bool isLineStable = graphingUtil.IsLaserActive && graphingUtil.resultLine.IsTransitionComplete;

        bool currentLeftHit = false;
        bool currentRightHit = false;

        if (isLineStable)
        {
            float m = graphingUtil.CurrentM;
            float b = graphingUtil.CurrentB;
            float r = graphingUtil.pointRadius;

            // Check Point A (Left)
            currentLeftHit = IsPointIntersected(graphingUtil.targetPointA, m, b, r);
            
            // Check Point B (Right)
            currentRightHit = IsPointIntersected(graphingUtil.targetPointB, m, b, r);
        }

        // Handle Left Sequence State Change
        if (currentLeftHit != lastLeftHit)
        {
            lastLeftHit = currentLeftHit;
            if (currentLeftHit)
            {
                Debug.Log("[GraphingPuzzleTrigger] Left Point Intersected - Starting Left Sequence.");
                leftSequence.StartSequence();
            }
            else
            {
                Debug.Log("[GraphingPuzzleTrigger] Left Point Intersection LOST - Resetting Left Sequence.");
                leftSequence.ResetAllLights();
            }
        }

        // Handle Right Sequence State Change
        if (currentRightHit != lastRightHit)
        {
            lastRightHit = currentRightHit;
            if (currentRightHit)
            {
                Debug.Log("[GraphingPuzzleTrigger] Right Point Intersected - Starting Right Sequence.");
                rightSequence.StartSequence();
            }
            else
            {
                Debug.Log("[GraphingPuzzleTrigger] Right Point Intersection LOST - Resetting Right Sequence.");
                rightSequence.ResetAllLights();
            }
        }
    }

    /// <summary>
    /// Calculates the perpendicular distance from a point to the line y = mx + b.
    /// Intersected if distance < radius.
    /// </summary>
    private bool IsPointIntersected(Vector2 pt, float m, float b, float radius)
    {
        // Distance from (x0, y0) to Ax + By + C = 0: |Ax0 + By0 + C| / sqrt(A^2 + B^2)
        // For y = mx + b, we have: mx - y + b = 0  => A=m, B=-1, C=b
        float distance = Mathf.Abs(m * pt.x - pt.y + b) / Mathf.Sqrt(m * m + 1);
        return distance < radius;
    }
}
