using TMPro;
using UnityEngine;

/// <summary>
/// Manages player score, updates HUD display, and prevents score from becoming negative.
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

    /// <summary>
    /// Adds points to the player's score and updates the UI immediately.
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += Mathf.Max(0, amount);
        UpdateUI();
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
