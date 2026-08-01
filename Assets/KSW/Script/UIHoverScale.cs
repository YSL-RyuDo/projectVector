using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);
    
    [Tooltip("Scale Speed")]
    [SerializeField] private float animationSpeed = 15f;

    private Vector3 defaultScale;
    private Coroutine scaleCoroutine;

    private void Awake()
    {
        defaultScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartScaleAnimation(Vector3.Scale(defaultScale, hoverScale));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartScaleAnimation(defaultScale);
    }

    private void StartScaleAnimation(Vector3 targetScale)
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(AnimateScale(targetScale));
    }

    private IEnumerator AnimateScale(Vector3 targetScale)
    {
        while (Vector3.Distance(transform.localScale, targetScale) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
            yield return null;
        }

        transform.localScale = targetScale;
    }

    private void OnDisable()
    {
        transform.localScale = defaultScale;
    }
}