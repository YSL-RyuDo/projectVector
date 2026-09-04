using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카카오톡 스타일 대화 시스템
///
/// 1. 로비 씬 시작 → NetworkManager로부터 캐릭터별 호감도 수신
/// 2. 이전 세션 데이터(PlayerPrefs)와 비교해서 호감도가 올랐으면 새 대화 출력 + 알림 표시
/// 3. 이미 본 대화는 말풍선 히스토리로 복원 (반복 출력 없음)
/// 4. 게임 종료 후 재접속해도 이전 대화 내역 유지
/// </summary>
public class DialogueManager : MonoBehaviour
{
    // Inspector에서 직접 등록하는 캐릭터 정보
    [System.Serializable]
    public class CharacterEntry
    {
        public string characterName;      // CSV의 Speaker 이름과 일치
        public int characterId;           // 서버 캐릭터 ID (NetworkManager와 매핑)
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

    [Header("새 톡 알림 UI")]
    [SerializeField] private GameObject[] newBadges;

    [Header("설정")]
    [SerializeField] private float lineInterval = 1f;

    // PlayerPrefs 키
    // "DM_favor_{characterName}"      마지막으로 저장된 호감도 레벨 (int)
    // "DM_seen_{characterName}_{id}"  해당 DialogueID를 완료했는지 (1 = 완료)
    private const string PrefsFavorKey = "DM_favor_";
    private const string PrefsSeenKey = "DM_seen_";

    // 캐릭터별 상태
    private class CharacterState
    {
        public int serverFavorLevel = 0;       // 서버에서 받은 현재 호감도
        public int lastSeenFavorLevel = 0;     // 이전 세션에서 마지막으로 본 호감도
        public List<GameObject> bubbles = new List<GameObject>();
        public bool isPlaying = false;
        public Coroutine playCoroutine;
        public bool isWaitingChoice = false;
        public DialogueEntry pendingChoiceEntry = null;
        public System.Action<string> pendingOnSelected = null;
        public bool hasNewContent = false;     // 새 톡 알림 여부
        public GameObject buttonRef;           // 캐릭터 버튼 GameObject 참조
    }

    private Dictionary<string, CharacterState> states = new Dictionary<string, CharacterState>();
    private CharacterEntry currentCharacter;

    // 초기화

    private void Start()
    {
        // 캐릭터별 상태 초기화
        foreach (var c in characters)
        {
            if (c == null) continue;

            var state = new CharacterState();
            // PlayerPrefs에서 이전 세션 호감도 로드
            state.lastSeenFavorLevel = PlayerPrefs.GetInt(PrefsFavorKey + c.characterName, 0);
            state.serverFavorLevel = state.lastSeenFavorLevel; // 서버 응답 전 기본값
            states[c.characterName] = state;
        }

        // 캐릭터 목록 버튼 생성
        BuildCharacterList();

        // 서버에 호감도 요청 - NetworkManager에 SendFavorLevelRequest() 추가 필요
        // NetworkManager.Instance?.SendFavorLevelRequest();
        // -> 서버 응답 시 OnRecvFavorLevels()가 호출됨

        // 개발 테스트용 서버 없이 바로 로컬 데이터로 시작
        InitWithLocalData();
    }

    private void InitWithLocalData()
    {
        foreach (var c in characters)
        {
            if (c == null) continue;
            var state = states[c.characterName];
            // 로컬 저장값 = 마지막으로 본 레벨이므로 새 톡 없음
            state.hasNewContent = false;
        }

        UpdateAllBadges();

        if (characters.Length > 0 && characters[0] != null)
            SelectCharacter(characters[0]);
    }

    public static DialogueManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// NetworkManager가 서버로부터 호감도를 받아오면 이 함수를 호출
    /// key = characterId (int), value = favorLevel (int)
    /// </summary>
    public void OnRecvFavorLevels(Dictionary<int, int> favorByCharacterId)
    {
        foreach (var c in characters)
        {
            if (c == null) continue;
            if (!favorByCharacterId.TryGetValue(c.characterId, out int serverLevel)) continue;
            if (!states.TryGetValue(c.characterName, out var state)) continue;

            int prevLevel = state.lastSeenFavorLevel;
            state.serverFavorLevel = serverLevel;

            if (serverLevel > prevLevel)
            {
                // 호감도 올랐음 -> 새 톡 알림
                state.hasNewContent = true;
                Debug.Log($"[DIALOGUE] {c.characterName} 호감도 상승: {prevLevel} → {serverLevel}");
            }
        }

        UpdateAllBadges();

        // 현재 선택된 캐릭터가 새 톡이 있으면 바로 이어서 출력
        if (currentCharacter != null &&
            states.TryGetValue(currentCharacter.characterName, out var cur) &&
            cur.hasNewContent)
        {
            PlayNewLevels(currentCharacter, cur);
        }
    }

