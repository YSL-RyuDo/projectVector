using UnityEngine;
using UnityEngine.InputSystem;

public class DreamForestSkill : MonoBehaviour
{
    [SerializeField] private GameObject _dreamForestPrefab; // ¸ùÈ¯ÀÇ ½£ ÇÁ¸®ÆÕ

    void Update()
    {
        // XÅ° ´©¸£¸é ±Ã±Ø±â ½ÃÀü
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            activateUltimate();
        }
    }

    private void activateUltimate()
    {
        // ÇÃ·¹ÀÌ¾î ¹ß¹Ø Áö¸é¿¡ »ý¼º
        Vector3 spawnPos = transform.position;
        Instantiate(_dreamForestPrefab, spawnPos, Quaternion.identity);

        Debug.Log("±Ã±Ø±â: ¸ùÈ¯ÀÇ ½£ Àü°³!");
    }
}