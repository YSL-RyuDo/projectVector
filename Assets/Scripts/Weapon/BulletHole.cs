using UnityEngine;

public class BulletHole : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 10f); // 10초 뒤 삭제
    }
}