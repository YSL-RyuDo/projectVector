using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카카오톡 스타일 대화 시스템
///
/// [구조]
/// 왼쪽: 캐릭터 목록 버튼 (CharacterListPanel)
/// 오른쪽 위: 채팅 ScrollRect (ChatPanel)
/// 오른쪽 아래: 선택지 패널 (ChoicePanel)
///
/// [기능]
/// 캐릭터별 채팅 기록 독립 보존
/// 캐릭터 전환 시 이전 채팅 기록 복원
/// 아이콘은 DialogueManager Inspector에서 직접 관리 (별도 SO 불필요)
/// FavorLevel 단위 대화 (D0_x 완료 → D1_x)
/// </summary>
public class DialogueManager : MonoBehaviour
{
    // Inspector에서 직접 등록하는 캐릭터 정보
    [System.Serializable]
    public class CharacterEntry
    {
        public string characterName;      // CSV의 Speaker 이름과 일치
        public Sprite icon;               // 캐릭터 아이콘 Sprite
        public DialogueData dialogueData; // 이 캐릭터의 DialogueData 에셋
    }

    [Header("캐릭터 설정")]
    [SerializeField] private CharacterEntry[] characters;   // 4명
    [SerializeField] private string myName = "학생";

    [Header("왼쪽 캐릭터 목록")]
    [SerializeField] private Transform characterListParent; // 캐릭터 버튼들의 부모
    [SerializeField] private GameObject characterButtonPrefab; // 캐릭터 선택 버튼 프리팹

    [Header("오른쪽 채팅창")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRect;    // Content (Vertical Layout Group)

    [Header("말풍선 프리팹")]
    [SerializeField] private GameObject leftBubblePrefab;  // 상대방 (좌)
    [SerializeField] private GameObject rightBubblePrefab; // 나 (우)

    [Header("선택지 패널")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button choiceButton1;
    [SerializeField] private Button choiceButton2;
    [SerializeField] private Button choiceButton3;
    [SerializeField] private TMP_Text choiceText1;
    [SerializeField] private TMP_Text choiceText2;
    [SerializeField] private TMP_Text choiceText3;

    [Header("설정")]
    [SerializeField] private float lineInterval = 1f;

    // 캐릭터별 상태
    private class CharacterState
    {
        public int favorLevel = 0;
        public List<GameObject> bubbles = new List<GameObject>(); // 생성된 말풍선들
        public bool isPlaying = false;
        public Coroutine playCoroutine;
        public bool isWaitingChoice = false;
        public DialogueEntry pendingChoiceEntry = null;  // 대기 중인 선택지 엔트리
        public System.Action<string> pendingOnSelected = null; // 선택 콜백
    }

    private Dictionary<string, CharacterState> states = new Dictionary<string, CharacterState>();
    private CharacterEntry currentCharacter;
    private Coroutine currentCoroutine;

    // 초기화

    private void Start()
    {
        // 캐릭터별 상태 초기화
        foreach (var c in characters)
        {
            if (c == null) continue;
            states[c.characterName] = new CharacterState();
        }

        // 캐릭터 목록 버튼 생성
        BuildCharacterList();

        // 첫 번째 캐릭터 선택
        if (characters.Length > 0 && characters[0] != null)
            SelectCharacter(characters[0]);
    }

    // 캐릭터 목록 버튼 생성

    private void BuildCharacterList()
    {
        foreach (Transform child in characterListParent)
            Destroy(child.gameObject);

        foreach (var c in characters)
        {
            if (c == null) continue;

            GameObject btn = Instantiate(characterButtonPrefab, characterListParent);

            // 버튼 안의 이름 텍스트
            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = c.characterName;

            // 버튼 안의 아이콘 Image 이름에 "Icon" 포함된 Image
            Image[] images = btn.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name.Contains("Icon") && c.icon != null)
                {
                    img.sprite = c.icon;
                    img.gameObject.SetActive(true);
                    break;
                }
            }

            // 캐릭터 캡처 (람다 클로저 방지)
            CharacterEntry captured = c;
            Button button = btn.GetComponentInChildren<Button>();
            if (button != null)
                button.onClick.AddListener(() => SelectCharacter(captured));
            else
                Debug.LogError($"[DIALOGUE] {c.characterName} 버튼 프리팹에 Button 컴포넌트 없음");
        }
    }

    // 캐릭터 전환

    public void SelectCharacter(CharacterEntry character)
    {
        if (character == null) return;

        // 이전 캐릭터 말풍선 숨기기
        if (currentCharacter != null && states.TryGetValue(currentCharacter.characterName, out var prevState))
        {
            foreach (var b in prevState.bubbles)
                if (b != null) b.SetActive(false);

            prevState.isWaitingChoice = choicePanel.activeSelf;
        }

        currentCharacter = character;

        if (!states.TryGetValue(character.characterName, out var state))
        {
            state = new CharacterState();
            states[character.characterName] = state;
        }

        foreach (var b in state.bubbles)
            if (b != null) b.SetActive(true);

        // 내용까지 다시 세팅
        if (state.isWaitingChoice && state.pendingChoiceEntry != null)
        {
            ShowChoices(state.pendingChoiceEntry, state.pendingOnSelected);
        }
        else
        {
            choicePanel.SetActive(false);
        }

        // 대화가 아직 한 번도 시작 안 됐으면 자동 시작
        if (state.bubbles.Count == 0 && !state.isPlaying)
            StartFavorLevel(character, state.favorLevel);

        // 재생 중이었으면 코루틴 이어서 진행 (contentRect는 공유하므로 버블만 on off)
        StartCoroutine(ScrollToBottom());
    }

