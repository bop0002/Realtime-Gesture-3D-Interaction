using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause menu điều khiển bằng gesture:
/// - Giữ gesture "Open" (custom được) trong dwellTimeToPause giây -> Pause.
/// - Khi pause: cursor bám đầu ngón trỏ (gesture "Pointer"); giữ cursor trên
///   1 button trong dwellTimeToClick giây -> invoke onClick.
/// - Resume / Restart đều có thể click bằng chuột (debug) hoặc bằng tay.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HandInput handInput;
    [SerializeField] private Blade blade;
    [SerializeField] private Spawner spawner;
    [SerializeField] private GameManager gameManager;

    [Header("UI")]
    [Tooltip("Panel cha chứa các nút Resume / Restart. Ẩn mặc định, bật khi pause.")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [Tooltip("Image (RectTransform) đại diện cho cursor tay. Chỉ hiện khi đang pause và gesture = cursorGesture.")]
    [SerializeField] private RectTransform handCursor;

    [Header("Gesture")]
    [Tooltip("Gesture để mở pause menu khi đang chơi.")]
    [SerializeField] private string pauseGesture = "Open";
    [Tooltip("Gesture để di chuyển cursor + click button trong menu.")]
    [SerializeField] private string cursorGesture = "Pointer";
    [Tooltip("Phải GIỮ pauseGesture liên tục bao nhiêu giây thì mới pause (chống false-trigger khi vung tay).")]
    [SerializeField] private float dwellTimeToPause = 0.5f;
    [Tooltip("Phải GIỮ cursor đứng yên trên 1 button bao nhiêu giây thì kích hoạt nút.")]
    [SerializeField] private float dwellTimeToClick = 0.4f;

    public bool IsPaused { get; private set; }

    private float pauseDwellAccum;
    private Button hoveredButton;
    private float clickDwellAccum;
    private Canvas cursorCanvas;

    private void Start()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (handCursor != null)
        {
            cursorCanvas = handCursor.GetComponentInParent<Canvas>();
            handCursor.gameObject.SetActive(false);
        }

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
    }

    private void Update()
    {
        if (IsPaused) UpdatePaused();
        else UpdatePlaying();
    }

    private void UpdatePlaying()
    {
        if (handInput == null || !handInput.HandVisible)
        {
            pauseDwellAccum = 0f;
            return;
        }

        if (MatchGesture(handInput.CurrentGesture, pauseGesture))
        {
            pauseDwellAccum += Time.unscaledDeltaTime;
            if (pauseDwellAccum >= dwellTimeToPause)
                Pause();
        }
        else
        {
            pauseDwellAccum = 0f;
        }
    }

    private void UpdatePaused()
    {
        bool isPointer = handInput != null && handInput.HandVisible &&
                         MatchGesture(handInput.CurrentGesture, cursorGesture);

        if (handCursor != null)
        {
            handCursor.gameObject.SetActive(isPointer);
            if (isPointer) PlaceCursor(handInput.ScreenPosition);
        }

        if (!isPointer)
        {
            hoveredButton = null;
            clickDwellAccum = 0f;
            return;
        }

        Button over = FindButtonUnder(handInput.ScreenPosition);

        if (over != hoveredButton)
        {
            hoveredButton = over;
            clickDwellAccum = 0f;
        }

        if (hoveredButton != null)
        {
            clickDwellAccum += Time.unscaledDeltaTime;
            if (clickDwellAccum >= dwellTimeToClick)
            {
                Button toInvoke = hoveredButton;
                hoveredButton = null;
                clickDwellAccum = 0f;
                toInvoke.onClick.Invoke();
            }
        }
    }

    private void PlaceCursor(Vector2 screenPos)
    {
        if (cursorCanvas != null && cursorCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)cursorCanvas.transform, screenPos, cursorCanvas.worldCamera, out Vector2 local);
            handCursor.anchoredPosition = local;
        }
        else
        {
            handCursor.position = screenPos;
        }
    }

    private Button FindButtonUnder(Vector2 screenPos)
    {
        if (resumeButton != null && IsScreenPointInRect((RectTransform)resumeButton.transform, screenPos))
            return resumeButton;
        if (restartButton != null && IsScreenPointInRect((RectTransform)restartButton.transform, screenPos))
            return restartButton;
        return null;
    }

    private static bool IsScreenPointInRect(RectTransform rect, Vector2 screenPos)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);
    }

    private static bool MatchGesture(string actual, string expected)
    {
        return !string.IsNullOrEmpty(expected) &&
               string.Equals(actual, expected, System.StringComparison.OrdinalIgnoreCase);
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        pauseDwellAccum = 0f;
        Time.timeScale = 0f;
        if (blade != null) blade.enabled = false;
        if (spawner != null) spawner.enabled = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        if (blade != null) blade.enabled = true;
        if (spawner != null) spawner.enabled = true;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (handCursor != null) handCursor.gameObject.SetActive(false);
        hoveredButton = null;
        clickDwellAccum = 0f;
    }

    public void Restart()
    {
        Resume();
        if (gameManager != null) gameManager.NewGame();
    }
}
