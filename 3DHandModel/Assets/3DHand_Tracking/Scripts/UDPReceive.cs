using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPReceive : MonoBehaviour
{

    Thread receiveThread;
    UdpClient client;
    public int port = 5026;
    public bool startRecieving = true;
    public bool printToConsole = true;
    [Tooltip("Log chỉ field gesture (model | rule) thay vì cả chuỗi landmark — tiện debug rule-based.")]
    public bool printGestureToConsole = false;
    public string data;
    [Tooltip("Gesture parse được từ payload gần nhất, dạng 'model=X | rule=Y'. Cập nhật mỗi packet UDP.")]
    public string gesture;


    public void Start()
    {

        receiveThread = new Thread(
            new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }


    // receive thread
    private void ReceiveData()
    {
        client = new UdpClient();
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.ExclusiveAddressUse = false;
        client.Client.Bind(new IPEndPoint(IPAddress.Any, port));

        while (startRecieving)
        {

            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] dataByte = client.Receive(ref anyIP);
                data = Encoding.UTF8.GetString(dataByte);

                gesture = ExtractGesture(data);

                if (printToConsole) { print(data); }
                if (printGestureToConsole) { print(gesture); }
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    // Lấy nhanh model_gesture (index 63) và rule_gesture (index 66) từ payload thô,
    // không cần Update / parse landmark. Trả "model=X | rule=Y" hoặc "(no data)".
    private static string ExtractGesture(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw.Length < 2 || raw[0] != '[') return "(no data)";
        string body = raw.Substring(1, raw.Length - 2);
        string[] parts = body.Split(',');
        string model = parts.Length > 63 ? parts[63].Trim(' ', '\'', '"') : "?";
        string rule  = parts.Length > 66 ? parts[66].Trim(' ', '\'', '"') : "?";
        return $"model={model} | rule={rule}";
    }

    private void OnDestroy()
    {
        startRecieving = false;
        if (client != null)
        {
            client.Close();
        }
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
    }

    private void OnApplicationQuit()
    {
        OnDestroy();
    }
}
