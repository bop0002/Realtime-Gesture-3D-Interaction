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
        if(data == null || data[0]!='[') return;
        data = data.Remove(0,1);
        data = data.Remove(data.Length-1,1);
        string[] points = data.Split(',');

        for(int i = 0;i<handPoints;i++)
        {
            var x = 7 - float.Parse(points[i*3])/100f; //Tinh toan sau
            var y = float.Parse(points[i*3+1])/100f;   
            var z = float.Parse(points[i*3+2])/100f;
            
            var tmp = new Vector3(x,y,z);

            // Debug.Log($"X:{tmp.x}, Y: {tmp.y},Z:{tmp.z}" );

            Points[i].localPosition = tmp;
        }

        
        if (points.Length > handPoints * 3)
        {
            string gesture = points[handPoints * 3].Trim(' ', '\'', '"');
            Debug.Log($"Detected Gesture: {gesture}");
        }
        
        //HandleHandModel();

    }


    private void HandleHandModel()
    {
        //handModel.transform.position =  Points[0].transform.localPosition;
    }
    
}
