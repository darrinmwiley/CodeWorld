using UnityEngine;

/// <summary>
/// Watches a LightSequenceController and triggers a DoorOpenerController
/// once the light sequence reaches 100% (IsFinished == true).
/// </summary>
public class LightSequenceDoorTrigger : MonoBehaviour
{
    [Header("References")]
    public LightSequenceController lightSequence;
    public DoorOpenerController doorOpener;

    private bool _hasTriggered = false;

    private void Update()
    {
        if (_hasTriggered) return;

        if (lightSequence == null || doorOpener == null)
            return;

        if (lightSequence.IsFinished)
        {
            doorOpener.StartOpenSequence();
            _hasTriggered = true;
        }
    }
}
