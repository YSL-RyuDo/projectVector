using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class InitiatorSkill_X : CharacterSkill
{
    [Header("Ultimate Area")]
    [SerializeField] private float _range = 13f;
    [SerializeField] private float _width = 8f;

    [Header("Visual")]
    [SerializeField] private GameObject _decalPrefab;
    [SerializeField] private float _warningTime = 0.6f;
    [SerializeField] private int _boltCount = 7;
    [SerializeField] private float _boltInterval = 0.05f;
    [SerializeField] private float _boltRadius = 2.0f;

    [Header("Lightning Line")]
    [SerializeField] private float _lineStartHeight = 10f;
    [SerializeField] private float _lineDuration = 0.15f;
    [SerializeField] private float _lineStartWidth = 0.15f;
    [SerializeField] private float _lineEndWidth = 0.05f;
    [SerializeField] private int _lineSegments = 5;
    [SerializeField] private float _lineJitter = 0.5f;
    [SerializeField] private Material _lineMaterial;

    [Header("Cast")]
    [SerializeField] private float _castCooldown = 1.0f;

    private bool _isCasting;
    private PlayerIdentity _playerIdentity;

    private void Awake()
    {
        _playerIdentity = GetComponent<PlayerIdentity>();
    }

    private void Update()
    {
        if (!IsMine())
            return;

        if (_isCasting)
            return;

        if (Keyboard.current == null)
            return;

        if (InGameChatUI.IsChatOpen)
            return;

        if (GameController.Instance != null &&
            GameController.Instance.IsPaused)
            return;

        if (SettingsMenu.Instance != null &&
            SettingsMenu.Instance.IsOpen)
            return;

        if (InitiatorSkill_Q.IsLocalDroneMode)
            return;

        if (InitiatorSkill_C.ShouldBlockWeaponInput())
            return;

        ElectricShock shock = GetComponent<ElectricShock>();

        if (shock == null)
            shock = GetComponentInChildren<ElectricShock>(true);

        if (shock != null && shock.IsStunned)
            return;

        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            UseSkill();
        }
    }

    private bool IsMine()
    {
        return _playerIdentity != null &&
               NetworkManager.Instance != null &&
               _playerIdentity.PlayerId == NetworkManager.Instance.MyPlayerId;
    }

    protected override void UseSkill()
    {
        if (!CanUsePurchasedSkill(SkillSlot.X))
            return;

        if (NetworkManager.Instance == null)
            return;

        Vector3 direction = transform.forward;

        Camera cam = Camera.main;

        if (cam != null)
            direction = cam.transform.forward;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = transform.forward;

        direction.Normalize();

        NetworkManager.Instance.SendUltimateCast(direction);

        _isCasting = true;

        StartCoroutine(CastCooldownRoutine());

        Debug.Log($"[ULTIMATE CAST REQUEST] dir={direction}");
    }

    private IEnumerator CastCooldownRoutine()
    {
        yield return new WaitForSeconds(_castCooldown);
        _isCasting = false;
    }

    public void PlayUltimateEffect(
        uint[] targetPlayerIds,
        Vector3 direction)
    {
        StopCoroutine(nameof(UltimateEffectRoutine));

        StartCoroutine(
            UltimateEffectRoutine(
                targetPlayerIds,
                direction
            )
        );
    }

    private IEnumerator UltimateEffectRoutine(
        uint[] targetPlayerIds,
        Vector3 direction)
    {
        Vector3 dir = direction;

        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            dir = transform.forward;

        dir.Normalize();

        Quaternion areaRotation =
            Quaternion.LookRotation(dir, Vector3.up);

        Vector3 spawnPos =
            transform.position +
            dir * (_range / 2f);

        spawnPos.y = 1f;

        GameObject decalObj = null;

        if (_decalPrefab != null)
        {
            decalObj = Instantiate(
                _decalPrefab,
                spawnPos,
                areaRotation
            );

            if (decalObj.TryGetComponent<DecalProjector>(out DecalProjector projector))
            {
                projector.size =
                    new Vector3(
                        _width,
                        _range,
                        10f
                    );
            }

            decalObj.transform.rotation =
                Quaternion.Euler(
                    90f,
                    areaRotation.eulerAngles.y,
                    0f
                );
        }

        yield return new WaitForSeconds(_warningTime);

        if (decalObj != null)
            Destroy(decalObj);

        HashSet<uint> hitTargetSet =
            new HashSet<uint>();

        if (targetPlayerIds != null)
        {
            for (int i = 0; i < targetPlayerIds.Length; i++)
            {
                if (targetPlayerIds[i] == 0)
                    continue;

                if (hitTargetSet.Contains(targetPlayerIds[i]))
                    continue;

                hitTargetSet.Add(targetPlayerIds[i]);

                PlayerIdentity target =
                    FindPlayerIdentityById(targetPlayerIds[i]);

                if (target == null)
                    continue;

                Vector3 targetPos =
                    target.transform.position;

                targetPos.y = 0f;

                StartCoroutine(
                    DrawLightning(targetPos)
                );
            }
        }

        int randomBoltCount =
            Mathf.Max(0, _boltCount);

        for (int i = 0; i < randomBoltCount; i++)
        {
            Vector3 boltPos =
                transform.position +
                dir * Random.Range(1f, _range) +
                transform.right * Random.Range(
                    -_width / 2f,
                    _width / 2f
                );

            boltPos.y = 0f;

            StartCoroutine(
                DrawLightning(boltPos)
            );

            yield return new WaitForSeconds(_boltInterval);
        }
    }

    private IEnumerator DrawLightning(Vector3 targetPos)
    {
        GameObject lineObj =
            new GameObject("UltimateLightningLine");

        LineRenderer lr =
            lineObj.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.startWidth = _lineStartWidth;
        lr.endWidth = _lineEndWidth;

        if (_lineMaterial != null)
        {
            lr.material = _lineMaterial;
        }
        else
        {
            lr.material =
                new Material(
                    Shader.Find("Sprites/Default")
                );
        }

        lr.startColor = Color.white;
        lr.endColor = Color.cyan;

        int segments =
            Mathf.Max(2, _lineSegments);

        lr.positionCount = segments;

        Vector3 startPos =
            targetPos +
            Vector3.up * _lineStartHeight;

        for (int i = 0; i < segments; i++)
        {
            float t =
                i / (float)(segments - 1);

            Vector3 pos =
                Vector3.Lerp(
                    startPos,
                    targetPos,
                    t
                );

            if (i > 0 && i < segments - 1)
            {
                pos +=
                    new Vector3(
                        Random.Range(-_lineJitter, _lineJitter),
                        0f,
                        Random.Range(-_lineJitter, _lineJitter)
                    );
            }

            lr.SetPosition(i, pos);
        }

        yield return new WaitForSeconds(_lineDuration);

        Destroy(lineObj);
    }

    private PlayerIdentity FindPlayerIdentityById(uint playerId)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        for (int i = 0; i < identities.Length; i++)
        {
            if (identities[i] == null)
                continue;

            if (identities[i].PlayerId == playerId)
                return identities[i];
        }

        return null;
    }
}