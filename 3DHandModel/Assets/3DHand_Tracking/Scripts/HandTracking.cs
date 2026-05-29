using UnityEngine;

public class HandTracking : MonoBehaviour
{
    [SerializeField] private UDPReceive udpReceive;
    public Transform[] Points;
    public Line[] Lines;
    public bool IsDebug;
    [SerializeField] private GameObject debugHand;

    [Header("World Scale")]
    [Tooltip("Số chia tọa độ từ Python (pixel) sang world unit. Khi bật Reach Remap, trường này CHỈ còn quy định KÍCH THƯỚC bàn tay (offset mỗi điểm so với cổ tay), không còn ảnh hưởng tầm với. Càng nhỏ tay càng to.")]
    [SerializeField] private float coordinateDivisor = 75f;

    [Header("Reach Remap (Gain)")]
    [Tooltip("Tách tầm với khỏi kích thước tay: ánh xạ vị trí cổ tay (chuẩn hoá 0..1 theo khung hình) vào vùng world rộng reachRange quanh reachCenter. Cho tay với tới toàn playground mà không cần khua nhiều. Tắt để về hành vi map 1:1 cũ.")]
    [SerializeField] private bool enableReachRemap = true;
    [Tooltip("Tâm vùng với tới trong world (thường = tâm playground box). Camera ở giữa khung → điểm này.")]
    [SerializeField] private Vector3 reachCenter = new Vector3(1f, 3.5f, 0f);
    [Tooltip("Bề rộng vùng tay quét tới theo trục X (ngang) và Y (dọc). Mép khung hình → reachCenter ± range/2. Tăng = với xa hơn với cùng quãng tay vật lý (gain).")]
    [SerializeField] private Vector2 reachRange = new Vector2(30f, 9f);
    [Tooltip("Hệ số khuếch đại chiều sâu (Z). Z của MediaPipe nhiễu & nhỏ nên depth vốn hạn chế; 1 = giữ scale cũ, tăng để đẩy forward/back rõ hơn.")]
    [SerializeField] private float depthGain = 1f;
    [Tooltip("Kích thước khung hình camera (px) dùng để chuẩn hoá khi payload UDP không kèm W,H. Khớp cap.set ở Python (mặc định 960x540).")]
    [SerializeField] private Vector2 fallbackFrameSize = new Vector2(960f, 540f);

    [Header("Hand Physics")]
    [Tooltip("Bật để auto-add SphereCollider + kinematic Rigidbody lên 21 Points và driver chúng qua MovePosition trong FixedUpdate (cho phép tay đẩy được vật lý).")]
    [SerializeField] private bool enableHandPhysics = true;
    [Tooltip("Radius của SphereCollider trên mỗi Point. Tune theo coordinateDivisor — tay càng to (divisor nhỏ) thì collider có thể giảm xuống.")]
    [SerializeField] private float pointColliderRadius = 0.15f;

    [Header("Gesture Filter")]
    [Tooltip("Các gesture cần bỏ qua hoàn toàn — khi Python gửi 1 gesture này, ModelGesture sẽ giữ nguyên giá trị trước đó (xem như không nhận gesture mới). Hữu ích khi model nhầm Close ↔ ThumbsUp/OK.")]
    [SerializeField] private string[] ignoredGestures = new[] { "ThumbsUp" };

    public enum GestureSource
    {
        Model,        // chỉ dùng output từ TFLite model (như trước)
        Rule,         // chỉ dùng rule-based đếm ngón từ Python
        EitherMatch,  // ưu tiên Model; nếu Model = None thì lấy Rule
        BothMatch,    // chỉ cho gesture khi Model == Rule (chống false-positive)
    }

    [Header("Gesture Source")]
    [Tooltip("Chọn nguồn gesture dùng cho CurrentGesture. BothMatch an toàn nhất cho các action quan trọng (grab/throw). Model giữ hành vi cũ.")]
    [SerializeField] private GestureSource gestureSource = GestureSource.Model;

    // Output từ TFLite model phía Python (đã qua ignoredGestures filter).
    public string ModelGesture { get; private set; } = "None";
    // Output rule-based đếm ngón (Open / Close / Pointer / Peace) — None nếu Python không gửi.
    public string RuleGesture { get; private set; } = "None";
    // Gesture cuối cùng theo gestureSource — các script khác (HandGrabber, GestureUIController) đọc trường này.
    public string CurrentGesture { get; private set; } = "None";

