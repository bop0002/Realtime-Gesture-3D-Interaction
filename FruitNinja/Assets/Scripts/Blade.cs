using UnityEngine;

public class Blade : MonoBehaviour
{
    public float sliceForce = 5f;
    public float minSliceVelocity = 0.01f;

    [Header("Input")]
    [Tooltip("Nguồn điều khiển bằng tay (UDP/MediaPipe). Để trống thì chỉ dùng chuột.")]
    [SerializeField] private HandInput handInput;
    [Tooltip("Chỉ kích hoạt blade khi gesture này đang bật (vd 'Pointer'). Để trống = luôn kích hoạt khi thấy tay.")]
    [SerializeField] private string activateGesture = "Pointer";
    [Tooltip("Cho phép điều khiển bằng chuột song song (tiện debug khi chưa chạy camera).")]
    [SerializeField] private bool useMouse = true;

    private Camera mainCamera;
    private Collider sliceCollider;
    private TrailRenderer sliceTrail;

    private bool wasActive;

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
    }

    private void OnDisable()
    {
        StopSlice();
        wasActive = false;
    }

    private void Update()
    {
        bool active = TryGetInput(out Vector3 screenPos);

        if (active && !wasActive) {
            StartSlice(screenPos);
        } else if (!active && wasActive) {
            StopSlice();
        } else if (active) {
            ContinueSlice(screenPos);
        }

        wasActive = active;
    }

    // Ưu tiên tay khi đang thấy tay; nếu không thì rơi về chuột (nếu bật).
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
