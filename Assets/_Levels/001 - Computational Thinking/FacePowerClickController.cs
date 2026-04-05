using UnityEngine;
using UnityEngine.Events;

public class FacePowerClickController : MonoBehaviour
{
    public ClickListener clickListener;
    public QuadFaceAnimator face;
    public QuadFaceAnimator.FaceMode onMode = QuadFaceAnimator.FaceMode.Idle;

    [Header("Interaction")]
    [Tooltip("When true, extra clicks will not turn the face off.")]
    public bool lockOnDuringDialogue = false;

    public UnityEvent OnFaceTurnedOn;

    void Start() => clickListener.AddClickHandler(OnPress);

    public void OnPress()
    {
        if (!face) return;

        if (face.mode == QuadFaceAnimator.FaceMode.Off)
        {
            // This should play turn-on and land in the right mode.
            if (onMode == QuadFaceAnimator.FaceMode.Talking) face.SetTalking();
            else face.SetIdle(); // Idle
            OnFaceTurnedOn?.Invoke();
        }
        else
        {
            if (lockOnDuringDialogue) return;

            // Must cancel turn-on/talking immediately.
            face.TurnOff();
        }
    }

    public void SetLockOnDuringDialogue(bool shouldLock)
    {
        lockOnDuringDialogue = shouldLock;
    }
}
