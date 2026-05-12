using UnityEngine;
using UnityEngine.InputSystem;

public class Spawner : MonoBehaviour
{
    [Header("설정")]
    public WeaponData[] spawnObjects; // 소환할 프리팹들을 가진 SO
    public Transform[] spawnPoints;   // 소환될 위치들

    void Update()
    {
        // 예시: 't' 키를 누르면 소환 체크 시작
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            CheckAndSpawn();
        }
    }

    public void CheckAndSpawn()
    {
        // 두 배열의 크기가 다를 수 있으므로 안전하게 작은 쪽 기준으로 반복
        int count = Mathf.Min(spawnObjects.Length, spawnPoints.Length);

        for (int i = 0; i < count; i++)
        {
            // 해당 위치(Transform)에 자식 오브젝트가 하나도 없다면
            if (spawnPoints[i].childCount == 0)
            {
                // spawnObjects[i]를 spawnPoints[i] 위치에, 부모를 spawnPoints[i]로 설정하여 소환
                GameObject drop = Instantiate(spawnObjects[i].dropPrefab, spawnPoints[i].position, spawnPoints[i].rotation * Quaternion.Euler(0f, 90f, 0f), spawnPoints[i]);

                drop.GetComponent<DroppedItem>().magAmmo = spawnObjects[i].magSize;
                drop.GetComponent<DroppedItem>().reserveAmmo = spawnObjects[i].maxReserveAmmo;
                drop.GetComponent<DroppedItem>().weaponData = spawnObjects[i];

                Debug.Log($"{spawnPoints[i].name} 위치에 {spawnObjects[i].name} 소환 완료!");
            }
        }
    }
}