    // 대화 재생

    private void StartFavorLevel(CharacterEntry character, int favorLevel)
    {
        if (!states.TryGetValue(character.characterName, out var state)) return;

        if (state.playCoroutine != null)
            StopCoroutine(state.playCoroutine);

        state.isPlaying = true;
        state.favorLevel = favorLevel;
        state.playCoroutine = StartCoroutine(PlayFavorLevel(character, state, favorLevel));
    }

    public void AdvanceToNextLevel()
    {
        if (currentCharacter == null) return;
        if (!states.TryGetValue(currentCharacter.characterName, out var state)) return;
        StartFavorLevel(currentCharacter, state.favorLevel + 1);
    }

    private IEnumerator PlayFavorLevel(CharacterEntry character, CharacterState state, int favorLevel)
    {
        DialogueData data = character.dialogueData;
        if (data == null) yield break;

        List<string> ids = data.GetDialogueIds(favorLevel);
        if (ids == null || ids.Count == 0)
        {
            Debug.Log($"[DIALOGUE] {character.characterName} FavorLevel {favorLevel} 데이터 없음");
            state.isPlaying = false;
            yield break;
        }

        int i = 0;
        while (i < ids.Count)
        {
            string dialogueId = ids[i];
            List<DialogueEntry> lines = data.GetLines(dialogueId);

            foreach (DialogueEntry entry in lines)
            {
                bool isLastOfBlock = entry == lines[lines.Count - 1];

                if (isLastOfBlock && entry.HasChoices)
                {
                    SpawnBubble(character, entry.speaker, entry.text, state);
                    yield return new WaitForSeconds(lineInterval);

                    // 현재 선택 중인 캐릭터일 때만 선택지 표시
                    // 다른 캐릭터 보는 중이면 이 캐릭터로 돌아올 때까지 대기
                    yield return new WaitUntil(() => currentCharacter == character);

                    bool chosen = false;
                    string nextId = null;

                    System.Action<string> onSelected = (selectedNextId) =>
                    {
                        chosen = true;
                        nextId = selectedNextId;
                    };

                    state.pendingChoiceEntry = entry;
                    state.pendingOnSelected = onSelected;
                    state.isWaitingChoice = true;

                    ShowChoices(entry, onSelected);

                    yield return new WaitUntil(() => chosen);

                    // 선택 완료 시 정리
                    state.pendingChoiceEntry = null;
                    state.pendingOnSelected = null;

                    // 내 선택을 오른쪽 말풍선으로
                    DialogueChoice selectedChoice = GetChoiceByNextId(entry, nextId);
                    if (selectedChoice != null)
                        SpawnBubble(character, myName, selectedChoice.text, state);

                    yield return new WaitForSeconds(lineInterval);

                    // 선택 결과 대화
                    if (!string.IsNullOrEmpty(nextId))
                    {
                        List<DialogueEntry> resultLines = data.GetLines(nextId);
                        foreach (var r in resultLines)
                        {
                            SpawnBubble(character, r.speaker, r.text, state);
                            yield return new WaitForSeconds(lineInterval);
                        }
                    }

                    i = ids.Count; // 선택지 블록 이후 나머지 ID 스킵
                    break;
                }
                else
                {
                    SpawnBubble(character, entry.speaker, entry.text, state);
                    yield return new WaitForSeconds(lineInterval);
                }
            }

            i++;
        }

        Debug.Log($"[DIALOGUE] {character.characterName} FavorLevel {favorLevel} 완료");
        state.isPlaying = false;
    }

    // 말풍선 생성

    private void SpawnBubble(CharacterEntry character, string speaker, string text, CharacterState state)
    {
        bool isMine = speaker == myName;
        GameObject prefab = isMine ? rightBubblePrefab : leftBubblePrefab;
        if (prefab == null) return;

        GameObject bubble = Instantiate(prefab, contentRect);

        // 텍스트
        TMP_Text[] texts = bubble.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            string n = t.gameObject.name.ToLower();
            if (n.Contains("name") || n.Contains("speaker"))
                t.text = speaker;
            else
                t.text = text;
        }

        // 아이콘: 프리팹 밖 CharacterData에서 동적으로 주입
        // 말풍선 프리팹 안에 "Icon" 이름 Image가 있으면 CharacterData.icon을 넣어줌
        if (!isMine)
        {
            Image[] images = bubble.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name.ToLower().Contains("icon"))
                {
                    if (character.icon != null)
                    {
                        img.sprite = character.icon;
                        img.gameObject.SetActive(true);
                    }
                    else
                    {
                        img.gameObject.SetActive(false);
                    }
                    break;
                }
            }
        }

        // 현재 보고 있는 캐릭터가 아니면 즉시 숨김 (기록은 유지)
        if (currentCharacter != character)
            bubble.SetActive(false);

        state.bubbles.Add(bubble);

        if (currentCharacter == character)
            StartCoroutine(ScrollToBottom());
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // 선택지

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

            // 현재 캐릭터 선택지 대기 상태 해제
            if (currentCharacter != null && states.TryGetValue(currentCharacter.characterName, out var state))
            {
                state.isWaitingChoice = false;
                state.pendingChoiceEntry = null;
                state.pendingOnSelected = null;
            }

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