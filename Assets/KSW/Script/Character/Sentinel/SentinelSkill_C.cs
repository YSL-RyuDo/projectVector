using UnityEngine;

public class SentinelSkill_C : CharacterSkill
{
    public GameObject trapPrefab;
    public Transform throwPoint;
    private bool isReady = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            UseSkill();
        }

        if (isReady)
        {
            if (Input.GetMouseButtonDown(0))
            {
                CharacterSkillUI.Instance.SentinelSkillText(false, "");
                throwTrap(22f);
                isReady = false;
            }
            else if (Input.GetMouseButtonDown(1))
            {
                CharacterSkillUI.Instance.SentinelSkillText(false, "");
                throwTrap(12f);
                isReady = false;
            }
        }
    }

    protected override void UseSkill()
    {
        isReady = !isReady;
        CharacterSkillUI.Instance.SentinelSkillText(true, "≈ı√¥ ∏µÂ: ¡¬≈¨∏Ø(±Ê∞‘ ≈ı√¥), øÏ≈¨∏Ø(¬™∞‘ ≈ı√¥)");
    }

    private void throwTrap(float force)
    {
        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position + transform.forward;
        GameObject obj = Instantiate(trapPrefab, spawnPos, transform.rotation);

        Sentinel_Trap trapScript = obj.GetComponent<Sentinel_Trap>();
        if (trapScript != null)
        {
            Vector3 throwDir = (Camera.main.transform.forward + Vector3.up * 0.2f).normalized;
            trapScript.Launch(throwDir, force);
        }
    }
}