    //[SerializeField] private GameObject handModel;
    private const int handPoints = 21;
    private (int startPoint,int endPoint)[] bones = new []
    {
        (0,1),(1,2),(2,3),(3,4), //1st finger
        (0,5),(5,6),(6,7),(7,8), //2st finger
        (5,9),(9,10),(10,11),(11,12), //3st finger
        (9,13),(13,14),(14,15),(15,16), //4st finger
        (0,17),(13,17),(17,18),(18,19),(19,20) //5st finger
    };

    // Target local position cho mỗi Point — set trong Update(), consume trong FixedUpdate().
    // Tách 2 step vì kinematic Rigidbody cần MovePosition (FixedUpdate) thay vì set transform
    // trực tiếp — set trực tiếp sẽ teleport rb, engine không sinh velocity → không đẩy được cube.
    private Vector3[] _targetLocalPositions;
    private Rigidbody[] _pointRigidbodies;
    private Collider[] _pointColliders;
    private bool _hasFreshTarget;

    private void Start()
    {
        for(int i =0;i<bones.Length;i++)
        {
            Lines[i].Init(Points[bones[i].startPoint],Points[bones[i].endPoint],$"{bones[i].startPoint} -{bones[i].endPoint}");
        }

        _targetLocalPositions = new Vector3[handPoints];
        for (int i = 0; i < handPoints && i < Points.Length; i++)
        {
            if (Points[i] != null) _targetLocalPositions[i] = Points[i].localPosition;
        }

        if (enableHandPhysics) SetupHandPhysics();
    }

    private void SetupHandPhysics()
    {
        _pointRigidbodies = new Rigidbody[handPoints];
        _pointColliders = new Collider[handPoints];

        for (int i = 0; i < handPoints && i < Points.Length; i++)
        {
            if (Points[i] == null) continue;
            var go = Points[i].gameObject;

            // QUAN TRỌNG: Rigidbody phải add TRƯỚC SphereCollider.
            // Nếu collider add trước khi GO có Rigidbody, Unity register nó vào static
            // collision tree và không tự promote sang dynamic khi rb được add sau đó →
            // cube sleeping không bị wake bởi collider tay. Add rb trước đảm bảo collider
            // sinh ra biết ngay là dynamic collider của rb này.
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _pointRigidbodies[i] = rb;

            var col = go.GetComponent<SphereCollider>();
            if (col == null) col = go.AddComponent<SphereCollider>();
            col.radius = pointColliderRadius;
            col.isTrigger = false;
            _pointColliders[i] = col;
        }

        // Flush pending transform/component changes vào physics scene ngay frame này,
        // tránh trường hợp collider mới add nhưng physics chưa thấy đến tick sau.
        Physics.SyncTransforms();
    }

    /// <summary>
    /// Bật/tắt collider tay runtime — HandGrabber gọi để disable collider lúc đang nắm object
    /// (tránh object đã grab + parent vào palmAnchor bị các collider tay khác đụng).
    /// </summary>
    public void SetHandPhysicsEnabled(bool on)
    {
        if (_pointColliders == null) return;
        for (int i = 0; i < _pointColliders.Length; i++)
        {
            if (_pointColliders[i] != null) _pointColliders[i].enabled = on;
        }
    }

