using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerSkill_X : CharacterSkill
{
    private PlayerIdentity playerIdentity;

    private void Awake()
    {
        playerIdentity = GetComponent<PlayerIdentity>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (!IsMine())
            return;

        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            UseSkill();
        }
    }

    protected override void UseSkill()
    {
        if (!CanUsePurchasedSkill(SkillSlot.X))
            return;

        Vector3 spawnPos = transform.position;

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendControllerUltimateCast(spawnPos);
            Debug.Log($"[SEND CONTROLLER ULTIMATE REQUEST] pos={spawnPos}");
        }
    }

    private bool IsMine()
    {
        if (playerIdentity == null)
            playerIdentity = GetComponent<PlayerIdentity>();

        return playerIdentity != null &&
               NetworkManager.Instance != null &&
               playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;
    }
}