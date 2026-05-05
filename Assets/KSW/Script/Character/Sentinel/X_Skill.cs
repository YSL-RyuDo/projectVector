using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class X_Skill : MonoBehaviour
{
    [SerializeField] private GameObject xAreaPrefab;
    [SerializeField] private float chargingTime = 1.5f;
    [SerializeField] private float skillDistance = 25f;

    private Slider chargingSlider;
    private bool isCharging = false;

    void Start()
    {
        GameObject sliderObj = GameObject.Find("ChargingSlider");

        if (sliderObj != null)
        {
            chargingSlider = sliderObj.GetComponent<Slider>();
            chargingSlider.gameObject.SetActive(false);
            chargingSlider.minValue = 0f;
            chargingSlider.maxValue = 1f;
        }
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.X) && Input.GetMouseButtonDown(0) && !isCharging)
        {
            StartCoroutine(XSkillRoutine());
        }
    }

    IEnumerator XSkillRoutine()
    {
        isCharging = true;
        float timer = 0f;

        if (chargingSlider != null)
        {
            chargingSlider.gameObject.SetActive(true);
            chargingSlider.value = 0f;
        }

        while (timer < chargingTime)
        {
            if (!Input.GetKey(KeyCode.X) || !Input.GetMouseButton(0))
            {
                HideUI();
                isCharging = false;
                yield break;
            }

            timer += Time.deltaTime;
            if (chargingSlider != null)
            {
                chargingSlider.value = timer / chargingTime;
            }
            yield return null;
        }

        ExecuteX();
        HideUI();
        isCharging = false;
    }

    void HideUI()
    {
        if (chargingSlider != null)
        {
            chargingSlider.gameObject.SetActive(false);
        }
    }

    void ExecuteX()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        Vector3 spawnPos;

        if (Physics.Raycast(ray, out RaycastHit hit, skillDistance))
        {
            spawnPos = hit.point;
        }
        else
        {
            spawnPos = ray.origin + (ray.direction * skillDistance);
        }

        Instantiate(xAreaPrefab, spawnPos, Quaternion.identity);
        StartCoroutine(ProtectSkillsExternal(10f));
    }

    IEnumerator ProtectSkillsExternal(float duration)
    {
        TrapDevice[] eSkills = FindObjectsByType<TrapDevice>(FindObjectsSortMode.None);

        foreach (var e in eSkills)
        {
            if (e != null) e.enabled = false;
        }

        yield return new WaitForSeconds(duration);

        foreach (var e in eSkills)
        {
            if (e != null) e.enabled = true;
        }
    }
}