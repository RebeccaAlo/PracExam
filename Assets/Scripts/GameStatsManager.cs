using TMPro;
using UnityEngine;

/// <summary>
/// Combined bridge manager that unifies ScoreManager and GameTimer operations.
/// Preserves backward compatibility while delegating to specialized managers.
/// </summary>
public class GameStatsManager : MonoBehaviour
{
    public static GameStatsManager Instance { get; private set; }

    [Header("Score Settings")]
    [Tooltip("Starting score value.")]
    [SerializeField] private int initialScore = 0;

    [Tooltip("Current score value.")]
    [SerializeField] private int currentScore = 0;

    [Header("Timer Settings")]
    [Tooltip("Starting countdown time in seconds.")]
    [SerializeField] private float initialTime = 60f;

    [Tooltip("Current remaining time in seconds.")]
    [SerializeField] private float remainingTime = 60f;

    [Tooltip("Whether the countdown timer is actively ticking.")]
    [SerializeField] private bool isTimerRunning = true;

    [Header("UI Display References (Optional)")]
    [Tooltip("UI text component displaying current score.")]
    [SerializeField] private TMP_Text scoreText;

    [Tooltip("UI text component displaying remaining time.")]
    [SerializeField] private TMP_Text timerText;

    public int CurrentScore => ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : currentScore;
    public float RemainingTime => GameTimer.Instance != null ? GameTimer.Instance.RemainingTime : remainingTime;
    public bool IsTimerRunning
    {
        get => GameTimer.Instance != null ? GameTimer.Instance.IsTimerRunning : isTimerRunning;
        set
        {
            isTimerRunning = value;
            if (GameTimer.Instance != null)
            {
                if (value) GameTimer.Instance.StartTimer();
                else GameTimer.Instance.StopTimer();
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ResetStats();
    }

    private void Update()
    {
        // Only run standalone countdown if a separate GameTimer component is not present
        if (GameTimer.Instance == null && isTimerRunning && remainingTime > 0f)
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

    public void AddScore(int amount)
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(amount);
        }
        currentScore += Mathf.Max(0, amount);
        UpdateUI();
    }

    public void SubtractScore(int amount)
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.SubtractScore(amount);
        }
        currentScore = Mathf.Max(0, currentScore - amount);
        UpdateUI();
    }

    public void AddTime(float seconds)
    {
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.AddTime(seconds);
        }
        remainingTime += Mathf.Max(0f, seconds);
        UpdateUI();
    }

    public void SubtractTime(float seconds)
    {
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.SubtractTime(seconds);
        }
        remainingTime = Mathf.Max(0f, remainingTime - seconds);
        if (remainingTime <= 0f)
        {
            OnTimeExpired();
        }
        UpdateUI();
    }

    public void ResetStats()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }
        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.ResetTimer();
        }

        currentScore = initialScore;
        remainingTime = initialTime;
        isTimerRunning = true;
        UpdateUI();
    }

    private void OnTimeExpired()
    {
        isTimerRunning = false;
        Debug.Log("[GameStatsManager] Countdown timer reached 0!");

        var gameOverManager = GameOverManager.Instance ?? Object.FindAnyObjectByType<GameOverManager>();
        if (gameOverManager != null && !gameOverManager.IsGameOver)
        {
            gameOverManager.TriggerGameOver();
        }
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {CurrentScore}";
        }

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(RemainingTime / 60f);
            int seconds = Mathf.FloorToInt(RemainingTime % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }
}
