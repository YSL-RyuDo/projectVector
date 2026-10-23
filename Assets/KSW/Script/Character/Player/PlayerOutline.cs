using UnityEngine;

[DisallowMultipleComponent]
public class PlayerOutline : MonoBehaviour
{
    [Header("아군 외곽선")]
    [SerializeField] private Color allyColor = new Color(0.18f, 0.52f, 0.78f, 1f);
    [SerializeField, Range(0f, 10f)] private float allyOutlineWidth = 1.4f;

    [Header("적군 외곽선")]
    [SerializeField] private Color enemyColor = new Color(0.82f, 0.25f, 0.24f, 1f);
    [SerializeField, Range(0f, 10f)] private float enemyOutlineWidth = 1.7f;

    [Header("외곽선 표시 방식")]
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineVisible;

    private Outline _outline;
    private bool _configured;
    private bool _isMine;
    private bool _isMyTeam;
    private CharacterController _bodyCapsule;

    public bool IsOccludedBySmoke(Camera camera)
    {
        if (!camera) return false;
        if (!_bodyCapsule) _bodyCapsule = GetComponentInParent<CharacterController>();
        if (!_bodyCapsule) _bodyCapsule = GetComponentInChildren<CharacterController>(true);
        Bounds body;
        if (_bodyCapsule) {
            var t=_bodyCapsule.transform;var scale=t.lossyScale;
            float radius=_bodyCapsule.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
            body=new Bounds(t.TransformPoint(_bodyCapsule.center),new Vector3(radius*2,Mathf.Max(radius*2,_bodyCapsule.height*Mathf.Abs(scale.y)),radius*2));
        } else {
            // A target without a movement capsule still uses its current rendered body extent.
            var renderer=GetComponentInChildren<SkinnedMeshRenderer>();
            body=renderer?renderer.bounds:new Bounds(transform.position+Vector3.up, new Vector3(.6f,2,.6f));
        }
        if (DreamForestPresentation.BlocksOutline(camera,body)) return true;
        // Ordinary smoke still preserves allied identification; the ultimate has
        // its own boundary and enemy-visitor identification rules above.
        if (_configured && _isMyTeam && !_isMine) return false;
        return SmokeOutlineOccluder.BlocksOutline(camera.transform.position,body);
    }

    private int _smokeHideCount;
    private int _dreamForestHideCount;

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
            _outline = GetComponentInChildren<Outline>(true);
    }

    public void Configure(bool isMine, bool isMyTeam)
    {
        FindOutline();

        if (_outline == null)
        {
            Debug.LogError($"[PLAYER OUTLINE] Outline 컴포넌트 없음 object={gameObject.name}");
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

        if (!_configured)
        {
            _outline.enabled = false;
            return;
        }

        if (_isMine)
        {
            _outline.enabled = false;
            return;
        }

        if (_smokeHideCount > 0)
        {
            _outline.enabled = false;
            return;
        }

        if (_dreamForestHideCount > 0)
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
        }
        else
        {
            _outline.OutlineColor = enemyColor;
            _outline.OutlineWidth = enemyOutlineWidth;
        }

        Debug.Log($"[PLAYER OUTLINE APPLY] object={gameObject.name}, mine={_isMine}, myTeam={_isMyTeam}, color={_outline.OutlineColor}, width={_outline.OutlineWidth}");
    }

    public void DisableOutline()
    {
        FindOutline();

        if (_outline != null)
            _outline.enabled = false;
    }

    public void RefreshOutline()
    {
        ApplyOutlineState();
    }

    public void AddSmokeHide()
    {
        _smokeHideCount++;
        ApplyOutlineState();

        Debug.Log($"[PLAYER OUTLINE SMOKE ADD] object={gameObject.name}, count={_smokeHideCount}");
    }

    public void RemoveSmokeHide()
    {
        _smokeHideCount = Mathf.Max(0, _smokeHideCount - 1);
        ApplyOutlineState();

        Debug.Log($"[PLAYER OUTLINE SMOKE REMOVE] object={gameObject.name}, count={_smokeHideCount}");
    }

    public void ClearSmokeHide()
    {
        _smokeHideCount = 0;
        ApplyOutlineState();

        Debug.Log($"[PLAYER OUTLINE SMOKE CLEAR] object={gameObject.name}");
    }

    public void AddDreamForestHide()
    {
        _dreamForestHideCount++;
        ApplyOutlineState();

        Debug.Log($"[PLAYER OUTLINE DREAM ADD] object={gameObject.name}, count={_dreamForestHideCount}");
    }

    public void RemoveDreamForestHide()
    {
        _dreamForestHideCount = Mathf.Max(0, _dreamForestHideCount - 1);
        ApplyOutlineState();

        Debug.Log($"[PLAYER OUTLINE DREAM REMOVE] object={gameObject.name}, count={_dreamForestHideCount}");
    }

    public void ClearDreamForestHide()
    {
        _dreamForestHideCount = 0;
        ApplyOutlineState();

        Debug.Log($"[PLAYER OUTLINE DREAM CLEAR] object={gameObject.name}");
    }

    public void ForceRefreshColor()
    {
        _smokeHideCount = 0;
        _dreamForestHideCount = 0;
        ApplyOutlineState();

        Debug.Log($"[PLAYER OUTLINE FORCE REFRESH COLOR] object={gameObject.name}");
    }
}
