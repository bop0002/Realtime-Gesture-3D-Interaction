using UnityEngine;

public class HandTracking : MonoBehaviour
{
    [SerializeField] private UDPReceive udpReceive;
    public Transform[] Points;
    public Line[] Lines;
    public bool IsDebug;
    [SerializeField] private GameObject debugHand;

    [Header("World Scale")]
    [SerializeField] private float coordinateDivisor = 45f;

    [Header("Reach Remap (Gain)")]    [SerializeField] private bool enableReachRemap = true;
    [SerializeField] private Vector3 reachCenter = new Vector3(1f, 3.5f, 0f);
    [SerializeField] private Vector2 reachRange = new Vector2(30f, 9f);
    [SerializeField] private float depthGain = 1f;
    [SerializeField] private Vector2 fallbackFrameSize = new Vector2(960f, 540f);

    [Header("Hand Physics")]
    [SerializeField] private bool enableHandPhysics = true;
    [SerializeField] private float pointColliderRadius = 0.15f;

    [Header("Gesture Filter")]
    [SerializeField] private string[] ignoredGestures = new[] { "ThumbsUp" };

    public enum GestureSource
    {
        Model,        
        Rule,         
        EitherMatch,  
        BothMatch,    
    }

    [Header("Gesture Source")]
    [SerializeField] private GestureSource gestureSource = GestureSource.Model;

    public string ModelGesture { get; private set; } = "None";
    public string RuleGesture { get; private set; } = "None";
    public string CurrentGesture { get; private set; } = "None";

    private const int handPoints = 21;
    private (int startPoint,int endPoint)[] bones = new []
    {
        (0,1),(1,2),(2,3),(3,4), //1st finger
        (0,5),(5,6),(6,7),(7,8), //2st finger
        (5,9),(9,10),(10,11),(11,12), //3st finger
        (9,13),(13,14),(14,15),(15,16), //4st finger
        (0,17),(13,17),(17,18),(18,19),(19,20) //5st finger
    };

    private Vector3[] _targetLocalPositions;
    private Rigidbody[] _pointRigidbodies;
    private Collider[] _pointColliders;
    private bool _hasFreshTarget;

    private Renderer[] _debugRenderers;
    private bool _debugVisible;
    private bool _debugVisibleInit;

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

        if (debugHand != null)
        {
            debugHand.SetActive(true);
            _debugRenderers = debugHand.GetComponentsInChildren<Renderer>(true);
        }
        ApplyDebugVisible(IsDebug);
    }

    private void ApplyDebugVisible(bool on)
    {
        if (_debugVisibleInit && _debugVisible == on) return;
        _debugVisible = on;
        _debugVisibleInit = true;
        if (_debugRenderers == null) return;
        for (int i = 0; i < _debugRenderers.Length; i++)
            if (_debugRenderers[i] != null) _debugRenderers[i].enabled = on;
    }

    private void SetupHandPhysics()
    {
        _pointRigidbodies = new Rigidbody[handPoints];
        _pointColliders = new Collider[handPoints];

        for (int i = 0; i < handPoints && i < Points.Length; i++)
        {
            if (Points[i] == null) continue;
            var go = Points[i].gameObject;
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

        Physics.SyncTransforms();
    }

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
        ApplyDebugVisible(IsDebug);

        string data= udpReceive.data;
        if(string.IsNullOrEmpty(data) || data.Length < 2 || data[0]!='[') return;
        data = data.Remove(0,1);
        data = data.Remove(data.Length-1,1);
        string[] points = data.Split(',');

        var divisor = coordinateDivisor > 0.0001f ? coordinateDivisor : 45;

        float wristRawX = float.Parse(points[0]);
        float wristRawY = float.Parse(points[1]);
        float wristRawZ = float.Parse(points[2]);

        Vector3 wristWorld;
        if (enableReachRemap)
        {
            float frameW = fallbackFrameSize.x, frameH = fallbackFrameSize.y;
            if (points.Length > handPoints * 3 + 2)
            {
                float.TryParse(points[handPoints * 3 + 1].Trim(' ', '\'', '"'), out float pw);
                float.TryParse(points[handPoints * 3 + 2].Trim(' ', '\'', '"'), out float ph);
                if (pw > 1f) frameW = pw;
                if (ph > 1f) frameH = ph;
            }

            float nx = wristRawX / frameW;
            float ny = wristRawY / frameH;
            wristWorld = new Vector3(
                reachCenter.x + (0.5f - nx) * reachRange.x,   
                reachCenter.y + (ny - 0.5f) * reachRange.y,
                reachCenter.z + (wristRawZ / divisor) * depthGain);
        }
        else
        {
            wristWorld = new Vector3(7f - wristRawX / divisor, wristRawY / divisor, wristRawZ / divisor);
        }

        for(int i = 0;i<handPoints;i++)
        {
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


        if (points.Length > handPoints * 3)
        {
            string gesture = points[handPoints * 3].Trim(' ', '\'', '"');
            if (!IsIgnoredGesture(gesture))
            {
                ModelGesture = gesture;
            }
        }
        else
        {
            ModelGesture = "None";
        }

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


    }

    private void FixedUpdate()
    {
        if (!enableHandPhysics || !_hasFreshTarget || _pointRigidbodies == null) return;

        for (int i = 0; i < handPoints && i < Points.Length; i++)
        {
            var rb = _pointRigidbodies[i];
            if (rb == null || Points[i] == null) continue;

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
