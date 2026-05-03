using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIAnimator : MonoBehaviour
{
    [Header("애니메이션 타입")]
    [SerializeField] private bool _fade;
    [SerializeField] private bool _scale;
    [SerializeField] private bool _up;
    [SerializeField] private bool _down;
    [SerializeField] private bool _left;
    [SerializeField] private bool _right;

    [Header("값 설정")]
    [Tooltip("지속 시간(값이 작을 수록 빨라짐)")]
    [SerializeField] private float _duration = 0.5f;

    [Tooltip("UI 시작 위치 오프셋")]
    [SerializeField] private float _moveOffset = 200f;

    [Tooltip("초기 스케일 값")]
    [SerializeField] private float _startScale = 0f;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private bool _uiAnimation = false;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _originalScale = transform.localScale;
    }

    /// <summary>
    /// UI 표시 + Animation 실행
    /// </summary>
    public void Show()
    {
        if (_uiAnimation) return;

        StopAllCoroutines();
        gameObject.SetActive(true);

        Vector2 basePos = _rectTransform.anchoredPosition;
        if (_scale) transform.localScale = Vector3.one * _startScale;
        if (_fade) _canvasGroup.alpha = 0f;

        StartCoroutine(openUI(basePos));
    }

    /// <summary>
    /// UI 닫기 + Animation 실행
    /// </summary>
    public void Close()
    {
        if (_uiAnimation) return;

        StopAllCoroutines();
        StartCoroutine(closeUI());
    }

    private Vector2 GetOffset(Vector2 pos)
    {
        Vector2 offset = pos;
        if (_up) offset -= new Vector2(0, _moveOffset);
        if (_down) offset += new Vector2(0, _moveOffset);
        if (_left) offset += new Vector2(_moveOffset, 0);
        if (_right) offset -= new Vector2(_moveOffset, 0);
        return offset;
    }

    private IEnumerator openUI(Vector2 basePos)
    {
        _uiAnimation = true;

        /* offset 시작 위치 */
        Vector2 startPos = GetOffset(basePos);
        /* 실제 드래그 위치 */
        Vector2 endPos = basePos;

        float time = 0f;
        while (time < _duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / _duration);

            /* 위치 애니메이션 */
            _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            /* 스케일 애니메이션 */
            if (_scale)
            {
                float scaleT = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(Vector3.one * _startScale, _originalScale, scaleT);
            }

            /* 페이드 애니메이션 */
            if (_fade) _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        _rectTransform.anchoredPosition = endPos;
        if (_scale) transform.localScale = _originalScale;
        if (_fade) _canvasGroup.alpha = 1f;

        _uiAnimation = false;
    }

    private IEnumerator closeUI()
    {
        _uiAnimation = true;

        /* 마지막 드래그 위치 */
        Vector2 basePos = _rectTransform.anchoredPosition;
        /* 드래그 위치에서 시작 */
        Vector2 startPos = basePos;
        /* offset 방향으로 이동 */
        Vector2 endPos = GetOffset(basePos);

        float time = 0f;
        while (time < _duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / _duration);

            _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            if (_scale)
            {
                float scaleT = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(_originalScale, Vector3.one * _startScale, scaleT);
            }

            if (_fade) _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        /* 닫기 끝나도 위치는 basePos로 되돌림 */
        _rectTransform.anchoredPosition = basePos;
        if (_scale) transform.localScale = _originalScale;
        if (_fade) _canvasGroup.alpha = 0f;

        gameObject.SetActive(false);
        _uiAnimation = false;
    }
}