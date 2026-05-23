using UnityEngine;
using UnityEngine.UI;
using TMPro; // Khai báo dùng TextMeshPro

public class GestureUIController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo script HandTracking (hoặc object chứa nó) vào đây")]
    public HandTracking handTracking;
    
    [Tooltip("Kéo Image (progress bar) vào đây. Image này cần set Image Type = Filled")]
    public Image fillImage;
    
    [Tooltip("Kéo Text (TextMeshPro) vào đây để hiển thị tên cử chỉ CHÍNH THỨC (khi đã đủ thời gian)")]
    public TextMeshProUGUI confirmedGestureText;

    [Tooltip("Kéo Text (TextMeshPro) phụ vào đây để hiển thị cử chỉ ĐANG ĐƯỢC GIỮ (Tùy chọn)")]
    public TextMeshProUGUI detectingGestureText;

    [Header("Settings")]
    [Tooltip("Thời gian (giây) cần giữ nguyên cử chỉ để xác nhận")]
    public float fillDuration = 3f;
    
    [Tooltip("Màu của thanh fill")]
    public Color fillColor = Color.green;

    [Tooltip("Thời gian cho phép nhiễu (mất tín hiệu ngắn hạn) mà không bị reset")]
    public float dropTolerance = 0.5f;

    private string currentTargetGesture = "";
    private float fillTimer = 0f;
    private float dropTimer = 0f;
    private string confirmedGesture = "None";

    void Start()
    {
        // Khởi tạo trạng thái ban đầu
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

        // Lấy cử chỉ hiện tại từ Python gửi qua
        string incomingGesture = handTracking.CurrentGesture;

        if (detectingGestureText != null)
        {
            detectingGestureText.text = $"Đang giữ: {incomingGesture}";
        }

        // Nếu mất tín hiệu hoặc không có cử chỉ gì thì reset thanh fill
        if (string.IsNullOrEmpty(incomingGesture) || incomingGesture == "None")
        {
            HandleGestureDrop();
        }
        else
        {
            // Có cử chỉ mới
            if (currentTargetGesture == "")
            {
                currentTargetGesture = incomingGesture;
                fillTimer = 0f;
                dropTimer = 0f;
            }
            else if (incomingGesture == currentTargetGesture)
            {
                // Đúng cử chỉ đang theo dõi -> Hồi phục drop timer và tăng fill timer
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
                // Nhận được cử chỉ khác -> coi như bị nhiễu
                HandleGestureDrop();
            }
        }

        UpdateUI();
    }

    private void HandleGestureDrop()
    {
        if (currentTargetGesture == "") return;

        dropTimer += Time.deltaTime;
        // Nếu thời gian chập chờn vượt quá mức chịu đựng -> Reset thật sự
        if (dropTimer >= dropTolerance)
        {
            ResetFill();
        }
    }

    private void ConfirmGesture(string gesture)
    {
        // Nếu cử chỉ đã được xác nhận, lưu lại và đổi text
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
        // Cập nhật giao diện thanh fill
        if (fillImage != null)
        {
            fillImage.fillAmount = fillTimer / fillDuration;
        }
    }
}
