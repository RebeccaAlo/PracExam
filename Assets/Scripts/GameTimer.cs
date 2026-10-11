using TMPro;
using UnityEngine;

/// <summary>
/// Controls the 30-second level countdown timer, updates the HUD display,
/// and delegates outcome resolution to GameOverManager when time expires.
/// </summary>
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Header("Timer Settings")]
    [Tooltip("Starting countdown time in seconds (30 seconds per level specification).")]
    [SerializeField] private float initialTime = 30f;

    [Tooltip("Current remaining time in seconds.")]
    [SerializeField] private float remainingTime = 30f;

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
        if (Time.timeScale <= 0f)
        {
            Time.timeScale = 1f;
        }
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!isTimerRunning) return;

        // Ensure timer doesn't run if level has already ended
        if (GameOverManager.Instance != null && GameOverManager.Instance.CurrentState != LevelState.Playing)
        {
            isTimerRunning = false;
            return;
        }

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
        if (GameOverManager.Instance != null && GameOverManager.Instance.CurrentState != LevelState.Playing) return;

        remainingTime += Mathf.Max(0f, seconds);
        UpdateUI();
    }

    /// <summary>
    /// Subtracts seconds from the remaining game time, clamping at zero to prevent negative time.
    /// Triggers outcome check if the time reaches zero.
    /// </summary>
    public void SubtractTime(float seconds)
    {
        if (GameOverManager.Instance != null && GameOverManager.Instance.CurrentState != LevelState.Playing) return;

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
    /// Pauses the countdown (e.g., when the level ends).
    /// </summary>
    public void StopTimer()
    {
        isTimerRunning = false;
    }

    /// <summary>
    /// Resets the remaining time back to initialTime (30 seconds) and restarts countdown.
    /// </summary>
    public void ResetTimer()
    {
        remainingTime = initialTime;
        isTimerRunning = true;
        UpdateUI();
    }

    /// <summary>
    /// Invoked when the timer reaches 0. Stops countdown and notifies GameOverManager to evaluate outcome.
    /// </summary>
    private void OnTimeExpired()
    {
        isTimerRunning = false;
        Debug.Log("[GameTimer] Level countdown reached 0! Evaluating level outcome.");

        var gameOverManager = GameOverManager.Instance ?? Object.FindAnyObjectByType<GameOverManager>();
        if (gameOverManager != null)
        {
            gameOverManager.OnTimerExpired();
        }
    }

    /// <summary>
    /// Refreshes the on-screen timer display immediately.
    /// </summary>
    public void UpdateUI()
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(remainingTime);
            timerText.text = $"Time: {seconds}s";
        }
    }
}
