using System.Collections.Generic;
using UnityEngine;

public class HandGrabber : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandTracking handTracking;
    [Tooltip("5 palm landmarks (Point 0, 5, 9, 13, 17). Dùng để tính anchor + rotation, ổn định khi nắm tay.")]
    [SerializeField] private Transform[] palmPoints = new Transform[5];
    [Tooltip("Index trong palmPoints trỏ vào Point 0 (wrist) — dùng để tính rotation. Mặc định 0.")]
    [SerializeField] private int wristIndex = 0;
    [Tooltip("Index trong palmPoints trỏ vào Point 5 (index MCP) — dùng để tính palm normal. Mặc định 1.")]
    [SerializeField] private int indexMcpIndex = 1;
    [Tooltip("Index trong palmPoints trỏ vào Point 9 (middle MCP) — dùng để tính rotation. Mặc định 2.")]
    [SerializeField] private int middleMcpIndex = 2;

    [Header("Detection Points")]
    [Tooltip("Các điểm tham gia tính vùng detect grab (OverlapSphere tại mỗi điểm). Để trống thì tự động dùng tất cả 21 landmark từ HandTracking.Points.")]
    [SerializeField] private Transform[] detectionPoints;

    [Header("Anchor Override (for model alignment)")]
    [Tooltip("Nếu set, palmAnchor sẽ follow Transform này thay vì centroid 5 palm points. Kéo bone của model (HandCon.bone) hoặc một empty child trên model vào đây để object cầm khớp với visual hand model.")]
    [SerializeField] private Transform palmAnchorOverride;
    [Tooltip("Offset vị trí (local theo override transform) — thường dùng để dịch từ wrist bone vào tâm lòng tay. Local +y thường là hướng ngón.")]
    [SerializeField] private Vector3 anchorPositionOffset = Vector3.zero;
    [Tooltip("Offset rotation (euler, local theo override transform)")]
    [SerializeField] private Vector3 anchorRotationOffset = Vector3.zero;

    [Header("Grab Settings")]
    [Tooltip("Tên gesture từ Python để kích hoạt grab")]
    [SerializeField] private string grabGesture = "Close";
    [Tooltip("Bán kính trigger quanh mỗi detection point. Union các vùng = vùng bao bàn tay.")]
    [SerializeField] private float pointRadius = 0.35f;
    [Tooltip("Layer chứa các Grabbable. Để Everything nếu chưa cấu hình layer.")]
    [SerializeField] private LayerMask grabbableMask = ~0;

    [Header("Debounce")]
    [Tooltip("Phải giữ gesture grab liên tục bao lâu (giây) trước khi grab thực sự")]
    [SerializeField] private float grabHoldTime = 0.15f;
    [Tooltip("Phải rời gesture grab liên tục bao lâu (giây) trước khi release. Tăng giá trị này để chống misclassification (ThumbsUp/OK/...) khi đang nắm.")]
    [SerializeField] private float releaseHoldTime = 0.5f;

    [Header("Throw")]
    [Tooltip("Số frame gần nhất dùng để tính velocity lúc release.")]
    [SerializeField, Range(2, 30)] private int velocitySampleCount = 10;
    [Tooltip("Hệ số nhân velocity khi throw. 1.0 = đúng tốc độ tay; >1 ném mạnh hơn; <1 nhẹ hơn.")]
    [SerializeField, Range(0f, 5f)] private float throwMultiplier = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 1f, 0.35f);

    private Transform palmAnchor;
    private Grabbable currentGrab;
    private Grabbable currentHighlight;
    private float grabHeldFor;
    private float releaseHeldFor;
    private readonly HashSet<Grabbable> overlapBuffer = new HashSet<Grabbable>();

    private struct PosSample { public Vector3 pos; public float time; }
    private readonly Queue<PosSample> palmSamples = new Queue<PosSample>();

    private void Awake()
    {
        var anchorGO = new GameObject("PalmAnchor");
        palmAnchor = anchorGO.transform;
        palmAnchor.SetParent(transform, worldPositionStays: false);
    }

    private void LateUpdate()
    {
        if (handTracking == null) return;
        if (palmAnchorOverride == null && !HasValidPalmPoints()) return;

        UpdatePalmAnchor();
        RecordPalmSample();

        string gesture = handTracking.CurrentGesture;
        bool wantGrab = !string.IsNullOrEmpty(gesture) &&
                        gesture.Equals(grabGesture, System.StringComparison.OrdinalIgnoreCase);

        if (wantGrab)
        {
            grabHeldFor += Time.deltaTime;
            releaseHeldFor = 0f;
        }
        else
        {
            releaseHeldFor += Time.deltaTime;
            grabHeldFor = 0f;
        }

        if (currentGrab != null)
        {
            if (releaseHeldFor >= releaseHoldTime)
            {
                ReleaseCurrent();
            }
            else
            {
                // Enforce object luôn ở centroid + rotation đồng bộ với palm
                currentGrab.transform.localPosition = Vector3.zero;
                currentGrab.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            Grabbable nearest = FindNearestGrabbable(out float _);
            HighlightOnly(nearest);
            if (grabHeldFor >= grabHoldTime && nearest != null) GrabTarget(nearest);
        }
    }

    private bool HasValidPalmPoints()
    {
        if (palmPoints == null || palmPoints.Length == 0) return false;
        for (int i = 0; i < palmPoints.Length; i++)
            if (palmPoints[i] == null) return false;
        return true;
    }

    private Vector3 GetPalmCentroid()
    {
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < palmPoints.Length; i++) sum += palmPoints[i].position;
        return sum / palmPoints.Length;
    }

    private void UpdatePalmAnchor()
    {
        if (palmAnchorOverride != null)
        {
            palmAnchor.rotation = palmAnchorOverride.rotation * Quaternion.Euler(anchorRotationOffset);
            palmAnchor.position = palmAnchorOverride.position + palmAnchorOverride.TransformDirection(anchorPositionOffset);
            return;
        }

        palmAnchor.position = GetPalmCentroid();

        Transform wrist = palmPoints[Mathf.Clamp(wristIndex, 0, palmPoints.Length - 1)];
        Transform mid   = palmPoints[Mathf.Clamp(middleMcpIndex, 0, palmPoints.Length - 1)];
        Transform idx   = palmPoints[Mathf.Clamp(indexMcpIndex, 0, palmPoints.Length - 1)];

        Vector3 fingerDir = (mid.position - wrist.position).normalized;
        Vector3 sideDir   = (idx.position - wrist.position).normalized;
        Vector3 palmNormal = Vector3.Cross(fingerDir, sideDir).normalized;

        if (fingerDir.sqrMagnitude > 0.0001f && palmNormal.sqrMagnitude > 0.0001f)
            palmAnchor.rotation = Quaternion.LookRotation(palmNormal, fingerDir);
    }

    private Transform[] GetActiveDetectionPoints()
    {
        if (detectionPoints != null && detectionPoints.Length > 0) return detectionPoints;
        if (handTracking != null && handTracking.Points != null) return handTracking.Points;
        return palmPoints;
    }

    private Grabbable FindNearestGrabbable(out float bestDist)
    {
        bestDist = float.MaxValue;
        Grabbable best = null;
        Vector3 centroid = palmAnchor.position;

        Transform[] pts = GetActiveDetectionPoints();
        overlapBuffer.Clear();
        for (int p = 0; p < pts.Length; p++)
        {
            if (pts[p] == null) continue;
            Collider[] hits = Physics.OverlapSphere(pts[p].position, pointRadius, grabbableMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                var g = hits[i].GetComponentInParent<Grabbable>();
                if (g == null || g.IsGrabbed) continue;
                if (!overlapBuffer.Add(g)) continue;
                float d = (g.transform.position - centroid).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = g;
                }
            }
        }
        return best;
    }

    private void HighlightOnly(Grabbable g)
    {
        if (currentHighlight == g) return;
        if (currentHighlight != null) currentHighlight.SetHighlight(false);
        currentHighlight = g;
        if (currentHighlight != null) currentHighlight.SetHighlight(true);
    }

    private void GrabTarget(Grabbable g)
    {
        g.Grab(palmAnchor);
        currentGrab = g;
        HighlightOnly(null);
        grabHeldFor = 0f;
        releaseHeldFor = 0f;
        // Object đã grab parent vào palmAnchor — nếu collider tay vẫn bật, các Point khác
        // có thể đụng vào object đang cầm và đẩy nó lệch khỏi anchor.
        if (handTracking != null) handTracking.SetHandPhysicsEnabled(false);
    }

    private void ReleaseCurrent()
    {
        Vector3 throwVelocity = ComputePalmVelocity() * throwMultiplier;
        currentGrab.Release(throwVelocity);
        currentGrab = null;
        grabHeldFor = 0f;
        releaseHeldFor = 0f;
        palmSamples.Clear();
        if (handTracking != null) handTracking.SetHandPhysicsEnabled(true);
    }

    private void RecordPalmSample()
    {
        palmSamples.Enqueue(new PosSample { pos = palmAnchor.position, time = Time.time });
        while (palmSamples.Count > velocitySampleCount) palmSamples.Dequeue();
    }

    private Vector3 ComputePalmVelocity()
    {
        if (palmSamples.Count < 2) return Vector3.zero;
        PosSample oldest = default, newest = default;
        bool first = true;
        foreach (var s in palmSamples)
        {
            if (first) { oldest = s; first = false; }
            newest = s;
        }
        float dt = newest.time - oldest.time;
        if (dt < 0.0001f) return Vector3.zero;
        return (newest.pos - oldest.pos) / dt;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Transform[] pts = GetActiveDetectionPoints();
        if (pts != null)
        {
            Gizmos.color = gizmoColor;
            for (int i = 0; i < pts.Length; i++)
            {
                if (pts[i] == null) continue;
                Gizmos.DrawWireSphere(pts[i].position, pointRadius);
            }
        }

        Vector3 anchorPos;
        if (palmAnchorOverride != null)
        {
            anchorPos = palmAnchorOverride.position + palmAnchorOverride.TransformDirection(anchorPositionOffset);
        }
        else
        {
            if (palmPoints == null) return;
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < palmPoints.Length; i++)
            {
                if (palmPoints[i] == null) continue;
                sum += palmPoints[i].position;
                count++;
            }
            if (count == 0) return;
            anchorPos = sum / count;
        }

        Gizmos.color = new Color(1f, 0.6f, 0f, 1f);
        Gizmos.DrawSphere(anchorPos, 0.06f);
    }
}
