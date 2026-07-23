using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카카오톡 스타일 대화 시스템
/// - 상대방: 왼쪽 말풍선 / 나(학생): 오른쪽 말풍선
/// - 1초 간격 자동 출력
/// - 선택지 나오면 버튼 표시 후 대기
/// - FavorLevel 단위로 대화 묶음 전체 출력
/// - 스크롤로 이전 대화 확인 가능
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private string myName = "학생";           // 내 이름 (우측 말풍선)
    [SerializeField] private int currentFavorLevel = 0;        // 현재 호감도

    [Header("스크롤 뷰")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRect;        // ScrollRect > Viewport > Content

    [Header("말풍선 프리팹")]
    [SerializeField] private GameObject leftBubblePrefab;      // 상대방 말풍선 (좌)
    [SerializeField] private GameObject rightBubblePrefab;     // 내 말풍선 (우)

    [Header("선택지 UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button choiceButton1;
    [SerializeField] private Button choiceButton2;
    [SerializeField] private Button choiceButton3;
    [SerializeField] private TMP_Text choiceText1;
    [SerializeField] private TMP_Text choiceText2;
    [SerializeField] private TMP_Text choiceText3;

    [Header("설정")]
    [SerializeField] private float lineInterval = 1f;

    // ── 내부 상태 ──
    private Coroutine playCoroutine;
    private bool isPlaying;

    // ─────────────────────────────────────────
    // 공개 메서드
    // ─────────────────────────────────────────

    /// 현재 호감도 대화 전체 시작
    public void StartCurrentLevel()
    {
        StartFavorLevel(currentFavorLevel);
    }

    /// 특정 호감도 레벨 대화 시작
    public void StartFavorLevel(int favorLevel)
    {
        if (isPlaying)
        {
            StopCoroutine(playCoroutine);
            isPlaying = false;
        }

        choicePanel.SetActive(false);
        currentFavorLevel = favorLevel;
        playCoroutine = StartCoroutine(PlayFavorLevel(favorLevel));
    }

    /// 호감도 올리고 다음 레벨 시작
    public void AdvanceToNextLevel()
    {
        currentFavorLevel++;
        StartFavorLevel(currentFavorLevel);
    }

    // ─────────────────────────────────────────
    // 코루틴
    // ─────────────────────────────────────────

    private IEnumerator PlayFavorLevel(int favorLevel)
    {
        isPlaying = true;

        List<string> ids = dialogueData.GetDialogueIds(favorLevel);

        if (ids == null || ids.Count == 0)
        {
            Debug.Log($"[DIALOGUE] FavorLevel {favorLevel} 데이터 없음");
            isPlaying = false;
            yield break;
        }

        // D0_1 → D0_2 → D0_3(선택지) 순서로 순회
        int i = 0;
        while (i < ids.Count)
        {
            string dialogueId = ids[i];
            List<DialogueEntry> lines = dialogueData.GetLines(dialogueId);

            foreach (DialogueEntry entry in lines)
            {
                // 마지막 줄이고 선택지 있으면 선택지 표시 후 대기
                bool isLastOfBlock = entry == lines[lines.Count - 1];

                if (isLastOfBlock && entry.HasChoices)
                {
                    // 선택지 있는 대사는 말풍선으로 먼저 출력
                    SpawnBubble(entry.speaker, entry.text);
                    yield return new WaitForSeconds(lineInterval);

                    // 선택지 버튼 표시
                    bool chosen = false;
                    string nextId = null;

                    ShowChoices(entry, (selectedNextId) =>
                    {
                        chosen = true;
                        nextId = selectedNextId;
                    });

                    // 선택 대기
                    yield return new WaitUntil(() => chosen);

                    // 선택한 내용을 내 말풍선으로 표시
                    DialogueChoice selectedChoice = GetChoiceByNextId(entry, nextId);
                    if (selectedChoice != null)
                        SpawnBubble(myName, selectedChoice.text);

                    yield return new WaitForSeconds(lineInterval);

                    // 선택지 결과 대화 출력
                    if (!string.IsNullOrEmpty(nextId))
                    {
                        List<DialogueEntry> resultLines = dialogueData.GetLines(nextId);
                        foreach (var r in resultLines)
                        {
                            SpawnBubble(r.speaker, r.text);
                            yield return new WaitForSeconds(lineInterval);
                        }
                    }

                    // 선택지 블록 이후 같은 레벨 남은 ID 건너뜀 (선택 분기로 끝)
                    i = ids.Count;
                    break;
                }
                else
                {
                    SpawnBubble(entry.speaker, entry.text);
                    yield return new WaitForSeconds(lineInterval);
                }
            }

            i++;
        }

        Debug.Log($"[DIALOGUE] FavorLevel {favorLevel} 완료");
        isPlaying = false;
    }

    // ─────────────────────────────────────────
    // 말풍선 생성
    // ─────────────────────────────────────────

    private void SpawnBubble(string speaker, string text)
    {
        bool isMine = speaker == myName;
        GameObject prefab = isMine ? rightBubblePrefab : leftBubblePrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"[DIALOGUE] 말풍선 프리팹 없음. isMine={isMine}");
            return;
        }

        GameObject bubble = Instantiate(prefab, contentRect);

        // 말풍선 안의 텍스트 설정
        TMP_Text[] texts = bubble.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            if (t.gameObject.name.Contains("Speaker") || t.gameObject.name.Contains("Name"))
                t.text = speaker;
            else if (t.gameObject.name.Contains("Text") || t.gameObject.name.Contains("Message"))
                t.text = text;
            else
                t.text = text;   // TMP가 하나뿐이면 그냥 텍스트로
        }

        // 스크롤을 항상 최하단으로
        StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        // 레이아웃 갱신 후 스크롤
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // ─────────────────────────────────────────
    // 선택지
    // ─────────────────────────────────────────

    private void ShowChoices(DialogueEntry entry, System.Action<string> onSelected)
    {
        choicePanel.SetActive(true);

        SetupChoice(choiceButton1, choiceText1, entry.choice1, onSelected);
        SetupChoice(choiceButton2, choiceText2, entry.choice2, onSelected);
        SetupChoice(choiceButton3, choiceText3, entry.choice3, onSelected);
    }

    private void SetupChoice(Button btn, TMP_Text label, DialogueChoice choice, System.Action<string> onSelected)
    {
        if (btn == null) return;

        bool valid = choice != null && !string.IsNullOrEmpty(choice.text);
        btn.gameObject.SetActive(valid);
        if (!valid) return;

        label.text = choice.text;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            choicePanel.SetActive(false);
            onSelected?.Invoke(choice.nextDialogueId);
        });
    }

    private DialogueChoice GetChoiceByNextId(DialogueEntry entry, string nextId)
    {
        if (entry.choice1 != null && entry.choice1.nextDialogueId == nextId) return entry.choice1;
        if (entry.choice2 != null && entry.choice2.nextDialogueId == nextId) return entry.choice2;
        if (entry.choice3 != null && entry.choice3.nextDialogueId == nextId) return entry.choice3;
        return null;
    }
}