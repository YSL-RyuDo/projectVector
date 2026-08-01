using System.Collections.Generic;
using UnityEngine;

public class WeaponDropSpawner : MonoBehaviour
{
    public static WeaponDropSpawner Instance;

    [Header("Weapon Database")]
    [SerializeField] private WeaponData[] weaponDatas;

    private readonly Dictionary<uint, GameObject> spawnedDrops = new();

    private void Awake()
    {
        Instance = this;
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

    public void SpawnDroppedWeapon(
        uint dropId,
        int weaponId,
        int magAmmo,
        int reserveAmmo,
        Vector3 position,
        Vector3 direction)
    {
        if (dropId == 0)
            return;

        if (spawnedDrops.ContainsKey(dropId))
            return;

        WeaponData data = GetWeaponData(weaponId);

        if (data == null)
        {
            Debug.LogError($"[WEAPON DROP SPAWN] WeaponData missing. weaponId={weaponId}");
            return;
        }

        if (data.dropPrefab == null)
        {
            Debug.LogError($"[WEAPON DROP SPAWN] dropPrefab missing. weaponId={weaponId}");
            return;
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
            return;
        }

        droppedItem.InitFromServer(
            dropId,
            data,
            magAmmo,
            reserveAmmo
        );

        Rigidbody rb = obj.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 throwDir = direction.normalized;
            if (throwDir.sqrMagnitude < 0.01f)
                throwDir = Vector3.forward;

            rb.AddForce(throwDir * 3f + Vector3.up * 1.5f, ForceMode.Impulse);
        }

        spawnedDrops.Add(dropId, obj);

        Debug.Log(
            $"[WEAPON DROP SPAWN] dropId={dropId}, weaponId={weaponId}, ammo={magAmmo}/{reserveAmmo}"
        );
    }

    public void DestroyDroppedWeapon(uint dropId)
    {
        if (!spawnedDrops.TryGetValue(dropId, out GameObject obj))
            return;

        spawnedDrops.Remove(dropId);

        if (obj != null)
            Destroy(obj);

        Debug.Log($"[WEAPON DROP DESTROY] dropId={dropId}");
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