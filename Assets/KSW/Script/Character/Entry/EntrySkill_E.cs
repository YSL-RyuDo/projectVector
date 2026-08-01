using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class EntrySkill_E : CharacterSkill
{
    [Header("Remote Visual")]
    [SerializeField] private GameObject _characterModel;

    [Header("Local First Person Visual")]
    [SerializeField] private GameObject _localFirstPersonRoot;
    [SerializeField] private float _selfFirstPersonAlpha = 0.35f;

    [Header("Remote Alpha")]
    [SerializeField] private float _allyAlpha = 0.2f;
    [SerializeField] private float _enemyAlpha = 0.0f;
    [SerializeField] private float _revealedEnemyAlpha = 1.0f;

    [Header("Debug")]
    [SerializeField] private bool _debugStealthTeam = false;

    private PlayerMovement _movement;
    private PlayerIdentity _playerIdentity;

    private Renderer[] _remoteRenderers;
    private Material[][] _remoteOriginalMaterials;
    private bool[] _remoteOriginalEnabled;
    private Material[][] _remoteRuntimeMaterials;

    private Renderer[] _selfRenderers;
    private Material[][] _selfOriginalMaterials;
    private bool[] _selfOriginalEnabled;
    private Material[][] _selfRuntimeMaterials;

    private bool _isStealth;
    private float _serverSpeedMultiplier = 1.0f;
    private float _serverRevealRange = 3.0f;

    private Coroutine _localTimeoutCoroutine;

    private float _lastRemoteAlpha = -999f;
    private float _lastSelfAlpha = -999f;

    public bool IsStealth => _isStealth;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _playerIdentity = GetComponent<PlayerIdentity>();

        ResolveRoots();

        CacheRemoteRenderers();
        CacheSelfRenderers();

        Debug.Log(
            $"[BEAST STEALTH CACHE] object={gameObject.name}, " +
            $"remoteRenderers={(_remoteRenderers == null ? 0 : _remoteRenderers.Length)}, " +
            $"selfRenderers={(_selfRenderers == null ? 0 : _selfRenderers.Length)}, " +
            $"localRoot={(_localFirstPersonRoot == null ? "null" : _localFirstPersonRoot.name)}"
        );
    }

    private void Update()
    {
        if (_isStealth)
        {
            if (IsMine())
            {
                RefreshSelfVisual();
            }
            else
            {
                RefreshRemoteVisual();
            }
        }

        if (!IsMine())
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

        if (!_isStealth &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            UseSkill();
            return;
        }

        if (_isStealth &&
            IsLocalCancelActionPressed())
        {
            RequestCancelStealth();
        }
    }

    private bool IsMine()
    {
        return _playerIdentity != null &&
               NetworkManager.Instance != null &&
               _playerIdentity.PlayerId ==
               NetworkManager.Instance.MyPlayerId;
    }

    private bool IsLocalCancelActionPressed()
    {
        bool attackPressed =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        bool qPressed =
            Keyboard.current != null &&
            Keyboard.current.qKey.wasPressedThisFrame;

        bool cPressed =
            Keyboard.current != null &&
            Keyboard.current.cKey.wasPressedThisFrame;

        bool xPressed =
            Keyboard.current != null &&
            Keyboard.current.xKey.wasPressedThisFrame;

        return attackPressed ||
               qPressed ||
               cPressed ||
               xPressed;
    }

    protected override void UseSkill()
    {
        if (!CanUsePurchasedSkill(SkillSlot.E))
            return;

        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance.SendBeastStealthCast();

        Debug.Log("[BEAST STEALTH CAST REQUEST]");
    }

    private void RequestCancelStealth()
    {
        if (NetworkManager.Instance == null)
            return;

        NetworkManager.Instance.SendBeastStealthCancel();

        Debug.Log("[BEAST STEALTH CANCEL REQUEST]");
    }

    public void ApplyStealthStateFromServer(
        bool active,
        float duration,
        float speedMultiplier,
        float revealRange)
    {
        if (active)
        {
            StartStealthFromServer(
                duration,
                speedMultiplier,
                revealRange
            );
        }
        else
        {
            EndStealthFromServer();
        }
    }

    private void StartStealthFromServer(
        float duration,
        float speedMultiplier,
        float revealRange)
    {
        _isStealth = true;
        _serverSpeedMultiplier = speedMultiplier;
        _serverRevealRange = revealRange;

        _lastRemoteAlpha = -999f;
        _lastSelfAlpha = -999f;

        ResolveRoots();

        if (IsMine())
        {
            CacheSelfRenderers();

            ApplySelfAlpha(
                _selfFirstPersonAlpha
            );

            if (_movement != null)
            {
                _movement.SetExternalSpeedMultiplier(
                    _serverSpeedMultiplier
                );
            }

            Debug.Log(
                $"[BEAST STEALTH START SELF] object={gameObject.name}, " +
                $"selfAlpha={_selfFirstPersonAlpha}, speed={speedMultiplier}"
            );
        }
        else
        {
            CacheRemoteRenderers();
            RefreshRemoteVisual();

            Debug.Log(
                $"[BEAST STEALTH START REMOTE] object={gameObject.name}, " +
                $"playerId={(_playerIdentity == null ? 0 : _playerIdentity.PlayerId)}, " +
                $"team={(_playerIdentity == null ? 255 : _playerIdentity.Team)}"
            );
        }

        StartLocalTimeout(duration);
    }

    private void StartLocalTimeout(float duration)
    {
        if (_localTimeoutCoroutine != null)
            StopCoroutine(_localTimeoutCoroutine);

        _localTimeoutCoroutine =
            StartCoroutine(LocalTimeoutRoutine(duration));
    }

    private IEnumerator LocalTimeoutRoutine(float duration)
    {
        if (duration <= 0f)
            yield break;

        yield return new WaitForSeconds(duration + 0.2f);

        if (_isStealth)
        {
            EndStealthFromServer();
        }
    }

    private void EndStealthFromServer()
    {
        if (!_isStealth)
            return;

        _isStealth = false;

        if (_localTimeoutCoroutine != null)
        {
            StopCoroutine(_localTimeoutCoroutine);
            _localTimeoutCoroutine = null;
        }

        if (IsMine())
        {
            RestoreSelfVisual();

            if (_movement != null)
            {
                _movement.SetExternalSpeedMultiplier(1.0f);
            }

            Debug.Log(
                $"[BEAST STEALTH END SELF] object={gameObject.name}"
            );
        }
        else
        {
            RestoreRemoteVisual();

            Debug.Log(
                $"[BEAST STEALTH END REMOTE] object={gameObject.name}"
            );
        }

        _lastRemoteAlpha = -999f;
        _lastSelfAlpha = -999f;
    }

    private void ResolveRoots()
    {
        if (_characterModel == null ||
            _characterModel == gameObject)
        {
            Transform model =
                transform.Find("Model");

            if (model != null)
                _characterModel = model.gameObject;
        }

        if (_localFirstPersonRoot == null)
        {
            Transform cameraRoot =
                transform.Find("CameraRoot");

            if (cameraRoot != null)
                _localFirstPersonRoot = cameraRoot.gameObject;
        }

        if (_localFirstPersonRoot == null)
        {
            Transform found =
                FindChildByName(
                    transform,
                    "WeaponHolder"
                );

            if (found != null)
                _localFirstPersonRoot = found.gameObject;
        }
    }

    private Transform FindChildByName(
        Transform root,
        string targetName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (child.name == targetName)
                return child;

            Transform found =
                FindChildByName(child, targetName);

            if (found != null)
                return found;
        }

        return null;
    }

    private void CacheRemoteRenderers()
    {
        ClearRemoteRuntimeMaterials();

        List<Renderer> list =
            new List<Renderer>();

        Renderer[] all =
            GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < all.Length; i++)
        {
            Renderer renderer = all[i];

            if (renderer == null)
                continue;

            if (IsUnderLocalFirstPersonRoot(renderer.transform))
                continue;

            list.Add(renderer);
        }

        _remoteRenderers = list.ToArray();
        _remoteOriginalMaterials =
            new Material[_remoteRenderers.Length][];

        _remoteOriginalEnabled =
            new bool[_remoteRenderers.Length];

        _remoteRuntimeMaterials =
            new Material[_remoteRenderers.Length][];

        for (int i = 0; i < _remoteRenderers.Length; i++)
        {
            if (_remoteRenderers[i] == null)
                continue;

            _remoteOriginalMaterials[i] =
                _remoteRenderers[i].sharedMaterials;

            _remoteOriginalEnabled[i] =
                _remoteRenderers[i].enabled;
        }
    }

    private void CacheSelfRenderers()
    {
        ClearSelfRuntimeMaterials();

        if (_localFirstPersonRoot == null)
        {
            _selfRenderers = new Renderer[0];
            return;
        }

        _selfRenderers =
            _localFirstPersonRoot.GetComponentsInChildren<Renderer>(true);

        _selfOriginalMaterials =
            new Material[_selfRenderers.Length][];

        _selfOriginalEnabled =
            new bool[_selfRenderers.Length];

        _selfRuntimeMaterials =
            new Material[_selfRenderers.Length][];

        for (int i = 0; i < _selfRenderers.Length; i++)
        {
            if (_selfRenderers[i] == null)
                continue;

            _selfOriginalMaterials[i] =
                _selfRenderers[i].sharedMaterials;

            _selfOriginalEnabled[i] =
                _selfRenderers[i].enabled;
        }
    }

    private bool IsUnderLocalFirstPersonRoot(
        Transform target)
    {
        if (_localFirstPersonRoot == null ||
            target == null)
        {
            return false;
        }

        return target == _localFirstPersonRoot.transform ||
               target.IsChildOf(_localFirstPersonRoot.transform);
    }

    private void RefreshSelfVisual()
    {
        if (Mathf.Approximately(
            _selfFirstPersonAlpha,
            _lastSelfAlpha))
        {
            return;
        }

        _lastSelfAlpha =
            _selfFirstPersonAlpha;

        ApplySelfAlpha(
            _selfFirstPersonAlpha
        );
    }

    private void RefreshRemoteVisual()
    {
        float alpha =
            CalculateRemoteAlphaForThisClient();

        if (Mathf.Approximately(alpha, _lastRemoteAlpha))
            return;

        _lastRemoteAlpha = alpha;

        ApplyRemoteAlpha(alpha);
    }

    private float CalculateRemoteAlphaForThisClient()
    {
        if (!_isStealth)
            return 1.0f;

        if (_playerIdentity == null)
            return _allyAlpha;

        if (NetworkManager.Instance == null)
            return _allyAlpha;

        PlayerIdentity myIdentity =
            NetworkManager.Instance.MyPlayer;

        if (myIdentity == null)
            return _allyAlpha;

        if (myIdentity.PlayerId == _playerIdentity.PlayerId)
            return 1.0f;

        bool sameTeam =
            myIdentity.Team == _playerIdentity.Team;

        if (sameTeam)
        {
            if (_debugStealthTeam)
            {
                Debug.Log(
                    $"[BEAST STEALTH ALLY] viewer={myIdentity.PlayerId}, " +
                    $"viewerTeam={myIdentity.Team}, target={_playerIdentity.PlayerId}, " +
                    $"targetTeam={_playerIdentity.Team}, alpha={_allyAlpha}"
                );
            }

            return _allyAlpha;
        }

        float revealRangeSq =
            _serverRevealRange *
            _serverRevealRange;

        float distSq =
            (
                myIdentity.transform.position -
                transform.position
            ).sqrMagnitude;

        if (distSq <= revealRangeSq)
        {
            if (_debugStealthTeam)
            {
                Debug.Log(
                    $"[BEAST STEALTH ENEMY REVEALED] viewer={myIdentity.PlayerId}, " +
                    $"target={_playerIdentity.PlayerId}, alpha={_revealedEnemyAlpha}"
                );
            }

            return _revealedEnemyAlpha;
        }

        if (_debugStealthTeam)
        {
            Debug.Log(
                $"[BEAST STEALTH ENEMY HIDDEN] viewer={myIdentity.PlayerId}, " +
                $"target={_playerIdentity.PlayerId}, alpha={_enemyAlpha}"
            );
        }

        return _enemyAlpha;
    }

    private void ApplySelfAlpha(float alpha)
    {
        ApplyAlphaToRenderers(
            _selfRenderers,
            _selfOriginalMaterials,
            _selfOriginalEnabled,
            ref _selfRuntimeMaterials,
            alpha,
            false
        );
    }

    private void ApplyRemoteAlpha(float alpha)
    {
        bool hideCompletely =
            alpha <= 0.01f;

        ApplyAlphaToRenderers(
            _remoteRenderers,
            _remoteOriginalMaterials,
            _remoteOriginalEnabled,
            ref _remoteRuntimeMaterials,
            alpha,
            hideCompletely
        );
    }

    private void ApplyAlphaToRenderers(
        Renderer[] renderers,
        Material[][] originalMaterials,
        bool[] originalEnabled,
        ref Material[][] runtimeMaterials,
        float alpha,
        bool hideCompletely)
    {
        if (renderers == null)
            return;

        alpha = Mathf.Clamp01(alpha);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            bool wasEnabled = true;

            if (originalEnabled != null &&
                i < originalEnabled.Length)
            {
                wasEnabled = originalEnabled[i];
            }

            if (!wasEnabled)
            {
                renderer.enabled = false;
                continue;
            }

            if (hideCompletely)
            {
                renderer.enabled = false;
                continue;
            }

            renderer.enabled = true;

            Material[] source =
                null;

            if (originalMaterials != null &&
                i < originalMaterials.Length)
            {
                source = originalMaterials[i];
            }

            if (source == null)
                source = renderer.sharedMaterials;

            Material[] runtime =
                CreateRuntimeMaterialsIfNeeded(
                    i,
                    source,
                    ref runtimeMaterials
                );

            for (int j = 0; j < runtime.Length; j++)
            {
                if (runtime[j] == null)
                    continue;

                SetMaterialTransparent(runtime[j]);
                SetMaterialAlpha(runtime[j], alpha);
            }

            renderer.materials = runtime;
        }
    }

    private Material[] CreateRuntimeMaterialsIfNeeded(
    int index,
    Material[] sourceMaterials,
    ref Material[][] runtimeMaterials)
    {
        if (runtimeMaterials != null &&
            index < runtimeMaterials.Length &&
            runtimeMaterials[index] != null &&
            runtimeMaterials[index].Length == sourceMaterials.Length)
        {
            return runtimeMaterials[index];
        }

        Material[] runtime =
            new Material[sourceMaterials.Length];

        for (int i = 0; i < sourceMaterials.Length; i++)
        {
            if (sourceMaterials[i] == null)
                continue;

            runtime[i] =
                CreateForcedTransparentMaterial(
                    sourceMaterials[i]
                );
        }

        if (runtimeMaterials != null &&
            index < runtimeMaterials.Length)
        {
            runtimeMaterials[index] = runtime;
        }

        return runtime;
    }

    private Material CreateForcedTransparentMaterial(
    Material source)
    {
        Shader urpLit =
            Shader.Find("Universal Render Pipeline/Lit");

        Material mat;

        if (urpLit != null)
        {
            mat =
                new Material(urpLit);
        }
        else
        {
            mat =
                new Material(source);
        }

        CopyTextureIfPossible(
            source,
            mat
        );

        CopyColorIfPossible(
            source,
            mat
        );

        SetMaterialTransparent(mat);

        return mat;
    }

    private void CopyTextureIfPossible(
    Material source,
    Material target)
    {
        if (source == null ||
            target == null)
        {
            return;
        }

        Texture texture = null;

        if (source.HasProperty("_BaseMap"))
        {
            texture =
                source.GetTexture("_BaseMap");
        }
        else if (source.HasProperty("_MainTex"))
        {
            texture =
                source.GetTexture("_MainTex");
        }

        if (texture == null)
            return;

        if (target.HasProperty("_BaseMap"))
        {
            target.SetTexture(
                "_BaseMap",
                texture
            );
        }

        if (target.HasProperty("_MainTex"))
        {
            target.SetTexture(
                "_MainTex",
                texture
            );
        }
    }

    private void CopyColorIfPossible(
        Material source,
        Material target)
    {
        if (source == null ||
            target == null)
        {
            return;
        }

        Color color = Color.white;

        if (source.HasProperty("_BaseColor"))
        {
            color =
                source.GetColor("_BaseColor");
        }
        else if (source.HasProperty("_Color"))
        {
            color =
                source.color;
        }

        if (target.HasProperty("_BaseColor"))
        {
            target.SetColor(
                "_BaseColor",
                color
            );
        }

        if (target.HasProperty("_Color"))
        {
            target.color =
                color;
        }
    }

    private void SetMaterialTransparent(Material mat)
    {
        if (mat == null)
            return;

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);

        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);

        if (mat.HasProperty("_AlphaClip"))
            mat.SetFloat("_AlphaClip", 0f);

        if (mat.HasProperty("_ZWrite"))
            mat.SetFloat("_ZWrite", 0f);

        mat.SetInt(
            "_SrcBlend",
            (int)UnityEngine.Rendering.BlendMode.SrcAlpha
        );

        mat.SetInt(
            "_DstBlend",
            (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha
        );

        mat.SetInt("_ZWrite", 0);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        mat.renderQueue =
            (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
    private void SetMaterialAlpha(
    Material mat,
    float alpha)
    {
        if (mat == null)
            return;

        alpha =
            Mathf.Clamp01(alpha);

        if (mat.HasProperty("_BaseColor"))
        {
            Color baseColor =
                mat.GetColor("_BaseColor");

            baseColor.a = alpha;

            mat.SetColor(
                "_BaseColor",
                baseColor
            );
        }

        if (mat.HasProperty("_Color"))
        {
            Color color =
                mat.color;

            color.a = alpha;

            mat.color =
                color;
        }
    }

    private void RestoreSelfVisual()
    {
        RestoreRenderers(
            _selfRenderers,
            _selfOriginalMaterials,
            _selfOriginalEnabled
        );

        ClearSelfRuntimeMaterials();
    }

    private void RestoreRemoteVisual()
    {
        RestoreRenderers(
            _remoteRenderers,
            _remoteOriginalMaterials,
            _remoteOriginalEnabled
        );

        ClearRemoteRuntimeMaterials();
    }

    private void RestoreRenderers(
        Renderer[] renderers,
        Material[][] originalMaterials,
        bool[] originalEnabled)
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];

            if (renderer == null)
                continue;

            bool wasEnabled = true;

            if (originalEnabled != null &&
                i < originalEnabled.Length)
            {
                wasEnabled = originalEnabled[i];
            }

            renderer.enabled = wasEnabled;

            if (originalMaterials != null &&
                i < originalMaterials.Length &&
                originalMaterials[i] != null)
            {
                renderer.sharedMaterials =
                    originalMaterials[i];
            }
        }
    }

    private void ClearSelfRuntimeMaterials()
    {
        ClearRuntimeMaterials(ref _selfRuntimeMaterials);
    }

    private void ClearRemoteRuntimeMaterials()
    {
        ClearRuntimeMaterials(ref _remoteRuntimeMaterials);
    }

    private void ClearRuntimeMaterials(
        ref Material[][] runtimeMaterials)
    {
        if (runtimeMaterials == null)
            return;

        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            Material[] materials =
                runtimeMaterials[i];

            if (materials == null)
                continue;

            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j] != null)
                    Destroy(materials[j]);
            }
        }

        runtimeMaterials = null;
    }

    private void OnDisable()
    {
        if (!_isStealth)
            return;

        _isStealth = false;

        if (IsMine())
        {
            RestoreSelfVisual();

            if (_movement != null)
                _movement.SetExternalSpeedMultiplier(1.0f);
        }
        else
        {
            RestoreRemoteVisual();
        }
    }

    private void OnDestroy()
    {
        ClearSelfRuntimeMaterials();
        ClearRemoteRuntimeMaterials();
    }
}