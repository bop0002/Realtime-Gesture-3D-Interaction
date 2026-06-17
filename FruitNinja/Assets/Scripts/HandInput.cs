using UnityEngine;
public class HandInput : MonoBehaviour
{
    [SerializeField] private UDPReceive udpReceive;

    [Header("Landmark")]
    [Tooltip("Index của landmark dùng để điều khiển blade. 8 = đầu ngón trỏ (Pointer).")]
    [SerializeField] private int controlPointIndex = 8;

    [Header("Camera Resolution (fallback)")]
    [SerializeField] private float cameraWidth = 640;
    [SerializeField] private float cameraHeight = 480;

    [Header("Active Region (normalized 0..1)")]
    [Tooltip("Khoảng X (theo tỉ lệ khung hình) mà đầu ngón TỚI ĐƯỢC sẽ trải FULL màn ngang. Vd nếu tay chỉ quét tới 0.65 bên phải, đặt Max=0.65. Bật Debug Log Range để biết khoảng thực tế.")]
    [SerializeField] private Vector2 inputRangeX = new Vector2(0f, 1f);
    [Tooltip("Tương tự cho trục Y (dọc).")]
    [SerializeField] private Vector2 inputRangeY = new Vector2(0f, 1f);

    [Header("One Euro Filter (chống jitter)")]
    [Tooltip("Bật bộ lọc thích nghi: tay đứng yên lọc mạnh (hết rung), vung nhanh lọc nhẹ (ít trễ).")]
    [SerializeField] private bool useOneEuro = true;
    [Tooltip("Càng THẤP càng mượt khi tay gần đứng yên (diệt jitter) nhưng trễ hơn. Thử 0.5–2.0.")]
    [SerializeField] private float oneEuroMinCutoff = 1.0f;
    [Tooltip("Càng CAO càng nhạy / ít trễ khi vung nhanh. Tăng nếu thấy lưỡi dao trễ lúc chém.")]
    [SerializeField] private float oneEuroBeta = 0.4f;
    [Tooltip("Cutoff cho đạo hàm tốc độ. Thường để 1.0.")]
    [SerializeField] private float oneEuroDCutoff = 1.0f;

    [Header("Mapping")]
    [Tooltip("Làm mượt vị trí bổ sung (0 = tắt). Thường để 0 khi đã dùng One Euro Filter.")]
    [Range(0f, 1f)][SerializeField] private float positionSmoothing = 0f;

    [Header("Detection")]
    [Tooltip("Sau bao lâu (giây) không nhận được data hợp lệ thì coi như mất tay.")]
    [SerializeField] private float lostHandTimeout = 0.2f;

    public enum GestureSource
    {
        Model,        // chỉ dùng output từ model
        Rule,         // chỉ dùng rule-based đếm ngón từ Python
        EitherMatch,  // ưu tiên Model; nếu Model = None thì lấy Rule
        BothMatch,    // chỉ cho gesture khi Model == Rule
    }

    [Header("Gesture Source")]
    [Tooltip("Chọn nguồn gesture dùng cho CurrentGesture. Blade/PauseMenu đang đọc CurrentGesture nên dùng Model giữ hành vi cũ; BothMatch chắc tay hơn cho gesture 'Open' (pause).")]
    [SerializeField] private GestureSource gestureSource = GestureSource.Model;

    [Header("Debug")]
    [Tooltip("Log khoảng nx/ny thực tế tay quét tới (min/max) để hiệu chỉnh Active Region.")]
    [SerializeField] private bool debugLogRange = false;

    private const int HandPoints = 21;

    public bool HandVisible { get; private set; }

    public Vector3 ScreenPosition { get; private set; }

    public string ModelGesture { get; private set; } = "None";

    public string RuleGesture { get; private set; } = "None";

    public string CurrentGesture { get; private set; } = "None";

    private float lastValidTime = -999f;
    private bool hasSmoothed;

    private float dbgMinNx = 1f, dbgMaxNx = 0f, dbgMinNy = 1f, dbgMaxNy = 0f;

    private readonly OneEuroFilter filterX = new OneEuroFilter();
    private readonly OneEuroFilter filterY = new OneEuroFilter();

