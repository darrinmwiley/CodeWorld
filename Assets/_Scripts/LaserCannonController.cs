using UnityEngine;

public class LaserCannonController : MonoBehaviour
{
    [Header("Mechanical Parts")]
    public Transform cylinder;
    public ItemSocket socketTheta;
    public ItemSocket socketY;

    [Header("Laser System")]
    public LineController laserLine;

    [Header("Movement Settings")]
    public float rotationSpeed = 60f;
    public float elevationSpeed = 1.5f;
    public float maxYHeight = 4.0f;
    public float minYHeight = 0.0f;

    [Header("Thresholds")]
    [Tooltip("How close to the target rotation/height before we consider it 'arrived'.")]
    public float arrivalThreshold = 0.05f;

    private float _targetRotation;
    private float _targetHeight;
    private bool _isLaserActive = false;
    
    // NEW: Store the starting position of the cylinder
    private Vector3 _initialLocalPos;

    void Awake()
    {
        if (cylinder != null)
        {
            _initialLocalPos = cylinder.localPosition;
        }
    }

    void Update()
    {
        UpdateTargets();
        MoveCannon();
        CheckFiringConditions();
    }

    private void UpdateTargets()
    {
        // Target Theta (Rotation)
        _targetRotation = (socketTheta != null && socketTheta.IsOccupied) 
            ? GetValueFromSocket(socketTheta) % 360f 
            : 0f;

        // Target Height (Added to starting Y)
        _targetHeight = (socketY != null && socketY.IsOccupied) 
            ? Mathf.Clamp(3 * GetValueFromSocket(socketY), minYHeight, maxYHeight) 
            : 0f;
    }

    private void MoveCannon()
    {
        if (cylinder == null) return;

        // 1. Rotation (Theta)
        Quaternion targetRot = Quaternion.Euler(0, _targetRotation, 0);
        cylinder.localRotation = Quaternion.RotateTowards(
            cylinder.localRotation, 
            targetRot, 
            rotationSpeed * Time.deltaTime
        );

        // 2. Elevation (Additive Y)
        // We target: Initial Y + Socket Value
        float targetY = _initialLocalPos.y + _targetHeight;
        
        Vector3 targetPos = new Vector3(
            cylinder.localPosition.x, 
            targetY, 
            cylinder.localPosition.z
        );

        cylinder.localPosition = Vector3.MoveTowards(
            cylinder.localPosition, 
            targetPos, 
            elevationSpeed * Time.deltaTime
        );
    }

    private void CheckFiringConditions()
    {
        if (socketTheta == null || socketY == null || laserLine == null) return;

        bool socketsFilled = socketTheta.IsOccupied && socketY.IsOccupied;

        // Check rotation
        bool rotationDone = Quaternion.Angle(cylinder.localRotation, Quaternion.Euler(0, _targetRotation, 0)) < arrivalThreshold;
        
        // Check relative elevation
        float targetY = _initialLocalPos.y + _targetHeight;
        bool elevationDone = Mathf.Abs(cylinder.localPosition.y - targetY) < arrivalThreshold;

        if (socketsFilled && rotationDone && elevationDone)
        {
            if (!_isLaserActive)
            {
                _isLaserActive = true;
                laserLine.RestartTransition();
                Debug.Log("<color=cyan>[Cannon]</color> Target Locked. Firing Laser.");
            }
        }
        else
        {
            if (_isLaserActive)
            {
                _isLaserActive = false;
                laserLine.StopAndClearTransition();
            }
        }
    }

    private float GetValueFromSocket(ItemSocket s)
    {
        GameObject item = s.GetSocketedItem();
        if (item != null && item.TryGetComponent<ItemValue>(out var iv))
        {
            return (float)(iv.ToDouble());
        }
        return 0f;
    }
}