    private void Update()
    {
        debugHand.SetActive(IsDebug);

        string data= udpReceive.data;
        if(string.IsNullOrEmpty(data) || data.Length < 2 || data[0]!='[') return;
        data = data.Remove(0,1);
        data = data.Remove(data.Length-1,1);
        string[] points = data.Split(',');

        var divisor = coordinateDivisor > 0.0001f ? coordinateDivisor : 100f;

        // Cổ tay (Point 0) là gốc: vị trí của nó quyết định tầm với (reach remap),
        // còn các điểm khác chỉ là offset cố định theo divisor (giữ nguyên kích thước tay).
        float wristRawX = float.Parse(points[0]);
        float wristRawY = float.Parse(points[1]);
        float wristRawZ = float.Parse(points[2]);

        Vector3 wristWorld;
        if (enableReachRemap)
        {
            // W,H đi kèm payload ở index 64,65 (sau model_gesture@63). Không có thì dùng fallback.
            float frameW = fallbackFrameSize.x, frameH = fallbackFrameSize.y;
            if (points.Length > handPoints * 3 + 2)
            {
                float.TryParse(points[handPoints * 3 + 1].Trim(' ', '\'', '"'), out float pw);
                float.TryParse(points[handPoints * 3 + 2].Trim(' ', '\'', '"'), out float ph);
                if (pw > 1f) frameW = pw;
                if (ph > 1f) frameH = ph;
            }

            // Chuẩn hoá 0..1 (Python đã flip Y nên ny=0 ở đáy, =1 ở đỉnh).
            float nx = wristRawX / frameW;
            float ny = wristRawY / frameH;
            wristWorld = new Vector3(
                reachCenter.x + (0.5f - nx) * reachRange.x,   // (0.5-nx) giữ mirror X như công thức "7 - x" cũ
                reachCenter.y + (ny - 0.5f) * reachRange.y,
                reachCenter.z + (wristRawZ / divisor) * depthGain);
        }
        else
        {
            // Hành vi cũ: map 1:1 pixel/divisor, gốc world tại "7 - x".
            wristWorld = new Vector3(7f - wristRawX / divisor, wristRawY / divisor, wristRawZ / divisor);
        }

        for(int i = 0;i<handPoints;i++)
        {
            // Offset mỗi điểm so với cổ tay (mirror X để khớp với hệ "7 - x"), chia divisor => kích thước tay cố định.
            var offset = new Vector3(
                -(float.Parse(points[i*3])   - wristRawX) / divisor,
                 (float.Parse(points[i*3+1]) - wristRawY) / divisor,
                 (float.Parse(points[i*3+2]) - wristRawZ) / divisor);

            var tmp = wristWorld + offset;

            // Debug.Log($"X:{tmp.x}, Y: {tmp.y},Z:{tmp.z}" );

            if (enableHandPhysics)
            {
                _targetLocalPositions[i] = tmp;
            }
            else
            {
                Points[i].localPosition = tmp;
            }
        }
        _hasFreshTarget = enableHandPhysics;


        // Parse model gesture (index 63) — backward compatible với payload cũ
        if (points.Length > handPoints * 3)
        {
            string gesture = points[handPoints * 3].Trim(' ', '\'', '"');
            if (!IsIgnoredGesture(gesture))
            {
                ModelGesture = gesture; // chỉ update khi không nằm trong blacklist
            }
        }
        else
        {
            ModelGesture = "None";
        }

        // Parse rule gesture (index 66 — sau model_gesture, width, height).
        // Backward compatible: payload cũ không có thì RuleGesture = "None".
        int ruleIdx = handPoints * 3 + 3;
        if (points.Length > ruleIdx)
        {
            RuleGesture = points[ruleIdx].Trim(' ', '\'', '"');
        }
        else
        {
            RuleGesture = "None";
        }

        CurrentGesture = ResolveCurrentGesture();

        //HandleHandModel();

    }

    private void FixedUpdate()
    {
        if (!enableHandPhysics || !_hasFreshTarget || _pointRigidbodies == null) return;

        for (int i = 0; i < handPoints && i < Points.Length; i++)
        {
            var rb = _pointRigidbodies[i];
            if (rb == null || Points[i] == null) continue;

            // Convert target localPosition → world. Parent có thể là HandTracking transform
            // hoặc null (Points ở scene root) — handle cả 2.
            var parent = Points[i].parent;
            Vector3 worldTarget = parent != null
                ? parent.TransformPoint(_targetLocalPositions[i])
                : _targetLocalPositions[i];

            rb.MovePosition(worldTarget);
        }
    }

    private string ResolveCurrentGesture()
    {
        bool modelValid = !string.IsNullOrEmpty(ModelGesture) && ModelGesture != "None";
        bool ruleValid  = !string.IsNullOrEmpty(RuleGesture)  && RuleGesture  != "None";

        switch (gestureSource)
        {
            case GestureSource.Rule:
                return ruleValid ? RuleGesture : "None";

            case GestureSource.EitherMatch:
                if (modelValid) return ModelGesture;
                if (ruleValid)  return RuleGesture;
                return "None";

            case GestureSource.BothMatch:
                if (modelValid && ruleValid &&
                    string.Equals(ModelGesture, RuleGesture, System.StringComparison.OrdinalIgnoreCase))
                    return ModelGesture;
                return "None";

            case GestureSource.Model:
            default:
                return modelValid ? ModelGesture : "None";
        }
    }


    private void HandleHandModel()
    {
        //handModel.transform.position =  Points[0].transform.localPosition;
    }

    private bool IsIgnoredGesture(string gesture)
    {
        if (string.IsNullOrEmpty(gesture) || ignoredGestures == null) return false;
        for (int i = 0; i < ignoredGestures.Length; i++)
        {
            if (!string.IsNullOrEmpty(ignoredGestures[i]) &&
                gesture.Equals(ignoredGestures[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
