using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerSkill_X : CharacterSkill
{
    [SerializeField] private GameObject _dreamForestPrefab;

    void Update()
    {
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            UseSkill();
        }
    }

    protected override void UseSkill()
    {
        Vector3 spawnPos = transform.position;
        Instantiate(_dreamForestPrefab, spawnPos, Quaternion.identity);
    }
}