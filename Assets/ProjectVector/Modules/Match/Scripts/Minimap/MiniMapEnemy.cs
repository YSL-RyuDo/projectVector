using System.Collections;
using UnityEngine;

public class MiniMapEnemy : MonoBehaviour
{
    [SerializeField] private Collider _enemyCollider;

    [Tooltip("머리나 가슴 위치를 지정하면 시야 판정이 더 정확해집니다.")]
    [SerializeField] private Transform _markerPoint;

    private PlayerIdentity _identity;
    private PlayerDeathController _deathController;
    private PlayerHP _playerHp;

    private Coroutine registerCoroutine;

    public Collider EnemyCollider =>
        _enemyCollider;

    public PlayerIdentity Identity =>
        _identity;

    public Vector3 MarkerWorldPosition
    {
        get
        {
            if (_markerPoint != null)
                return _markerPoint.position;

            if (_enemyCollider != null)
                return _enemyCollider.bounds.center;

            return transform.position +
                   Vector3.up * 1.2f;
        }
    }

    public bool IsDead
    {
        get
        {
            if (_deathController != null &&
                _deathController.IsDead)
            {
                return true;
            }

            if (_playerHp != null &&
                _playerHp.IsDead)
            {
                return true;
            }

            return false;
        }
    }

    private void Awake()
    {
        _identity =
            GetComponent<PlayerIdentity>();

        if (_identity == null)
        {
            _identity =
                GetComponentInParent<PlayerIdentity>();
        }

        if (_identity == null)
        {
            _identity =
                GetComponentInChildren<PlayerIdentity>(true);
        }

        if (_enemyCollider == null)
        {
            _enemyCollider =
                GetComponent<Collider>();
        }

        if (_enemyCollider == null)
        {
            _enemyCollider =
                GetComponentInChildren<Collider>(true);
        }

        _deathController =
            GetComponent<PlayerDeathController>();

        if (_deathController == null)
        {
            _deathController =
                GetComponentInParent<PlayerDeathController>();
        }

        _playerHp =
            GetComponent<PlayerHP>();

        if (_playerHp == null)
        {
            _playerHp =
                GetComponentInParent<PlayerHP>();
        }
    }

    private void OnEnable()
    {
        if (registerCoroutine != null)
            StopCoroutine(registerCoroutine);

        registerCoroutine =
            StartCoroutine(RegisterToManagerCo());
    }

    private IEnumerator RegisterToManagerCo()
    {
        while (MiniMapManager.Instance == null)
            yield return null;

        MiniMapManager.Instance.RegisterPlayer(this);

        registerCoroutine = null;

        Debug.Log(
            $"[MINIMAP PLAYER] 등록 성공. " +
            $"object={gameObject.name}, " +
            $"playerId={_identity?.PlayerId}, " +
            $"team={_identity?.Team}, " +
            $"characterId={_identity?.CharacterId}"
        );
    }

    private void OnDisable()
    {
        if (registerCoroutine != null)
        {
            StopCoroutine(registerCoroutine);
            registerCoroutine = null;
        }

        if (MiniMapManager.Instance != null)
        {
            MiniMapManager.Instance.UnregisterPlayer(this);
        }
    }
}