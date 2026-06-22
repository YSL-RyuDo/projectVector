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
            _outline = GetComponentInChildren<Outline>(true);
        }
    }
    public void Configure(bool isMine, bool isMyTeam)
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

        // 자기 자신은 외곽선을 표시하지 않는다.
        if (isMine)
        {
            _outline.enabled = false;

            Debug.Log(
                $"[PLAYER OUTLINE] 로컬 플레이어 외곽선 비활성화. " +
                $"object={gameObject.name}"
            );

            return;
        }

        _outline.enabled = true;
        _outline.OutlineMode = outlineMode;

        if (isMyTeam)
        {
            _outline.OutlineColor = allyColor;
            _outline.OutlineWidth = allyOutlineWidth;

            Debug.Log(
                $"[PLAYER OUTLINE] 아군 외곽선 적용. " +
                $"object={gameObject.name}"
            );
        }
        else
        {
            _outline.OutlineColor = enemyColor;
            _outline.OutlineWidth = enemyOutlineWidth;

            Debug.Log(
                $"[PLAYER OUTLINE] 적군 외곽선 적용. " +
                $"object={gameObject.name}"
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

        FindOutline();

        if (_outline != null && _outline.enabled)
        {
            _outline.OutlineMode = outlineMode;
        }
    }
}