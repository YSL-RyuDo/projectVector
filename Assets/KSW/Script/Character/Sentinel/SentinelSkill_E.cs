using UnityEngine;
using System.Collections;

public class SentinelSkill_E : CharacterSkill
{
    [SerializeField] private GameObject trapPrefab;
    [SerializeField] private float maxDistance = 20f;

    [SerializeField] private LayerMask wallLayer;

    private GameObject currentTrap;
    private bool isPlanning = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            UseSkill();
        }

        if (isPlanning)
        {
            HandlePlacement();
        }
    }

    protected override void UseSkill()
    {
        if (currentTrap == null)
        {
            isPlanning = !isPlanning;
            UpdateUI();
        }
        else
        {
            RecallTrap();
        }
    }

    void HandlePlacement()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.red);

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(ray, out hit, maxDistance, wallLayer))
            {
                if (Vector3.Dot(hit.normal, Vector3.up) > 0.5f)
                {
                    return;
                }

                if (Vector3.Dot(hit.normal, Vector3.up) < -0.5f)
                {
                    return;
                }

                Vector3 spawnPos = hit.point;

                if (Input.GetMouseButtonDown(1))
                {
                    spawnPos += ray.direction * 0.7f;
                }

                currentTrap = Instantiate(trapPrefab, spawnPos, Quaternion.LookRotation(hit.normal));

                Rigidbody rb = currentTrap.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                isPlanning = false;
                UpdateUI();
            }
        }
    }

    void UpdateUI()
    {
        if (CharacterSkillUI.Instance != null)
        {
            string msg = isPlanning ? "투척 모드: 좌클릭(앞면), 우클릭(벽 너머)" : "";
            CharacterSkillUI.Instance.SentinelSkillText(isPlanning, msg);
        }
    }

    void RecallTrap()
    {
        Destroy(currentTrap);
        currentTrap = null;
    }
}