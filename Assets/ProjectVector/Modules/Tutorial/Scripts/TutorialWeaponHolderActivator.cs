using UnityEngine;

public class TutorialWeaponHolderActivator : MonoBehaviour
{
    [SerializeField] private GameObject weaponHolder;

    private void Start()
    {
        if (weaponHolder == null)
        {
            Debug.LogWarning("[TUTORIAL] WeaponHolder가 연결되지 않았습니다.");
            return;
        }

        weaponHolder.SetActive(true);
    }
}