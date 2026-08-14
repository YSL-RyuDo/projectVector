using System.Collections.Generic;
using UnityEngine;

public class WeaponDropSpawner : MonoBehaviour
{
    public static WeaponDropSpawner Instance;

    [Header("Weapon Database")]
    [SerializeField] private WeaponData[] weaponDatas;

    [Header("Weapon Skin Database")]
    [SerializeField] private WeaponSkinDatabase skinDatabase;

    private readonly Dictionary<uint, GameObject> spawnedDrops = new();

    private void Awake()
    {
        Instance = this;
        if (skinDatabase != null)
            skinDatabase.Register();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public WeaponData GetWeaponData(int weaponId)
    {
        if (weaponDatas == null)
            return null;

        foreach (WeaponData data in weaponDatas)
        {
            if (data == null)
                continue;

            if (data.weaponId == weaponId)
                return data;
        }

        return null;
    }

    public GameObject SpawnDroppedWeapon(
        uint dropId,
        int weaponId,
        int magAmmo,
        int reserveAmmo,
        int skinId,
        Vector3 position,
        Vector3 direction)
    {
        if (dropId == 0)
            return null;

        if (spawnedDrops.ContainsKey(dropId))
            return null;

        WeaponData data = GetWeaponData(weaponId);

        if (data == null)
        {
            Debug.LogError($"[WEAPON DROP SPAWN] WeaponData missing. weaponId={weaponId}");
            return null;
        }

        if (data.dropPrefab == null)
        {
            Debug.LogError($"[WEAPON DROP SPAWN] dropPrefab missing. weaponId={weaponId}");
            return null;
        }

        GameObject obj = Instantiate(
            data.dropPrefab,
            position + Vector3.up * 0.35f,
            Quaternion.identity
        );

        int droppedItemLayer = LayerMask.NameToLayer("DroppedItem");

        if (droppedItemLayer != -1)
            SetLayerRecursively(obj, droppedItemLayer);

        DroppedItem droppedItem = obj.GetComponent<DroppedItem>();

        if (droppedItem == null)
            droppedItem = obj.GetComponentInChildren<DroppedItem>(true);

        if (droppedItem == null)
        {
            Debug.LogError($"[WEAPON DROP SPAWN] DroppedItem missing. prefab={data.dropPrefab.name}");
            Destroy(obj);
            return null;
        }

        droppedItem.InitFromServer(
            dropId,
            data,
            magAmmo,
            reserveAmmo,
            skinId
        );

        WeaponSkinSlot skinSlot = obj.GetComponent<WeaponSkinSlot>();
        if (skinSlot == null)
            skinSlot = obj.GetComponentInChildren<WeaponSkinSlot>(true);

        if (skinSlot != null)
        {
            WeaponSkinDatabase database = skinDatabase != null
                ? skinDatabase
                : WeaponSkinDatabase.Instance;

            WeaponSkinData skin = skinId > 0 && database != null
                ? database.GetById(weaponId, skinId)
                : null;

            if (skinId > 0 && skin == null)
            {
                Debug.LogWarning(
                    $"[WEAPON DROP SPAWN] SkinData missing. weaponId={weaponId}, skinId={skinId}"
                );
            }

            skinSlot.ApplySkin(skin);
        }

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
            rb = obj.GetComponentInChildren<Rigidbody>(true);

        // A valid drop root should already contain a Rigidbody. Keep this
        // fallback so a newly-added weapon cannot become frozen in mid-air
        // just because its drop prefab was configured without one.
        if (rb == null)
        {
            rb = obj.AddComponent<Rigidbody>();
            Debug.LogWarning(
                $"[WEAPON DROP SPAWN] Rigidbody was missing and has been added. prefab={data.dropPrefab.name}"
            );
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 throwDir = direction.normalized;
        if (throwDir.sqrMagnitude < 0.01f)
            throwDir = Vector3.forward;

        rb.AddForce(throwDir * 3f + Vector3.up * 1.5f, ForceMode.Impulse);
        rb.WakeUp();

        spawnedDrops.Add(dropId, obj);

        Debug.Log(
            $"[WEAPON DROP SPAWN] dropId={dropId}, weaponId={weaponId}, " +
            $"skinId={skinId}, ammo={magAmmo}/{reserveAmmo}"
        );

        return obj;
    }

    public bool DestroyDroppedWeapon(uint dropId)
    {
        if (!spawnedDrops.TryGetValue(dropId, out GameObject obj))
            return false;

        spawnedDrops.Remove(dropId);

        if (obj != null)
            Destroy(obj);

        Debug.Log($"[WEAPON DROP DESTROY] dropId={dropId}");
        return true;
    }

    public void ClearAllDroppedWeapons()
    {
        foreach (var pair in spawnedDrops)
        {
            if (pair.Value != null)
            {
                Destroy(pair.Value);
            }
        }

        spawnedDrops.Clear();

        Debug.Log("[WEAPON DROP] clear all dropped weapons");
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null)
            return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            if (child != null)
                SetLayerRecursively(child.gameObject, layer);
        }
    }
}