    private void Update()
    {
        if (udpReceive == null) { HandVisible = false; return; }

        if (TryParse(udpReceive.data, out Vector3 rawScreen, out string gesture, out string ruleGesture))
        {
            ModelGesture = gesture;
            RuleGesture = ruleGesture;
            CurrentGesture = ResolveCurrentGesture();
            lastValidTime = Time.time;

            if (positionSmoothing > 0f && hasSmoothed)
                ScreenPosition = Vector3.Lerp(rawScreen, ScreenPosition, positionSmoothing);
            else
                ScreenPosition = rawScreen;

            hasSmoothed = true;
        }

        bool stale = Time.time - lastValidTime > lostHandTimeout;
        HandVisible = !stale;
        if (stale)
        {
            ModelGesture = "None";
            RuleGesture = "None";
            CurrentGesture = "None";
            hasSmoothed = false;
            filterX.Reset();
            filterY.Reset();
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

    private bool TryParse(string data, out Vector3 screenPos, out string gesture, out string ruleGesture)
    {
        screenPos = Vector3.zero;
        gesture = "None";
        ruleGesture = "None";

        if (string.IsNullOrEmpty(data) || data.Length < 2 || data[0] != '[') return false;

        string body = data.Substring(1, data.Length - 2);
        string[] parts = body.Split(',');
        if (parts.Length < HandPoints * 3) return false;

        int idx = Mathf.Clamp(controlPointIndex, 0, HandPoints - 1);
        if (!float.TryParse(parts[idx * 3], out float px)) return false;
        if (!float.TryParse(parts[idx * 3 + 1], out float py)) return false;

        float w = cameraWidth > 0.0001f ? cameraWidth : 640;
        float h = cameraHeight > 0.0001f ? cameraHeight : 480;
        int dimBase = HandPoints * 3 + 1; // bỏ qua gesture
        if (parts.Length >= dimBase + 2)
        {
            if (float.TryParse(parts[dimBase], out float fw) && fw > 1f) w = fw;
            if (float.TryParse(parts[dimBase + 1], out float fh) && fh > 1f) h = fh;
        }

        // Normalize pixel camera -> [0,1] thô.
        float nx = px / w;
        float ny = py / h;

        // One Euro Filter trên toạ độ chuẩn hoá (resolution-independent).
        if (useOneEuro)
        {
            float t = Time.time;
            nx = filterX.Filter(nx, t, oneEuroMinCutoff, oneEuroBeta, oneEuroDCutoff);
            ny = filterY.Filter(ny, t, oneEuroMinCutoff, oneEuroBeta, oneEuroDCutoff);
        }

        if (debugLogRange) TrackRange(nx, ny, w, h);

        // Trải khoảng input thực tế (Active Region) ra full màn hình.
        float mx = Remap(inputRangeX, nx);
        float my = Remap(inputRangeY, ny);

        screenPos = new Vector3(mx * Screen.width, my * Screen.height, 0f);

        if (parts.Length > HandPoints * 3)
            gesture = parts[HandPoints * 3].Trim(' ', '\'', '"');

        // Rule gesture nằm sau width/height (index dimBase+2 = 66). Backward-compatible.
        if (parts.Length > dimBase + 2)
            ruleGesture = parts[dimBase + 2].Trim(' ', '\'', '"');

        return true;
    }

    // Map khoảng [range.x, range.y] -> [0,1], clamp để không tràn ngoài màn hình.
    private static float Remap(Vector2 range, float v)
    {
        float a = range.x, b = range.y;
        if (Mathf.Abs(b - a) < 0.0001f) return Mathf.Clamp01(v);
        return Mathf.Clamp01((v - a) / (b - a));
    }

    private void TrackRange(float nx, float ny, float w, float h)
    {
        bool changed = false;
        if (nx < dbgMinNx) { dbgMinNx = nx; changed = true; }
        if (nx > dbgMaxNx) { dbgMaxNx = nx; changed = true; }
        if (ny < dbgMinNy) { dbgMinNy = ny; changed = true; }
        if (ny > dbgMaxNy) { dbgMaxNy = ny; changed = true; }
        if (changed)
            Debug.Log($"[HandInput] frame={w}x{h} | nx[{dbgMinNx:F2}..{dbgMaxNx:F2}] ny[{dbgMinNy:F2}..{dbgMaxNy:F2}]");
    }

    private class OneEuroFilter
    {
        private bool initialized;
        private float xPrev;
        private float dxPrev;
        private float tPrev;

        public void Reset() { initialized = false; }

        public float Filter(float x, float t, float minCutoff, float beta, float dCutoff)
        {
            if (!initialized)
            {
                initialized = true;
                xPrev = x;
                dxPrev = 0f;
                tPrev = t;
                return x;
            }

            float dt = t - tPrev;
            if (dt <= 0f) dt = 1f / 60f;
            tPrev = t;

            // Lowpass đạo hàm để ước lượng tốc độ ổn định.
            float dx = (x - xPrev) / dt;
            float edx = Mathf.Lerp(dxPrev, dx, Alpha(dCutoff, dt));
            dxPrev = edx;

            // Cutoff thích nghi theo tốc độ.
            float cutoff = minCutoff + beta * Mathf.Abs(edx);
            float xFiltered = Mathf.Lerp(xPrev, x, Alpha(cutoff, dt));
            xPrev = xFiltered;
            return xFiltered;
        }

        private static float Alpha(float cutoff, float dt)
        {
            float tau = 1f / (2f * Mathf.PI * cutoff);
            return 1f / (1f + tau / dt);
        }
    }
}
