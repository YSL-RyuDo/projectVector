using UnityEngine;
using UnityEngine.UI;

// All reflected Images are authored in the scene. Only displayed content is synchronized.
[ExecuteAlways]
public sealed class VectorCardReflection : MonoBehaviour
{
    public Image[] sources;
    public Image[] reflections;

    void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (sources == null || reflections == null) return;
        for (int i = 0; i < Mathf.Min(sources.Length, reflections.Length); i++)
        {
            var source = sources[i];
            var target = reflections[i];
            if (source == null || target == null) continue;
            target.enabled = source.enabled;
            // The source card's parent already controls the whole reflection's visibility.
            // Disabling the reflection root here would prevent it from refreshing on rejoin.
            if (i != 0) target.gameObject.SetActive(source.gameObject.activeSelf);
            target.sprite = source.sprite;
            target.color = source.color;
            // The first entry mirrors the card root. Its transform is the reflection's anchor.
            if (i == 0) continue;
            var from = source.rectTransform;
            var to = target.rectTransform;
            to.anchorMin = from.anchorMin; to.anchorMax = from.anchorMax;
            to.pivot = from.pivot; to.sizeDelta = from.sizeDelta;
            to.anchoredPosition = from.anchoredPosition;
            to.localScale = from.localScale; to.localRotation = from.localRotation;
        }
    }
}
