using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ControllerSwampNetworkManager : MonoBehaviour
{
    public static ControllerSwampNetworkManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private GameObject _areaPrefab;

    [Header("Area")]
    [SerializeField] private float _slowMultiplier = 0.2f;

    [Header("Floor Decal")]
    [SerializeField] private float _floorDecalDepth = 0.25f;
    [SerializeField] private float _floorOffset = 0.04f;

    [Header("Wall Decal")]
    [SerializeField] private bool _createWallDecals = true;
    [SerializeField] private int _wallRayCount = 16;
    [SerializeField] private float _wallRayHeight = 0.6f;
    [SerializeField] private float _wallDecalWidth = 2.5f;
    [SerializeField] private float _wallDecalHeight = 1.7f;
    [SerializeField] private float _wallDecalDepth = 0.25f;
    [SerializeField] private float _wallOffset = 0.05f;
    [SerializeField] private LayerMask _wallMask = ~0;

    private readonly Dictionary<uint, GameObject> _trapMap = new Dictionary<uint, GameObject>();
    private readonly Dictionary<uint, GameObject> _areaMap = new Dictionary<uint, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void OnSwampSpawn(uint trapId, uint ownerPlayerId, Vector3 position, Vector3 velocity)
    {
        if (_trapPrefab == null)
        {
            Debug.LogError("[SWAMP NET] Trap Prefab is null");
            return;
        }

        if (_trapMap.TryGetValue(trapId, out GameObject oldTrap))
        {
            if (oldTrap != null) Destroy(oldTrap);
            _trapMap.Remove(trapId);
        }

        Quaternion rot = velocity.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(velocity.normalized, Vector3.up) : Quaternion.identity;
        GameObject trap = Instantiate(_trapPrefab, position, rot);
        trap.SetActive(true);

        SwampTrap swampTrap = trap.GetComponent<SwampTrap>();
        if (swampTrap == null) swampTrap = trap.GetComponentInChildren<SwampTrap>(true);

        if (swampTrap != null)
        {
            bool isMine = NetworkManager.Instance != null && ownerPlayerId == NetworkManager.Instance.MyPlayerId;
            swampTrap.InitFromServer(trapId, ownerPlayerId, isMine);
        }

        Rigidbody rb = trap.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = velocity;
            rb.angularVelocity = new Vector3(3, 5, 1);
        }

        IgnoreOwnerCollision(trap, ownerPlayerId);
        _trapMap[trapId] = trap;

        Debug.Log($"[SWAMP NET SPAWN] trapId={trapId}, owner={ownerPlayerId}, pos={position}, vel={velocity}");
    }

    public void OnSwampActivate(uint trapId, uint ownerPlayerId, Vector3 position, Vector3 normal, float radius, float duration)
    {
        RemoveTrapOnly(trapId);

        if (_areaPrefab == null)
        {
            Debug.LogError("[SWAMP NET] Area Prefab is null");
            return;
        }

        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.up;

        normal.Normalize();

        if (normal.y < 0.45f)
        {
            Debug.LogWarning($"[SWAMP NET ACTIVATE BLOCK] wall normal={normal}, pos={position}");
            return;
        }

        Vector3 spawnPos = position + Vector3.up * 0.03f;
        GameObject area = Instantiate(_areaPrefab, spawnPos, Quaternion.identity);
        area.SetActive(true);

        if (_areaMap.TryGetValue(trapId, out GameObject oldArea))
        {
            if (oldArea != null)
                Destroy(oldArea);

            _areaMap.Remove(trapId);
        }

        _areaMap[trapId] = area;

        DecalProjector projector = area.GetComponentInChildren<DecalProjector>(true);
        if (projector != null)
            projector.gameObject.SetActive(false);

        BoxCollider box = area.GetComponent<BoxCollider>();
        if (box != null)
            box.enabled = false;

        SphereCollider sphere = area.GetComponent<SphereCollider>();
        if (sphere == null)
            sphere = area.GetComponentInChildren<SphereCollider>(true);

        if (sphere != null)
        {
            sphere.enabled = true;
            sphere.isTrigger = true;
            sphere.center = Vector3.zero;
            sphere.radius = radius;
        }

        SwampGroundMeshVisual visual = area.GetComponent<SwampGroundMeshVisual>();
        if (visual == null)
            visual = area.GetComponentInChildren<SwampGroundMeshVisual>(true);

        if (visual != null)
        {
            Renderer renderer = visual.GetComponent<Renderer>();
            Material mat = renderer != null ? renderer.sharedMaterial : null;
            visual.Init(radius, mat, ~0);
        }

        SwampArea swampArea = area.GetComponent<SwampArea>();
        if (swampArea == null)
            swampArea = area.GetComponentInChildren<SwampArea>(true);

        if (swampArea != null)
            swampArea.InitFromServer(ownerPlayerId, _slowMultiplier);

        area.GetComponent<ControllerSwampVfx>()?.Begin(duration);

        Destroy(area, duration);

        Debug.Log($"[SWAMP NET ACTIVATE] trapId={trapId}, owner={ownerPlayerId}, pos={spawnPos}, radius={radius}, duration={duration}");
    }

    public void OnSwampDestroy(uint trapId)
    {
        RemoveTrapOnly(trapId);

        if (_areaMap.TryGetValue(trapId, out GameObject area))
        {
            if (area != null) Destroy(area);
            _areaMap.Remove(trapId);
        }

        Debug.Log($"[SWAMP NET DESTROY] trapId={trapId}");
    }

    private void SetupFloorDecal(GameObject area, float radius)
    {
        DecalProjector projector = area.GetComponent<DecalProjector>();
        if (projector == null) projector = area.GetComponentInChildren<DecalProjector>(true);

        if (projector == null) return;

        projector.gameObject.SetActive(true);
        projector.size = new Vector3(radius * 2f, radius * 2f, _floorDecalDepth);
        projector.pivot = Vector3.zero;
        projector.drawDistance = 1000f;
        projector.fadeFactor = 1f;
    }

    private void SetupSlowCollider(GameObject area, float radius)
    {
        SphereCollider sphere = area.GetComponent<SphereCollider>();
        if (sphere == null) sphere = area.GetComponentInChildren<SphereCollider>(true);

        if (sphere != null)
        {
            sphere.enabled = true;
            sphere.isTrigger = true;
            sphere.center = Vector3.zero;
            sphere.radius = radius;
        }

        BoxCollider box = area.GetComponent<BoxCollider>();
        if (box != null) box.enabled = false;
    }

    private void SetupSwampArea(GameObject area, uint ownerPlayerId)
    {
        SwampArea swampArea = area.GetComponent<SwampArea>();
        if (swampArea == null) swampArea = area.GetComponentInChildren<SwampArea>(true);

        if (swampArea != null)
        {
            swampArea.enabled = true;
            swampArea.InitFromServer(ownerPlayerId, _slowMultiplier);
        }
    }

    private void CreateWallDecals(Transform parent, Vector3 center, float radius)
    {
        DecalProjector sourceProjector = parent.GetComponentInChildren<DecalProjector>(true);
        if (sourceProjector == null || sourceProjector.material == null) return;

        List<Vector3> usedNormals = new List<Vector3>();
        Vector3 rayOriginBase = center + Vector3.up * _wallRayHeight;

        for (int i = 0; i < _wallRayCount; i++)
        {
            float angle = 360f * i / _wallRayCount;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

            if (!Physics.Raycast(rayOriginBase, dir, out RaycastHit hit, radius, _wallMask, QueryTriggerInteraction.Ignore))
                continue;

            if (hit.normal.y > 0.35f)
                continue;

            bool duplicated = false;
            for (int n = 0; n < usedNormals.Count; n++)
            {
                if (Vector3.Dot(usedNormals[n], hit.normal) > 0.92f)
                {
                    duplicated = true;
                    break;
                }
            }

            if (duplicated)
                continue;

            usedNormals.Add(hit.normal);
            CreateOneWallDecal(parent, hit.point, hit.normal, sourceProjector.material);
        }
    }

    private void CreateOneWallDecal(Transform parent, Vector3 hitPoint, Vector3 normal, Material material)
    {
        GameObject wallObj = new GameObject("SwampWallDecal");
        wallObj.transform.SetParent(parent, true);
        wallObj.transform.position = hitPoint + normal.normalized * _wallOffset;
        wallObj.transform.rotation = MakeDecalRotation(normal);

        DecalProjector wallProjector = wallObj.AddComponent<DecalProjector>();
        wallProjector.material = material;
        wallProjector.size = new Vector3(_wallDecalWidth, _wallDecalHeight, _wallDecalDepth);
        wallProjector.pivot = Vector3.zero;
        wallProjector.drawDistance = 1000f;
        wallProjector.fadeFactor = 1f;
    }

    private Quaternion MakeDecalRotation(Vector3 surfaceNormal)
    {
        if (surfaceNormal.sqrMagnitude <= 0.0001f)
            surfaceNormal = Vector3.up;

        surfaceNormal.Normalize();
        return Quaternion.FromToRotation(Vector3.up, surfaceNormal) * Quaternion.Euler(90f, 0f, 0f);
    }

    private IEnumerator DestroyAreaLater(uint trapId, GameObject area, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (_areaMap.TryGetValue(trapId, out GameObject current) && current == area)
            _areaMap.Remove(trapId);

        if (area != null)
            Destroy(area);
    }

    private void ForceActiveRecursive(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(true);

        Transform[] children = obj.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
            children[i].gameObject.SetActive(true);
    }

    private void RemoveTrapOnly(uint trapId)
    {
        if (!_trapMap.TryGetValue(trapId, out GameObject trap)) return;

        if (trap != null)
        {
            trap.SetActive(false);
            Destroy(trap);
        }

        _trapMap.Remove(trapId);
    }

    private void IgnoreOwnerCollision(GameObject trap, uint ownerPlayerId)
    {
        if (trap == null) return;

        PlayerIdentity owner = NetworkManager.Instance != null ? NetworkManager.Instance.MyPlayer : null;
        if (owner == null) return;
        if (owner.PlayerId != ownerPlayerId) return;

        Collider ownerCollider = owner.GetComponent<Collider>();
        Collider trapCollider = trap.GetComponent<Collider>();
        if (trapCollider == null) trapCollider = trap.GetComponentInChildren<Collider>();

        if (ownerCollider != null && trapCollider != null)
            Physics.IgnoreCollision(ownerCollider, trapCollider, true);
    }
}
