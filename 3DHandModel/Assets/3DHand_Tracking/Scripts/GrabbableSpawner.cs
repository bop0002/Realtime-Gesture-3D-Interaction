using UnityEngine;

public class GrabbableSpawner : MonoBehaviour
{
    public enum Shape { Cube, Sphere, Capsule }

    [System.Serializable]
    public class SpawnEntry
    {
        public Shape shape = Shape.Cube;
        public Vector3 position = new Vector3(3f, 4f, 0f);
        public float size = 0.8f;
        public Color color = Color.cyan;
        public float mass = 0.2f;
    }

    [Header("Manual Grabbables")]
    [Tooltip("Object spawn ở vị trí cụ thể. Để trống nếu chỉ muốn dùng auto-fill.")]
    [SerializeField] private SpawnEntry[] entries = new SpawnEntry[0];

    [Header("Auto-Fill Random Cubes")]
    [Tooltip("Số cube random spawn rải trong vùng playground (cộng dồn với entries trên)")]
    [SerializeField] private int autoFillCount = 30;
    [SerializeField] private float autoFillMinSize = 0.5f;
    [SerializeField] private float autoFillMaxSize = 0.5f;
    [Tooltip("Y spawn (so với mặt box). Random trong khoảng này.")]
    [SerializeField] private Vector2 autoFillSpawnHeightRange = new Vector2(4f, 8f);
    [SerializeField] private float autoFillMargin = 0.5f;

    [Header("Box Container (open-top)")]
    [SerializeField] private bool createBox = true;
    [Tooltip("Vị trí tâm đáy box trong world")]
    [SerializeField] private Vector3 boxPosition = new Vector3(1f, 0f, 0f);
    [Tooltip("Kích thước trong lòng box (rộng x cao x sâu) — playground size")]
    [SerializeField] private Vector3 boxInnerSize = new Vector3(30f, 6f, 18f);
    [SerializeField] private float wallThickness = 0.3f;
    [SerializeField] private Color boxColor = new Color(0.35f, 0.35f, 0.4f);

    private int spawnSerial;

    private void Start()
    {
        if (createBox) SpawnBox();
        SpawnBatch();
    }

    [ContextMenu("Spawn Batch (Entries + AutoFill)")]
    public void SpawnBatch()
    {
        for (int i = 0; i < entries.Length; i++) Spawn(entries[i], spawnSerial++);
        AutoFill();
    }

    private void AutoFill()
    {
        if (autoFillCount <= 0) return;

        float halfW = boxInnerSize.x * 0.5f - autoFillMargin;
        float halfD = boxInnerSize.z * 0.5f - autoFillMargin;

        for (int i = 0; i < autoFillCount; i++)
        {
            var e = new SpawnEntry
            {
                shape = Shape.Cube,
                position = new Vector3(
                    boxPosition.x + Random.Range(-halfW, halfW),
                    boxPosition.y + Random.Range(autoFillSpawnHeightRange.x, autoFillSpawnHeightRange.y),
                    boxPosition.z + Random.Range(-halfD, halfD)
                ),
                size = Random.Range(autoFillMinSize, autoFillMaxSize),
                color = Color.HSVToRGB(Random.value, Random.Range(0.5f, 0.9f), Random.Range(0.7f, 1f)),
                mass = 0.2f
            };
            Spawn(e, spawnSerial++);
        }
    }

    private void Spawn(SpawnEntry e, int index)
    {
        PrimitiveType prim = e.shape switch
        {
            Shape.Sphere  => PrimitiveType.Sphere,
            Shape.Capsule => PrimitiveType.Capsule,
            _             => PrimitiveType.Cube,
        };

        var go = GameObject.CreatePrimitive(prim);
        go.name = $"Grabbable_{e.shape}_{index}";
        go.transform.position = e.position;
        go.transform.localScale = Vector3.one * e.size;

        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.material.color = e.color;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = e.mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        go.AddComponent<Grabbable>();
    }

    private void SpawnBox()
    {
        var root = new GameObject("Box");
        root.transform.position = boxPosition;

        float w = boxInnerSize.x;
        float h = boxInnerSize.y;
        float d = boxInnerSize.z;
        float t = wallThickness;

        CreateWall(root.transform, "Bottom",
            localPos: new Vector3(0f, t * 0.5f, 0f),
            scale:    new Vector3(w + 2f * t, t, d + 2f * t));

        CreateWall(root.transform, "Wall_Front",
            localPos: new Vector3(0f, t + h * 0.5f,  d * 0.5f + t * 0.5f),
            scale:    new Vector3(w + 2f * t, h, t));

        CreateWall(root.transform, "Wall_Back",
            localPos: new Vector3(0f, t + h * 0.5f, -d * 0.5f - t * 0.5f),
            scale:    new Vector3(w + 2f * t, h, t));

        CreateWall(root.transform, "Wall_Left",
            localPos: new Vector3(-w * 0.5f - t * 0.5f, t + h * 0.5f, 0f),
            scale:    new Vector3(t, h, d));

        CreateWall(root.transform, "Wall_Right",
            localPos: new Vector3( w * 0.5f + t * 0.5f, t + h * 0.5f, 0f),
            scale:    new Vector3(t, h, d));
    }

    private void CreateWall(Transform parent, string name, Vector3 localPos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, worldPositionStays: false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;

        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.material.color = boxColor;
    }
}
