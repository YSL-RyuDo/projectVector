using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text bestText;

    private void Start()
    {
        var sm = ScoreManager.Instance;
        sm.OnScoreChanged += UpdateScore;
        sm.OnBestScoreChanged += UpdateBest;

        UpdateScore(sm.Score);
        UpdateBest(sm.BestScore);
    }

    private void OnDestroy()
    {
        var sm = ScoreManager.Instance;
        if (sm == null) return;
        sm.OnScoreChanged -= UpdateScore;
        sm.OnBestScoreChanged -= UpdateBest;
    }

    private void UpdateScore(int score) => scoreText.text = score.ToString();
    private void UpdateBest(int best) => bestText.text = $"BEST {best}";
}