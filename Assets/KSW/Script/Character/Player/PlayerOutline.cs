using UnityEngine;

[DisallowMultipleComponent]
public class PlayerOutline : MonoBehaviour
{
    [Header("아군 외곽선")]
    [SerializeField]
    private Color allyColor =
        new Color(0.18f, 0.52f, 0.78f, 1f);

    [SerializeField, Range(0f, 10f)]
    private float allyOutlineWidth = 1.4f;

    [Header("적군 외곽선")]
    [SerializeField]
    private Color enemyColor =
        new Color(0.82f, 0.25f, 0.24f, 1f);

    [SerializeField, Range(0f, 10f)]
    private float enemyOutlineWidth = 1.7f;

    [Header("외곽선 표시 방식")]
    [SerializeField]
    private Outline.Mode outlineMode =
        Outline.Mode.OutlineVisible;

    private Outline _outline;
    private bool _configured;

    private bool _isMine;
    private bool _isMyTeam;

    private int _smokeHideCount;

    private void Awake()
    {
        FindOutline();
    }

    private void FindOutline()
    {
        if (_outline != null)
            return;

        _outline = GetComponent<Outline>();

        if (_outline == null)
        {
            _outline =
                GetComponentInChildren<Outline>(true);
        }
    }

    public void Configure(
        bool isMine,
        bool isMyTeam)
    {
        FindOutline();

        if (_outline == null)
        {
            Debug.LogError(
                $"[PLAYER OUTLINE] Outline 컴포넌트가 없습니다. " +
                $"object={gameObject.name}"
            );

            return;
        }

        _configured = true;
        _isMine = isMine;
        _isMyTeam = isMyTeam;

        ApplyOutlineState();
    }

    private void ApplyOutlineState()
    {
        FindOutline();

        if (_outline == null)
            return;

        // 자기 자신은 항상 외곽선 끔
        if (_isMine)
        {
            _outline.enabled = false;
            return;
        }

        // 연막 안이면 외곽선 끔
        if (_smokeHideCount > 0)
        {
            _outline.enabled = false;
            return;
        }

        _outline.enabled = true;
        _outline.OutlineMode = outlineMode;

        if (_isMyTeam)
        {
            _outline.OutlineColor = allyColor;
            _outline.OutlineWidth = allyOutlineWidth;

            Debug.Log(
                $"[PLAYER OUTLINE] 아군 외곽선 적용. object={gameObject.name}"
            );
        }
        else
        {
            _outline.OutlineColor = enemyColor;
            _outline.OutlineWidth = enemyOutlineWidth;

            Debug.Log(
                $"[PLAYER OUTLINE] 적군 외곽선 적용. object={gameObject.name}"
            );
        }
    }

    public void DisableOutline()
    {
        FindOutline();

        if (_outline != null)
        {
            _outline.enabled = false;
        }
    }

    public void RefreshOutline()
    {
        if (!_configured)
            return;

        ApplyOutlineState();
    }

    public void AddSmokeHide()
    {
        _smokeHideCount++;

        FindOutline();

        if (_outline != null)
        {
            _outline.enabled = false;
        }

        Debug.Log(
            $"[PLAYER OUTLINE SMOKE HIDE ADD] object={gameObject.name}, count={_smokeHideCount}"
        );
    }

    public void RemoveSmokeHide()
    {
        _smokeHideCount =
            Mathf.Max(0, _smokeHideCount - 1);

        Debug.Log(
            $"[PLAYER OUTLINE SMOKE HIDE REMOVE] object={gameObject.name}, count={_smokeHideCount}"
        );

        if (_smokeHideCount == 0)
        {
            ApplyOutlineState();
        }
    }

    public void ClearSmokeHide()
    {
        _smokeHideCount = 0;
        ApplyOutlineState();
    }
}