using UnityEngine;
public class HandInput : MonoBehaviour
{
    [SerializeField] private UDPReceive udpReceive;

    [Header("Landmark")]
    [SerializeField] private int controlPointIndex = 8;

    [Header("Camera Resolution (fallback)")]
    [SerializeField] private float cameraWidth = 960;
    [SerializeField] private float cameraHeight = 540;

    [Header("Active Region (normalized 0..1)")]
    [SerializeField] private Vector2 inputRangeX = new Vector2(0f, 1f);
    [SerializeField] private Vector2 inputRangeY = new Vector2(0f, 1f);

    [Header("One Euro Filter (chống jitter)")]
    [SerializeField] private bool useOneEuro = true;
    [Tooltip("Low = smooth no jittering when still")]
    [SerializeField] private float oneEuroMinCutoff = 1.0f;
    [Tooltip("High = low latency when move fast")]
    [SerializeField] private float oneEuroBeta = 0.4f;
    [SerializeField] private float oneEuroDCutoff = 1.0f;

    [Header("Mapping")]
    [Range(0f, 1f)][SerializeField] private float positionSmoothing = 0f;

    [Header("Detection")]
    [Tooltip("Sau bao lâu (giây) không nhận được data hợp lệ thì coi như mất tay.")]
    [SerializeField] private float lostHandTimeout = 0.2f;

    public enum GestureSource
    {
        Model,        
        Rule,         
        EitherMatch,  
        BothMatch,    
    }

    [Header("Gesture Source")]
    [SerializeField] private GestureSource gestureSource = GestureSource.Model;

    [Header("Debug")]
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

        float w = cameraWidth > 0.0001f ? cameraWidth : 960;
        float h = cameraHeight > 0.0001f ? cameraHeight : 540;
        int dimBase = HandPoints * 3 + 1;
        if (parts.Length >= dimBase + 2)
        {
            if (float.TryParse(parts[dimBase], out float fw) && fw > 1f) w = fw;
            if (float.TryParse(parts[dimBase + 1], out float fh) && fh > 1f) h = fh;
        }

        // Normalize pixel camera -> [0,1] thô.
        float nx = px / w;
        float ny = py / h;

        // One Euro Filter
        if (useOneEuro)
        {
            float t = Time.time;
            nx = filterX.Filter(nx, t, oneEuroMinCutoff, oneEuroBeta, oneEuroDCutoff);
            ny = filterY.Filter(ny, t, oneEuroMinCutoff, oneEuroBeta, oneEuroDCutoff);
        }

        if (debugLogRange) TrackRange(nx, ny, w, h);

        float mx = Remap(inputRangeX, nx);
        float my = Remap(inputRangeY, ny);

        screenPos = new Vector3(mx * Screen.width, my * Screen.height, 0f);

        if (parts.Length > HandPoints * 3)
            gesture = parts[HandPoints * 3].Trim(' ', '\'', '"');

        if (parts.Length > dimBase + 2)
            ruleGesture = parts[dimBase + 2].Trim(' ', '\'', '"');

        return true;
    }

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

            float dx = (x - xPrev) / dt;
            float edx = Mathf.Lerp(dxPrev, dx, Alpha(dCutoff, dt));
            dxPrev = edx;

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
