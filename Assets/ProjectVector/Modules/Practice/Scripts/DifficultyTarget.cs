using UnityEngine;

public class DifficultyTarget : MonoBehaviour, IDamageable
{
    public EnemySpawner spawner; // 매니저 연결
    public string difficultyLevel; // 인스펙터에서 "Easy", "Hard" 등 입력

    // 총알(Bullet)과 충돌했을 때
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name.Contains("Bullet"))
        {
            TriggerStart();
        }
    }

    // 레이캐스트 등을 통해 외부에서 맞았다고 알려줄 때
    public void TakeDamage()
    {
        TriggerStart();
    }

    private void TriggerStart()
    {
        if (spawner != null)
        {
            spawner.StartTrainingByDifficulty(difficultyLevel);

            // 버튼이 눌렸다는 시각적 효과 (선택 사항)
            Debug.Log($"{difficultyLevel} 버튼 명중!");
        }
    }
}