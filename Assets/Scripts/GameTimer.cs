using TMPro;
using UnityEngine;

/// <summary>
/// Controls the countdown game timer, updates the HUD display, and triggers Game Over when time expires.
/// Prevents the timer from becoming negative and halts countdown while Game Over is active.
/// </summary>
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Header("Timer Settings")]
    [Tooltip("Starting countdown time in seconds.")]
    [SerializeField] private float initialTime = 60f;

    [Tooltip("Current remaining time in seconds.")]
    [SerializeField] private float remainingTime = 60f;

    [Tooltip("Whether the timer is actively counting down.")]
    [SerializeField] private bool isTimerRunning = true;

    [Header("UI Reference")]
    [Tooltip("TextMeshPro UI element displaying the remaining time.")]
    [SerializeField] private TMP_Text timerText;

    public float RemainingTime => remainingTime;
    public float InitialTime => initialTime;
    public bool IsTimerRunning => isTimerRunning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        remainingTime = initialTime;
        isTimerRunning = true;
        UpdateUI();
    }

    private void Start()
    {
        // Ensure timeScale is not left paused from a previous state
        if (Time.timeScale <= 0f)
        {
            Time.timeScale = 1f;
        }
        UpdateUI();
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        if (remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                OnTimeExpired();
            }
            UpdateUI();
        }
    }

    /// <summary>
    /// Adds seconds to the remaining game time and updates the UI immediately.
    /// </summary>
    public void AddTime(float seconds)
    {
        remainingTime += Mathf.Max(0f, seconds);
        UpdateUI();
    }

    /// <summary>
    /// Subtracts seconds from the remaining game time, clamping at zero to prevent negative time.
    /// Triggers Game Over if the time reaches zero.
    /// </summary>
    public void SubtractTime(float seconds)
    {
        remainingTime = Mathf.Max(0f, remainingTime - seconds);
        UpdateUI();

        if (remainingTime <= 0f)
        {
            OnTimeExpired();
        }
    }

    /// <summary>
    /// Starts or resumes the countdown.
    /// </summary>
    public void StartTimer()
    {
        isTimerRunning = true;
    }

    /// <summary>
    /// Pauses the countdown (e.g., during Game Over screen).
    /// </summary>
    public void StopTimer()
    {
        isTimerRunning = false;
    }

    /// <summary>
    /// Resets the remaining time back to initialTime and restarts the countdown.
    /// </summary>
    public void ResetTimer()
    {
        remainingTime = initialTime;
        isTimerRunning = true;
        UpdateUI();
    }

    /// <summary>
    /// Invoked when the timer reaches 0. Stops countdown and notifies GameOverManager.
    /// </summary>
    private void OnTimeExpired()
    {
        isTimerRunning = false;
        Debug.Log("[GameTimer] Time has run out! Triggering Game Over.");

        var gameOverManager = GameOverManager.Instance ?? Object.FindAnyObjectByType<GameOverManager>();
        if (gameOverManager != null && !gameOverManager.IsGameOver)
        {
            gameOverManager.TriggerGameOver();
        }
    }

    /// <summary>
    /// Refreshes the on-screen timer display immediately.
    /// </summary>
    public void UpdateUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }
}
