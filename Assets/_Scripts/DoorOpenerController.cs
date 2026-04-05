using System.Collections;
using UnityEngine;

public class DoorOpenerController : MonoBehaviour
{
    [Header("Door References")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Open Targets (Local Z)")]
    public float leftDoorOpenLocalZ = 2.22f;
    public float rightDoorOpenLocalZ = -3.45f;

    [Header("Timing")]
    public float openDuration = 1.5f;

    [Header("Input")]
    public KeyCode triggerKey = KeyCode.O;

    private bool _isOpening = false;
    private bool _isOpen = false;

    private void Update()
    {
        if (Input.GetKeyDown(triggerKey) && !_isOpening && !_isOpen)
        {
            StartOpenSequence();
        }
    }

    public void StartOpenSequence()
    {
        if (_isOpening || _isOpen) return;
        StartCoroutine(OpenDoorsRoutine());
    }

    public IEnumerator OpenDoorsRoutine()
    {
        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogWarning("[DoorOpenerController] Missing leftDoor or rightDoor reference.");
            yield break;
        }

        _isOpening = true;

        Vector3 leftStart = leftDoor.localPosition;
        Vector3 rightStart = rightDoor.localPosition;

        Vector3 leftTarget = new Vector3(leftStart.x, leftStart.y, leftDoorOpenLocalZ);
        Vector3 rightTarget = new Vector3(rightStart.x, rightStart.y, rightDoorOpenLocalZ);

        float safeDuration = Mathf.Max(0.01f, openDuration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);

            leftDoor.localPosition = Vector3.Lerp(leftStart, leftTarget, t);
            rightDoor.localPosition = Vector3.Lerp(rightStart, rightTarget, t);

            yield return null;
        }

        leftDoor.localPosition = leftTarget;
        rightDoor.localPosition = rightTarget;

        _isOpening = false;
        _isOpen = true;
    }
}
