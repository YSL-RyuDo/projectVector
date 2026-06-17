using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KillLogManager : MonoBehaviour
{
    public static KillLogManager Instance;

    [Header("Scroll View")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;

    [Header("Kill Log Prefabs")]
    [SerializeField] private KillLogEntryUI ourKillLogPrefab;
    [SerializeField] private KillLogEntryUI enemyKillLogPrefab;

    [Header("Character Images")]
    [Tooltip("배열 인덱스가 characterId와 같아야 합니다.")]
    [SerializeField] private Sprite[] characterSprites;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 5f;
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private int maxVisibleLogs = 5;

    private readonly List<KillLogEntryUI> activeEntries = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ShowKillLog(
        bool isOurKill,
        int killerCharacterId,
        int deadCharacterId,
        string killerNickname,
        string deadNickname)
    {
        if (content == null)
        {
            Debug.LogError(
                "[KILL LOG] Content가 연결되지 않았습니다."
            );

            return;
        }

        KillLogEntryUI selectedPrefab =
            isOurKill
                ? ourKillLogPrefab
                : enemyKillLogPrefab;

        if (selectedPrefab == null)
        {
            Debug.LogError(
                $"[KILL LOG] prefab missing. isOurKill={isOurKill}"
            );

            return;
        }

        KillLogEntryUI entry =
            Instantiate(selectedPrefab, content);

        // 가장 최근 킬로그를 위에 표시
        entry.transform.SetAsFirstSibling();

        Sprite killerSprite =
            GetCharacterSprite(killerCharacterId);

        Sprite deadSprite =
            GetCharacterSprite(deadCharacterId);

        entry.Initialize(
            killerSprite,
            deadSprite,
            killerNickname,
            deadNickname
        );

        activeEntries.Insert(0, entry);

        // 최대 개수 초과 시 가장 오래된 항목 즉시 제거
        while (activeEntries.Count > maxVisibleLogs)
        {
            int lastIndex = activeEntries.Count - 1;

            KillLogEntryUI oldest =
                activeEntries[lastIndex];

            activeEntries.RemoveAt(lastIndex);

            if (oldest != null)
                Destroy(oldest.gameObject);
        }

        StartCoroutine(RemoveAfterDelay(entry));

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        if (scrollRect != null)
        {
            // 최신 로그가 위에 있으므로 스크롤도 위로 이동
            scrollRect.verticalNormalizedPosition = 1f;
        }

        Debug.Log(
            $"[KILL LOG SHOW] our={isOurKill}, " +
            $"killer={killerNickname}, dead={deadNickname}, " +
            $"killerCharacter={killerCharacterId}, " +
            $"deadCharacter={deadCharacterId}"
        );
    }

    private Sprite GetCharacterSprite(int characterId)
    {
        if (characterSprites == null)
            return null;

        if (characterId < 0 ||
            characterId >= characterSprites.Length)
        {
            Debug.LogWarning(
                $"[KILL LOG] invalid characterId={characterId}"
            );

            return null;
        }

        return characterSprites[characterId];
    }

    private IEnumerator RemoveAfterDelay(
        KillLogEntryUI entry)
    {
        yield return new WaitForSeconds(displayDuration);

        if (entry == null)
            yield break;

        CanvasGroup canvasGroup =
            entry.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                entry.gameObject.AddComponent<CanvasGroup>();
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            if (entry == null)
                yield break;

            elapsed += Time.deltaTime;

            canvasGroup.alpha =
                1f - Mathf.Clamp01(elapsed / fadeDuration);

            yield return null;
        }

        activeEntries.Remove(entry);

        if (entry != null)
            Destroy(entry.gameObject);
    }

    public void ClearAll()
    {
        StopAllCoroutines();

        for (int i = 0; i < activeEntries.Count; i++)
        {
            if (activeEntries[i] != null)
                Destroy(activeEntries[i].gameObject);
        }

        activeEntries.Clear();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}