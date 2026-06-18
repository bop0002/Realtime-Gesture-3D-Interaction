using UnityEngine;
using UnityEngine.UI;
using TMPro; // Khai báo dùng TextMeshPro

public class GestureUIController : MonoBehaviour
{
    [Header("References")]
    public HandTracking handTracking;

    public Image fillImage;
    
    public TextMeshProUGUI confirmedGestureText;

    public TextMeshProUGUI detectingGestureText;

    [Header("Settings")]
    public float fillDuration = 3f;
    
    public Color fillColor = Color.green;

    public float dropTolerance = 0.5f;

    private string currentTargetGesture = "";
    private float fillTimer = 0f;
    private float dropTimer = 0f;
    private string confirmedGesture = "None";

    void Start()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = 0;
            fillImage.color = fillColor;
        }
        
        if (confirmedGestureText != null)
        {
            confirmedGestureText.text = "Kết quả: Chưa có";
        }
        if (detectingGestureText != null)
        {
            detectingGestureText.text = "Đang giữ: None";
        }
    }

    void Update()
    {
        if (handTracking == null) return;

        string incomingGesture = handTracking.CurrentGesture;

        if (detectingGestureText != null)
        {
            detectingGestureText.text = $"Đang giữ: {incomingGesture}";
        }

        if (string.IsNullOrEmpty(incomingGesture) || incomingGesture == "None")
        {
            HandleGestureDrop();
        }
        else
        {
            if (currentTargetGesture == "")
            {
                currentTargetGesture = incomingGesture;
                fillTimer = 0f;
                dropTimer = 0f;
            }
            else if (incomingGesture == currentTargetGesture)
            {
                dropTimer = 0f; 
                
                if (fillTimer < fillDuration)
                {
                    fillTimer += Time.deltaTime;
                    if (fillTimer >= fillDuration)
                    {
                        fillTimer = fillDuration;
                        ConfirmGesture(currentTargetGesture);
                    }
                }
            }
            else
            {
                HandleGestureDrop();
            }
        }

        UpdateUI();
    }

    private void HandleGestureDrop()
    {
        if (currentTargetGesture == "") return;

        dropTimer += Time.deltaTime;
        if (dropTimer >= dropTolerance)
        {
            ResetFill();
        }
    }

    private void ConfirmGesture(string gesture)
    {
        if (confirmedGesture != gesture)
        {
            confirmedGesture = gesture;
            if (confirmedGestureText != null)
            {
                confirmedGestureText.text = $"Kết quả: {confirmedGesture}";
            }
        }
    }

    private void ResetFill()
    {
        fillTimer = 0f;
        dropTimer = 0f;
        currentTargetGesture = "";
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = fillTimer / fillDuration;
        }
    }
}