    // 캐릭터 목록 버튼 생성

    private void BuildCharacterList()
    {
        foreach (Transform child in characterListParent)
            Destroy(child.gameObject);

        for (int i = 0; i < characters.Length; i++)
        {
            var c = characters[i];
            if (c == null) continue;

            GameObject btn = Instantiate(characterButtonPrefab, characterListParent);

            if (!states.TryGetValue(c.characterName, out var state)) continue;
            state.buttonRef = btn;

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = c.characterName;

            Image[] images = btn.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name == "Image" && c.icon != null)
                {
                    img.sprite = c.icon;
                    img.color = Color.white;
                    img.gameObject.SetActive(true);
                    break;
                }
            }

            // newBadges 배열에 직접 연결된 게 있으면 사용
            // 없으면 버튼 프리팹 안에서 "NewBadge" 이름으로 찾기
            if (newBadges != null && i < newBadges.Length && newBadges[i] != null)
            {
                Debug.Log("No Badges");
            }
            else
            {
                // 프리팹 안에서 자동 탐색
                foreach (Transform child in btn.GetComponentsInChildren<Transform>(true))
                {
                    if (child.gameObject.name == "NewBadge")
                    {
                        // newBadges 배열 크기 확장해서 저장
                        if (newBadges == null) newBadges = new GameObject[characters.Length];
                        if (i < newBadges.Length) newBadges[i] = child.gameObject;
                        break;
                    }
                }
            }

