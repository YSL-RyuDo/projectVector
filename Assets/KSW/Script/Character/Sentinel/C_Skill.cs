using UnityEngine;

public class C_Skill : MonoBehaviour
{
    public GameObject trapPrefab;
    public Transform throwPoint;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ThrowTrap();
        }
    }

    void ThrowTrap()
    {
        Vector3 spawnPos = throwPoint != null ? throwPoint.position : transform.position + transform.forward;

        GameObject obj = Instantiate(trapPrefab, spawnPos, transform.rotation);
        Sentinel_Trap trapScript = obj.GetComponent<Sentinel_Trap>();

        if (trapScript != null)
        {
            Vector3 throwDir = (Camera.main.transform.forward + Vector3.up * 0.3f).normalized;
            trapScript.Launch(throwDir, 18f);
        }
    }
}