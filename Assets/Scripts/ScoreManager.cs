using TMPro;
using UnityEngine;

/// <summary>
/// Manages player score, updates HUD display, prevents negative scores,
/// and checks for the 10,000 point win condition in real time.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score Settings")]
    [Tooltip("Starting score when the game begins or resets.")]
    [SerializeField] private int initialScore = 0;

    [Tooltip("Current score.")]
    [SerializeField] private int currentScore = 0;

    [Header("UI Reference")]
    [Tooltip("TextMeshPro UI element displaying current score.")]
    [SerializeField] private TMP_Text scoreText;

    public int CurrentScore => currentScore;
    public int InitialScore => initialScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentScore = initialScore;
        UpdateUI();
    }

    private void Start()
    {
        UpdateUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Adds points to the player's score and checks for the win condition (>= 10,000).
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += Mathf.Max(0, amount);
        UpdateUI();

        // Check if the player reaches or exceeds 10,000 points to trigger WIN immediately
        var levelMgr = GameOverManager.Instance ?? Object.FindAnyObjectByType<GameOverManager>();
        if (levelMgr != null && levelMgr.CurrentState == LevelState.Playing)
        {
            if (currentScore >= levelMgr.TargetWinScore)
            {
                levelMgr.TriggerWin();
            }
        }
    }

    /// <summary>
    /// Subtracts points from the player's score, clamping at zero to prevent negative scores.
    /// </summary>
    public void SubtractScore(int amount)
    {
        currentScore = Mathf.Max(0, currentScore - amount);
        UpdateUI();
    }

    /// <summary>
    /// Resets the score to its initial value and refreshes the UI.
    /// </summary>
    public void ResetScore()
    {
        currentScore = initialScore;
        UpdateUI();
    }

    /// <summary>
    /// Refreshes the score display on the UI.
    /// </summary>
    public void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }
}
