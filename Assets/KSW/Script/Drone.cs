using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Drone : MonoBehaviour
{
    public static Drone Instance { get; private set; }

    public DroneMovement Movement { get; private set; }
    public TaserGun TaserGun { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        Movement = GetComponent<DroneMovement>();
        TaserGun = GetComponent<TaserGun>();
    }
}