using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Monitors a LineController with two points and triggers a LightSequenceController
/// when the line's volume (capsule) intersects a target GameObject.
/// </summary>
public class LineIntersectionLightTrigger : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The LineController with 2 points to monitor.")]
    public LineController targetLine;

    [Tooltip("The GameObject that the line must intersect.")]
    public GameObject targetObject;

    [Tooltip("The light sequence to start/stop.")]
    public LightSequenceController lightSequence;

    [Header("Settings")]
    [Tooltip("The layer mask to use for intersection.")]
    public LayerMask layerMask = -1;

    [Tooltip("Optional: Add a small buffer to the line's width for more forgiving intersection.")]
    public float radiusBuffer = 0.05f;

    [Tooltip("Delay after the line transition completes before we start polling intersections.")]
    public float pollDelayAfterLineComplete = 0.15f;

    private bool lastIntersectionResult = false;
    private bool _wasLineCompleteLastFrame = false;
    private float _lineCompleteSinceTime = -1f;

    void Update()
    {
        if (targetLine == null || targetObject == null || lightSequence == null) return;

        bool lineComplete = targetLine.IsTransitionComplete;
        if (lineComplete)
        {
            if (!_wasLineCompleteLastFrame)
                _lineCompleteSinceTime = Time.time;

            _wasLineCompleteLastFrame = true;
        }
        else
        {
            _wasLineCompleteLastFrame = false;
            _lineCompleteSinceTime = -1f;
        }

        bool canPoll = lineComplete &&
                       _lineCompleteSinceTime >= 0f &&
                       (Time.time - _lineCompleteSinceTime) >= Mathf.Max(0f, pollDelayAfterLineComplete);

        bool currentResult = canPoll && CheckIntersection();

        // Only trigger changes on state transition
        if (currentResult != lastIntersectionResult)
        {
            lastIntersectionResult = currentResult;
            if (currentResult)
            {
                Debug.Log($"[LineIntersectionLightTrigger] Intersection with {targetObject.name} DETECTED - Starting sequence.");
                lightSequence.StartSequence();
            }
            else
            {
                Debug.Log($"[LineIntersectionLightTrigger] Intersection with {targetObject.name} LOST - Resetting lights.");
                lightSequence.ResetAllLights();
            }
        }
    }

    private bool CheckIntersection()
    {
        List<Vector3> livePoints = targetLine.GetLivePoints();
        if (livePoints.Count < 2) return false;

        // Use the first two positions for our intersection check
        Vector3 start = livePoints[0];
        Vector3 end = livePoints[1];
        
        // Calculate the radius based on the actual line width
        float radius = (targetLine.LineWidth * 0.5f) + radiusBuffer;

        // Physics.OverlapCapsule is much more reliable than Linecast for "visual" intersections
        // It checks the entire volume between 'start' and 'end' with the given radius.
        Collider[] hits = Physics.OverlapCapsule(start, end, radius, layerMask, QueryTriggerInteraction.Collide);

        foreach (var hit in hits)
        {
            // Verify if the hit object is the target or a child of the target
            if (hit.gameObject == targetObject || hit.transform.IsChildOf(targetObject.transform))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (targetLine == null) return;
        
        List<Vector3> livePoints = targetLine.GetLivePoints();
        if (livePoints.Count < 2) return;

        float r = (targetLine.LineWidth * 0.5f) + radiusBuffer;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(livePoints[0], livePoints[1]);
        Gizmos.DrawWireSphere(livePoints[0], r);
        Gizmos.DrawWireSphere(livePoints[1], r);
    }
}
