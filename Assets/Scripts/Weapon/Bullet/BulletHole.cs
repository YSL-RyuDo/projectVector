using UnityEngine;

public class BulletHole : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 3f); // 3초 뒤 삭제
    }
}