            CharacterEntry captured = c;
            Button button = btn.GetComponentInChildren<Button>();
            if (button != null)
                button.onClick.AddListener(() => SelectCharacter(captured));
            else
                Debug.LogError($"[DIALOGUE] {c.characterName} 버튼에 Button 컴포넌트 없음");
        }
    }

    // 배지(알림) 갱신

    private void UpdateAllBadges()
    {
        for (int i = 0; i < characters.Length; i++)
        {
            var c = characters[i];
            if (c == null) continue;
            if (!states.TryGetValue(c.characterName, out var state)) continue;

            bool show = state.hasNewContent;

            if (newBadges != null && i < newBadges.Length && newBadges[i] != null)
                newBadges[i].SetActive(show);
        }
    }

    private void SetBadge(CharacterEntry character, bool show)
    {
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] != character) continue;
            if (newBadges != null && i < newBadges.Length && newBadges[i] != null)
                newBadges[i].SetActive(show);
            break;
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

        // 선택지 복원
        if (state.isWaitingChoice && state.pendingChoiceEntry != null)
            ShowChoices(state.pendingChoiceEntry, state.pendingOnSelected);
        else
            choicePanel.SetActive(false);

        // 처음 진입 -> 이미 본 대화 히스토리 복원 후 새 대화 출력
        if (state.bubbles.Count == 0 && !state.isPlaying)
            StartCoroutine(RestoreAndPlayRoutine(character, state));

        // 새 톡이 있으면 배지 끄기 (읽음 처리)
        if (state.hasNewContent)
        {
            state.hasNewContent = false;
            SetBadge(character, false);
        }

        StartCoroutine(ScrollToBottom());
    }

    // 히스토리 복원 -> 새 대화 재생

    /// <summary>
    /// 이미 본 대화(lastSeenFavorLevel까지)를 빠르게 복원하고
    /// 새로 올라온 레벨(serverFavorLevel까지)을 정상 속도로 출력
    /// </summary>
    private IEnumerator RestoreAndPlayRoutine(CharacterEntry character, CharacterState state)
    {
        state.isPlaying = true;
        DialogueData data = character.dialogueData;
        if (data == null) { state.isPlaying = false; yield break; }

        //  1 이미 본 레벨 히스토리 즉시 복원 
        for (int lv = 0; lv <= state.lastSeenFavorLevel; lv++)
        {
            List<string> ids = data.GetDialogueIds(lv);
            if (ids == null) continue;

            foreach (string id in ids)
            {
                // 이 DialogueID를 완료했으면 복원
                if (!IsSeenDialogue(character.characterName, id)) continue;

                List<DialogueEntry> lines = data.GetLines(id);
                foreach (var entry in lines)
                {
                    // 선택지 줄은 텍스트만 복원 (버튼 표시 안 함)
                    SpawnBubbleInstant(character, entry.speaker, entry.text, state);

                    // 선택지 결과도 복원
                    if (entry.HasChoices)
                    {
                        string selectedNextId = LoadSelectedChoice(character.characterName, id);
                        if (!string.IsNullOrEmpty(selectedNextId))
                        {
                            DialogueChoice chosen = GetChoiceByNextId(entry, selectedNextId);
                            if (chosen != null)
                                SpawnBubbleInstant(character, myName, chosen.text, state);

                            List<DialogueEntry> resultLines = data.GetLines(selectedNextId);
                            foreach (var r in resultLines)
                                SpawnBubbleInstant(character, r.speaker, r.text, state);
                        }
                        break; // 선택지 이후 이 블록 종료
                    }
                }
            }
        }

        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;

        //  2 새 레벨 대화 순서대로 출력
        for (int lv = state.lastSeenFavorLevel + 1; lv <= state.serverFavorLevel; lv++)
        {
            yield return StartCoroutine(PlayFavorLevel(character, state, lv));
        }

        // 현재 레벨도 아직 안 본 경우 (첫 로그인 등)
        if (state.bubbles.Count == 0 || !IsSeenDialogue(character.characterName,
            data.GetDialogueIds(state.lastSeenFavorLevel)?[0] ?? ""))
        {
            // lastSeenFavorLevel 자체를 아직 안 봤으면 출력
            if (!HasSeenAnyDialogue(character.characterName, state.lastSeenFavorLevel))
                yield return StartCoroutine(PlayFavorLevel(character, state, state.lastSeenFavorLevel));
        }

        state.isPlaying = false;
    }

    /// <summary>새 레벨만 추가 출력 (SelectCharacter에서 새 톡 감지 시 호출)</summary>
    private void PlayNewLevels(CharacterEntry character, CharacterState state)
    {
        if (state.isPlaying) return;
        state.isPlaying = true;
        state.playCoroutine = StartCoroutine(PlayNewLevelsRoutine(character, state));
    }

    private IEnumerator PlayNewLevelsRoutine(CharacterEntry character, CharacterState state)
    {
        for (int lv = state.lastSeenFavorLevel + 1; lv <= state.serverFavorLevel; lv++)
            yield return StartCoroutine(PlayFavorLevel(character, state, lv));

        state.isPlaying = false;
    }

    // FavorLevel 대화 재생

    private IEnumerator PlayFavorLevel(CharacterEntry character, CharacterState state, int favorLevel)
    {
        DialogueData data = character.dialogueData;
        if (data == null) yield break;

        List<string> ids = data.GetDialogueIds(favorLevel);
        if (ids == null || ids.Count == 0)
        {
            Debug.Log($"[DIALOGUE] {character.characterName} FavorLevel {favorLevel} 데이터 없음");
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

                    yield return new WaitUntil(() => currentCharacter == character);

                    bool chosen = false;
                    string nextId = null;

                    System.Action<string> onSelected = (selectedNextId) =>
                    {
                        chosen = true;
                        nextId = selectedNextId;

                        // 선택 결과 영구 저장
                        SaveSelectedChoice(character.characterName, dialogueId, selectedNextId);
                    };

                    state.pendingChoiceEntry = entry;
                    state.pendingOnSelected = onSelected;
                    state.isWaitingChoice = true;

                    ShowChoices(entry, onSelected);

                    yield return new WaitUntil(() => chosen);

                    state.pendingChoiceEntry = null;
                    state.pendingOnSelected = null;

                    DialogueChoice selectedChoice = GetChoiceByNextId(entry, nextId);
                    if (selectedChoice != null)
                        SpawnBubble(character, myName, selectedChoice.text, state);

                    yield return new WaitForSeconds(lineInterval);

                    if (!string.IsNullOrEmpty(nextId))
                    {
                        List<DialogueEntry> resultLines = data.GetLines(nextId);
                        foreach (var r in resultLines)
                        {
                            SpawnBubble(character, r.speaker, r.text, state);
                            yield return new WaitForSeconds(lineInterval);
                        }
                    }

                    // 이 DialogueID 완료 저장
                    MarkSeenDialogue(character.characterName, dialogueId);
                    i = ids.Count;
                    break;
                }
                else
                {
                    SpawnBubble(character, entry.speaker, entry.text, state);
                    yield return new WaitForSeconds(lineInterval);
                }
            }

            // 선택지 없는 블록 완료 저장
            if (i < ids.Count)
                MarkSeenDialogue(character.characterName, dialogueId);

            i++;
        }

        // 이 레벨 완료 -> lastSeenFavorLevel 갱신 및 저장
        if (favorLevel > state.lastSeenFavorLevel)
        {
            state.lastSeenFavorLevel = favorLevel;
            PlayerPrefs.SetInt(PrefsFavorKey + character.characterName, favorLevel);
            PlayerPrefs.Save();
        }

        Debug.Log($"[DIALOGUE] {character.characterName} FavorLevel {favorLevel} 완료");
    }

    // PlayerPrefs 저장/로드

    private void MarkSeenDialogue(string charName, string dialogueId)
    {
        PlayerPrefs.SetInt(PrefsSeenKey + charName + "_" + dialogueId, 1);
        PlayerPrefs.Save();
    }

    private bool IsSeenDialogue(string charName, string dialogueId)
    {
        return PlayerPrefs.GetInt(PrefsSeenKey + charName + "_" + dialogueId, 0) == 1;
    }

    private bool HasSeenAnyDialogue(string charName, int favorLevel)
    {
        foreach (var c in characters)
        {
            if (c == null || c.characterName != charName) continue;
            if (c.dialogueData == null) return false;

            List<string> ids = c.dialogueData.GetDialogueIds(favorLevel);
            if (ids == null) return false;

            foreach (var id in ids)
                if (IsSeenDialogue(charName, id)) return true;

            return false;
        }
        return false;
    }

    private void SaveSelectedChoice(string charName, string dialogueId, string nextId)
    {
        PlayerPrefs.SetString(PrefsSeenKey + charName + "_choice_" + dialogueId, nextId);
        PlayerPrefs.Save();
    }

    private string LoadSelectedChoice(string charName, string dialogueId)
    {
        return PlayerPrefs.GetString(PrefsSeenKey + charName + "_choice_" + dialogueId, "");
    }

    // 말풍선 생성

    /// 일반 말풍선 (lineInterval 포함, 현재 캐릭터가 아니면 숨김)
    private void SpawnBubble(CharacterEntry character, string speaker, string text, CharacterState state)
    {
        GameObject bubble = CreateBubbleObject(character, speaker, text);
        if (bubble == null) return;

        if (currentCharacter != character)
            bubble.SetActive(false);

        state.bubbles.Add(bubble);

        if (currentCharacter == character)
            StartCoroutine(ScrollToBottom());
    }

    /// 히스토리 복원용 말풍선 (즉시 생성, 현재 캐릭터가 아니면 숨김)
    private void SpawnBubbleInstant(CharacterEntry character, string speaker, string text, CharacterState state)
    {
        GameObject bubble = CreateBubbleObject(character, speaker, text);
        if (bubble == null) return;

        if (currentCharacter != character)
            bubble.SetActive(false);

        state.bubbles.Add(bubble);
    }

    private GameObject CreateBubbleObject(CharacterEntry character, string speaker, string text)
    {
        bool isMine = speaker == myName;
        GameObject prefab = isMine ? rightBubblePrefab : leftBubblePrefab;
        if (prefab == null) return null;

        GameObject bubble = Instantiate(prefab, contentRect);

        TMP_Text[] texts = bubble.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            string n = t.gameObject.name.ToLower();
            if (n.Contains("name") || n.Contains("speaker"))
                t.text = speaker;
            else
                t.text = text;
        }

        if (!isMine)
        {
            Image[] images = bubble.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.name.ToLower().Contains("icon"))
                {
                    img.sprite = character.icon != null ? character.icon : null;
                    img.gameObject.SetActive(character.icon != null);
                    break;
                }
            }
        }

        return bubble;
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

            if (currentCharacter != null &&
                states.TryGetValue(currentCharacter.characterName, out var state))
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