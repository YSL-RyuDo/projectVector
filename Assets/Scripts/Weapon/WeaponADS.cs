using UnityEngine;

public class WeaponADS : MonoBehaviour
{
    public Vector3 hipPosition = new Vector3(0.3f, -0.25f, 0.6f);
    public Vector3 adsPosition = new Vector3(0f, -0.15f, 0.5f);
    public float adsMoveSpeed = 12f;

    private bool isADS = false;

    void Update()
    {
        Vector3 target = isADS ? adsPosition : hipPosition;
        transform.localPosition =
            Vector3.Lerp(transform.localPosition, target, adsMoveSpeed * Time.deltaTime);
    }

    public void SetADS(bool ads)
    {
        isADS = ads;
    }
}
