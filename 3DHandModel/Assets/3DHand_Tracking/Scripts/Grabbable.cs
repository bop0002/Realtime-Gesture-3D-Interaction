using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grabbable : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f);

    private Rigidbody rb;
    private Color originalColor;
    private bool hasOriginalColor;
    private bool isGrabbed;
    private bool isHighlighted;
    private RigidbodyInterpolation savedInterpolation;

    public bool IsGrabbed => isGrabbed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        CacheOriginalColor();
    }

    private void CacheOriginalColor()
    {
        if (targetRenderer != null && targetRenderer.material.HasProperty("_Color"))
        {
            originalColor = targetRenderer.material.color;
            hasOriginalColor = true;
        }
    }

    public void Grab(Transform holder)
    {
        if (isGrabbed) return;
        isGrabbed = true;
        savedInterpolation = rb.interpolation;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetParent(holder, worldPositionStays: false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        SetHighlight(false);
    }

    public void Release(Vector3 throwVelocity = default)
    {
        if (!isGrabbed) return;
        isGrabbed = false;
        transform.SetParent(null, worldPositionStays: true);
        rb.isKinematic = false;
        rb.interpolation = savedInterpolation;
        rb.linearVelocity = throwVelocity;
        rb.angularVelocity = Vector3.zero;
    }

    public void SetHighlight(bool on)
    {
        if (isGrabbed) on = false;
        if (on == isHighlighted) return;
        isHighlighted = on;
        if (targetRenderer == null || !hasOriginalColor) return;
        targetRenderer.material.color = on ? highlightColor : originalColor;
    }
}
