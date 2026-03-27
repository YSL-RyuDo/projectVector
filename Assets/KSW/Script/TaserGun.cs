using UnityEngine;
using UnityEngine.InputSystem;

public class TaserGun : MonoBehaviour
{
    [SerializeField] private DroneCamera _droneCamera;

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            taserHit();
        }
    }

    private void taserHit()
    {
        Transform cam = _droneCamera.transform;

        Ray ray = new Ray(cam.position, cam.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Debug.DrawRay(cam.position, cam.forward * 100f, Color.yellow, 1f);

            ElectricShock shock = hit.collider.GetComponent<ElectricShock>();
            if (shock != null)
            {
                shock.Shock(3f);
            }
        }
    }
}