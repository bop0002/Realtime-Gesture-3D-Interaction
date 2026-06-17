using UnityEngine;

public class Blade : MonoBehaviour
{
    public float sliceForce = 5f;
    public float minSliceVelocity = 0.01f;

    [Header("Input")]
    [SerializeField] private HandInput handInput;
    [SerializeField] private string activateGesture = "Pointer";
    [SerializeField] private bool useMouse = true;
    [SerializeField] private float dropTolerance = 0.1f;

    private Camera mainCamera;
    private Collider sliceCollider;
    private TrailRenderer sliceTrail;

    private bool wasActive;
    private float dropTimer;

    public Vector3 direction { get; private set; }
    public bool slicing { get; private set; }

    private void Awake()
    {
        mainCamera = Camera.main;
        sliceCollider = GetComponent<Collider>();
        sliceTrail = GetComponentInChildren<TrailRenderer>();
    }

    private void OnEnable()
    {
        StopSlice();
        wasActive = false;
        dropTimer = 0f;
    }

    private void OnDisable()
    {
        StopSlice();
        wasActive = false;
        dropTimer = 0f;
    }

    private void Update()
    {
        bool active = TryGetInput(out Vector3 screenPos);

        if (active) {
            dropTimer = 0f;
            if (!wasActive) StartSlice(screenPos);
            else            ContinueSlice(screenPos);
            wasActive = true;
        } else if (wasActive) {
            dropTimer += Time.deltaTime;
            if (dropTimer >= dropTolerance) {
                StopSlice();
                wasActive = false;
                dropTimer = 0f;
            }
        }
    }

    private bool TryGetInput(out Vector3 screenPos)
    {
        if (handInput != null && handInput.HandVisible) {
            bool gestureOk = string.IsNullOrEmpty(activateGesture) ||
                string.Equals(handInput.CurrentGesture, activateGesture, System.StringComparison.OrdinalIgnoreCase);
            if (gestureOk) {
                screenPos = handInput.ScreenPosition;
                return true;
            }
        }

        if (useMouse && Input.GetMouseButton(0)) {
            screenPos = Input.mousePosition;
            return true;
        }

        screenPos = Vector3.zero;
        return false;
    }

    private void StartSlice(Vector3 screenPos)
    {
        Vector3 position = mainCamera.ScreenToWorldPoint(screenPos);
        position.z = 0f;
        transform.position = position;

        slicing = true;
        sliceCollider.enabled = true;
        sliceTrail.enabled = true;
        sliceTrail.Clear();
    }

    private void StopSlice()
    {
        slicing = false;
        sliceCollider.enabled = false;
        sliceTrail.enabled = false;
    }

    private void ContinueSlice(Vector3 screenPos)
    {
        Vector3 newPosition = mainCamera.ScreenToWorldPoint(screenPos);
        newPosition.z = 0f;
        direction = newPosition - transform.position;

        float velocity = direction.magnitude / Time.deltaTime;
        sliceCollider.enabled = velocity > minSliceVelocity;

        transform.position = newPosition;
    }

}
