using UnityEngine;

public class EnemyDummy: MonoBehaviour, IDamageable // 인터페이스 상속
{
    public EnemySpawner spawner;

    public void TakeDamage()
    { // 맞았을 때 실행될 약속된 함수
        ProcessHit();
    }

    private void ProcessHit()
    {
        if (!gameObject.activeSelf) return;
        spawner.AddScore();
        gameObject.SetActive(false);
    }

    // 총알 충돌 시에도 호출
    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.name.Contains("Bullet")) TakeDamage();
    }
}