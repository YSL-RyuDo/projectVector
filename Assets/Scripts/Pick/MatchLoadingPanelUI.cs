using UnityEngine;

[System.Serializable]
public class MatchLoadingCharacterViewData
{
    public int characterId;
    public string characterName;
    public Sprite characterSprite;
}

public class MatchLoadingPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject rootPanel;

    [SerializeField] private MatchLoadingSlotUI[] attackSlots;
    [SerializeField] private MatchLoadingSlotUI[] defenseSlots;

    [Header("Character Table")]
    [SerializeField] private MatchLoadingCharacterViewData[] characterTable;

    [Header("Rank Table")]
    [SerializeField] private Sprite[] rankSprites;

    private MatchLoadingInfoData cachedData;

    private void Awake()
    {
        if (rootPanel == null)
            rootPanel = gameObject;
    }

    public void Show(MatchLoadingInfoData data)
    {
        cachedData = data; // 데이터 캐싱

        Debug.Log("[MATCH LOADING PANEL] Show called");

        if (rootPanel == null)
            rootPanel = gameObject;

        rootPanel.SetActive(true);
        gameObject.SetActive(true);

        ClearAll();

        Debug.Log(
            $"[MATCH LOADING DATA] roomId={data.roomId}, " +
            $"attackCount={data.attackMembers.Count}, " +
            $"defenseCount={data.defenseMembers.Count}"
        );

        for (int i = 0; i < data.attackMembers.Count && i < attackSlots.Length; i++)
        {
            MatchLoadingMemberData member = data.attackMembers[i];
            ApplySlot(attackSlots[i], member, "ATTACK", i);
        }

        for (int i = 0; i < data.defenseMembers.Count && i < defenseSlots.Length; i++)
        {
            MatchLoadingMemberData member = data.defenseMembers[i];
            ApplySlot(defenseSlots[i], member, "DEFENSE", i);
        }
    }

    public void OnMatchStart()
    {
        Debug.Log("Match Start! Load Game Scene here.");
        // SceneManager.LoadScene("GameScene");

        Debug.Log("Match Start! 맵과 캐릭터를 생성합니다.");

        if (MatchManager.Instance != null && cachedData != null)
        {
            // 매치 매니저에게 데이터를 토스하여 맵과 캐릭터 생성을 지시
            MatchManager.Instance.SetupMatch(cachedData);
        }

        // 로딩 UI 판넬 닫기
        //if (rootPanel != null) rootPanel.SetActive(false);
    }

    private void ApplySlot(
    MatchLoadingSlotUI slot,
    MatchLoadingMemberData member,
    string teamName,
    int index)
    {
        if (slot == null)
        {
            Debug.LogError($"[MATCH SLOT ERROR] {teamName}[{index}] slot is null");
            return;
        }

        MatchLoadingCharacterViewData characterData = GetCharacterData(member.characterId);
        Sprite rankSprite = GetRankSprite(member.rankImageId);

        string characterName = "";
        Sprite characterSprite = null;

        if (characterData != null)
        {
            characterName = characterData.characterName;
            characterSprite = characterData.characterSprite;
        }

        Debug.Log(
            $"[MATCH SLOT] team={teamName}, index={index}, " +
            $"slot={slot.gameObject.name}, " +
            $"nickname={member.nickname}, " +
            $"characterId={member.characterId}, " +
            $"characterName={characterName}, " +
            $"characterSprite={(characterSprite != null ? characterSprite.name : "NULL")}, " +
            $"rankImageId={member.rankImageId}, " +
            $"rankSprite={(rankSprite != null ? rankSprite.name : "NULL")}"
        );

        slot.SetData(
            characterSprite,
            characterName,
            member.nickname,
            rankSprite
        );
    }

    private void ClearAll()
    {
        if (attackSlots != null)
        {
            for (int i = 0; i < attackSlots.Length; i++)
            {
                if (attackSlots[i] != null)
                    attackSlots[i].Clear();
            }
        }

        if (defenseSlots != null)
        {
            for (int i = 0; i < defenseSlots.Length; i++)
            {
                if (defenseSlots[i] != null)
                    defenseSlots[i].Clear();
            }
        }
    }

    private MatchLoadingCharacterViewData GetCharacterData(int characterId)
    {
        if (characterTable == null)
            return null;

        for (int i = 0; i < characterTable.Length; i++)
        {
            MatchLoadingCharacterViewData data = characterTable[i];

            if (data == null)
                continue;

            if (data.characterId == characterId)
                return data;
        }

        Debug.LogError($"[MATCH CHARACTER TABLE ERROR] characterId={characterId} not found");
        return null;
    }

    private Sprite GetRankSprite(int rankImageId)
    {
        if (rankSprites == null)
            return null;

        if (rankImageId < 0 || rankImageId >= rankSprites.Length)
        {
            Debug.LogWarning($"[MATCH RANK TABLE WARNING] invalid rankImageId={rankImageId}");
            return null;
        }

        return rankSprites[rankImageId];
    }
}