using UnityEngine;
using UnityEditor;
using System.Drawing;
public class HandTracking : MonoBehaviour
{
    [SerializeField] private UDPReceive udpReceive;
    public Transform[] Points;
    public Line[] Lines;
    public bool IsDebug;
    [SerializeField] private GameObject debugHand;

    [Header("World Scale")]
    [Tooltip("Số chia tọa độ từ Python (pixel) sang world unit. Càng nhỏ thì bàn tay càng to. 100 = mặc định cũ, 50 = gấp đôi.")]
    [SerializeField] private float coordinateDivisor = 75f;

    [Header("Gesture Filter")]
    [Tooltip("Các gesture cần bỏ qua hoàn toàn — khi Python gửi 1 gesture này, CurrentGesture sẽ giữ nguyên giá trị trước đó (xem như không nhận gesture mới). Hữu ích khi model nhầm Close ↔ ThumbsUp/OK.")]
    [SerializeField] private string[] ignoredGestures = new[] { "ThumbsUp" };

    // Biến lưu trữ cử chỉ nhận được từ Python
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

    private void Start()
    {
        for(int i =0;i<bones.Length;i++)
        {
            Lines[i].Init(Points[bones[i].startPoint],Points[bones[i].endPoint],$"{bones[i].startPoint} -{bones[i].endPoint}");
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

        for(int i = 0;i<handPoints;i++)
        {
            var divisor = coordinateDivisor > 0.0001f ? coordinateDivisor : 100f;
            var x = 7 - float.Parse(points[i*3])/divisor; //Tinh toan sau
            var y = float.Parse(points[i*3+1])/divisor;
            var z = float.Parse(points[i*3+2])/divisor;
            
            var tmp = new Vector3(x,y,z);

            // Debug.Log($"X:{tmp.x}, Y: {tmp.y},Z:{tmp.z}" );

            Points[i].localPosition = tmp;
        }

        
        if (points.Length > handPoints * 3)
        {
            string gesture = points[handPoints * 3].Trim(' ', '\'', '"');
            if (IsIgnoredGesture(gesture))
            {
                //Debug.Log($"Ignored Gesture: {gesture} (keeping previous: {CurrentGesture})");
            }
            else
            {
                CurrentGesture = gesture; // Cập nhật biến
                //Debug.Log($"Detected Gesture: {gesture}"); // Ẩn bớt log cho đỡ rác console
            }
        }
        else
        {
            CurrentGesture = "None";
        }
        
        //HandleHandModel();

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
