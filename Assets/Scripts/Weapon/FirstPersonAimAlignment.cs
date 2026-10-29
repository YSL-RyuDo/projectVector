using UnityEngine;

// Corrects only the local first-person presentation; shot direction, spread and network data stay unchanged.
// The rig component and optical reference are authored in the character/weapon prefabs before Play.
[DefaultExecutionOrder(1000)]
public sealed class FirstPersonAimAlignment : MonoBehaviour
{
    [SerializeField] private Camera aimCamera;
    [SerializeField] private PlayerItem inventory;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private float blendSpeed = 18f;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private bool corrected;
    private float weight;
    private Weapon previousWeapon;

    private void RestoreBase()
    {
        if (!corrected) return;
        transform.localPosition = basePosition;
        transform.localRotation = baseRotation;
        corrected = false;
    }
    private void Update() => RestoreBase();
    private void OnDisable() { RestoreBase(); weight = 0; previousWeapon = null; }
    private void LateUpdate()
    {
        if (GetComponentInParent<ControllerSmokeMotion>()?.Active == true) { weight=0; return; }
        if (GetComponentInParent<ControllerSwampMotion>()?.Active == true) { weight=0; return; }
        if (GetComponentInParent<ControllerDecoyMotion>()?.Active == true) { weight=0; return; }
        if (GetComponentInParent<ControllerForestMotion>()?.Active == true) { weight=0; return; }
        if (GetComponentInParent<InitiatorSkill_E>()?.BlocksActions == true) { weight=0; return; }
        if (movement == null || !movement.IsMine || aimCamera == null || inventory == null) return;
        var weapon = inventory.GetCurrentWeapon();
        if (weapon != previousWeapon) { weight = 0; previousWeapon = weapon; }
        var sight = weapon != null ? weapon.GetComponent<WeaponADS>()?.SightReference : null;
        bool aiming = weapon != null && weapon.IsAiming && !inventory.IsReloading && sight != null;
        weight = Mathf.Lerp(weight, aiming ? 1 : 0, 1 - Mathf.Exp(-blendSpeed * Time.deltaTime));
        if (weight < .0001f || sight == null || !sight.IsChildOf(transform)) return;

        basePosition = transform.localPosition; baseRotation = transform.localRotation;
        var position = transform.position; var rotation = transform.rotation;
        // Keep each authored rig's viewing distance; remove only lateral/vertical and angular error.
        float depth = Mathf.Clamp(Vector3.Dot(sight.position - aimCamera.transform.position, aimCamera.transform.forward), .12f, 1f);
        var correction = aimCamera.transform.rotation * Quaternion.Inverse(sight.rotation);
        transform.rotation = correction * rotation;
        transform.position += aimCamera.transform.position + aimCamera.transform.forward * depth - sight.position;
        var alignedPosition = transform.position; var alignedRotation = transform.rotation;
        transform.SetPositionAndRotation(Vector3.Lerp(position, alignedPosition, weight), Quaternion.Slerp(rotation, alignedRotation, weight));
        corrected = true;
    }
}
