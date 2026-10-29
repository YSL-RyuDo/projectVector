using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using System.Security.Principal;
using System.Text;

// Feature-specific wire handlers. Packet layouts and public methods are unchanged.
public partial class NetworkManager
{
    public void SendRegister(string id, string nick, string pw, string confirm)
    {
        byte[] buffer = new byte[4 + 20 + 20 + 64 + 64];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_REGISTER).CopyTo(buffer, 2);

        Packet.WriteFixedString(buffer, 4, id, 20);
        Packet.WriteFixedString(buffer, 24, nick, 20);
        Packet.WriteFixedString(buffer, 44, pw, 64);
        Packet.WriteFixedString(buffer, 108, confirm, 64);

        client.Send(buffer);
    }

    public void SendLogin(string id, string pw)
    {
        reconnectAccount = id.Trim();
        reconnectPassword = pw; // Process memory only; never PlayerPrefs, files or logs.
        IsAuthenticated = false;
        ResetLocalCombatReport();
        ResetSocialState();
        myUserId = id.Trim();
        equippedWeaponSkinsByCharacter.Clear();
        ClearLobbyRoomChatContext();

        byte[] buffer = new byte[4 + 20 + 64];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOGIN).CopyTo(buffer, 2);

        Packet.WriteFixedString(buffer, 4, id, 20);
        Packet.WriteFixedString(buffer, 24, pw, 64);

        client.Send(buffer);
    }

    public void SendLobbyEnter()
    {
        byte[] buffer = new byte[4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOBBY_ENTER).CopyTo(buffer, 2);

        client.Send(buffer);
    }

    public void SendLobbyChat(string message)
    {
        byte[] buffer = new byte[4 + 128];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOBBY_CHAT).CopyTo(buffer, 2);

        Packet.WriteFixedString(buffer, 4, message, 128);
        client.Send(buffer);
    }

    public void SendRoomChat(string message)
    {
        if (lobbyRoomChatKind == LobbyRoomChatKind.None)
            return;

        byte[] buffer = new byte[4 + 128];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_ROOM_CHAT).CopyTo(buffer, 2);

        Packet.WriteFixedString(buffer, 4, message, 128);
        client.Send(buffer);
    }

    public void SendFriendChat(uint targetPlayerId, string message)
    {
        if (targetPlayerId == 0 || targetPlayerId == myPlayerId)
            return;

        byte[] buffer = new byte[4 + 4 + 128];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_FRIEND_CHAT).CopyTo(buffer, 2);
        BitConverter.GetBytes(targetPlayerId).CopyTo(buffer, 4);

        Packet.WriteFixedString(buffer, 8, message, 128);
        client.Send(buffer);
    }

    public void SendFriendOverviewRequest()
    {
        SendHeaderOnly(PacketType.PKT_CS_FRIEND_OVERVIEW_REQUEST);
    }

    public void SendFriendPresenceSet(FriendPresenceMode mode)
    {
        byte[] buffer = new byte[5];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_FRIEND_PRESENCE_SET)
            .CopyTo(buffer, 2);
        buffer[4] = (byte)mode;
        client.Send(buffer);
    }

    public void SendFriendSearch(string query)
    {
        if (!IsConnected)
        {
            Debug.LogWarning("[FRIEND SEARCH SEND BLOCKED] server is not connected");
            return;
        }

        if (!IsAuthenticated)
        {
            Debug.LogWarning("[FRIEND SEARCH SEND BLOCKED] login is not complete");
            return;
        }

        byte[] buffer = new byte[4 + 40];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_FRIEND_SEARCH)
            .CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, query, 40);
        Debug.Log($"[FRIEND SEARCH SEND] type=70 query='{query}' bytes={buffer.Length}");
        client.Send(buffer);
    }

    public void SendFriendRecommendRequest()
    {
        SendHeaderOnly(PacketType.PKT_CS_FRIEND_RECOMMEND_REQUEST);
    }

    public void SendFriendOperation(
        FriendOperation operation,
        string targetUserId)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            return;

        byte[] buffer = new byte[4 + 1 + 20];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_FRIEND_OPERATION)
            .CopyTo(buffer, 2);
        buffer[4] = (byte)operation;
        Packet.WriteFixedString(buffer, 5, targetUserId, 20);
        client.Send(buffer);
    }

    public void SendFriendRequestListRequest()
    {
        SendHeaderOnly(PacketType.PKT_CS_FRIEND_REQUEST_LIST_REQUEST);
    }

    public void SendFriendRecentRequest()
    {
        SendHeaderOnly(PacketType.PKT_CS_FRIEND_RECENT_REQUEST);
    }

    public void SendFriendMemoSet(string targetUserId, string memo)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            return;

        byte[] buffer = new byte[4 + 20 + 32];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_FRIEND_MEMO_SET)
            .CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, targetUserId, 20);
        Packet.WriteFixedString(buffer, 24, memo ?? "", 32);
        client.Send(buffer);
    }

    public void SendProfileRequest(string targetUserId = "")
    {
        byte[] buffer = new byte[24];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PROFILE_REQUEST).CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, targetUserId ?? string.Empty, 20);
        client.Send(buffer);
    }

    public void SendProfilePrivacySet(bool enabled)
    {
        byte[] buffer = new byte[5];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PROFILE_PRIVACY_SET).CopyTo(buffer, 2);
        buffer[4] = enabled ? (byte)1 : (byte)0;
        client.Send(buffer);
    }

    public void SendProfileLike(string targetUserId)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
            return;
        byte[] buffer = new byte[24];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PROFILE_LIKE).CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, targetUserId, 20);
        client.Send(buffer);
    }
    public void SendProfileBattleStatsSet(IReadOnlyList<int> battleStatIds)
    {
        if (battleStatIds == null || battleStatIds.Count != 7) return;
        byte[] buffer = new byte[11];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_PROFILE_BATTLE_STATS_SET).CopyTo(buffer, 2);
        for (int i = 0; i < 7; ++i)
        {
            if (battleStatIds[i] < 0 || battleStatIds[i] > 14) return;
            buffer[4 + i] = (byte)battleStatIds[i];
        }
        client.Send(buffer);
    }

    public void SendLobbyCharacterSelect(int modelId)
    {
        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOBBY_CHARACTER_SELECT).CopyTo(buffer, 2);
        BitConverter.GetBytes(modelId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    public void SendLobbyBackgroundSelect(int backgroundId)
    {
        byte[] buffer = new byte[4 + 4];

        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOBBY_BACKGROUND_SELECT).CopyTo(buffer, 2);
        BitConverter.GetBytes(backgroundId).CopyTo(buffer, 4);

        client.Send(buffer);
    }

    public void SendLobbyCharacterList()
    {

        byte[] bytes = new byte[4];

        BitConverter.GetBytes((ushort)bytes.Length).CopyTo(bytes, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LOBBY_CHARACTER_LIST).CopyTo(bytes, 2);

        client.Send(bytes);
    }

    public void SendWeaponSkinLoadoutRequest(int characterId)
    {
        byte[] buffer = new byte[8];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_WEAPON_SKIN_LOADOUT_REQUEST).CopyTo(buffer, 2);
        BitConverter.GetBytes(characterId).CopyTo(buffer, 4);
        client.Send(buffer);
    }

    public void SendWeaponSkinSelect(int characterId, int weaponId, int skinId)
    {
        byte[] buffer = new byte[16];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_WEAPON_SKIN_SELECT).CopyTo(buffer, 2);
        BitConverter.GetBytes(characterId).CopyTo(buffer, 4);
        BitConverter.GetBytes(weaponId).CopyTo(buffer, 8);
        BitConverter.GetBytes(skinId).CopyTo(buffer, 12);
        client.Send(buffer);
    }

    public void SendCareerDetailRequest(
        string targetUserId,
        int seasonId,
        int modeType,
        int characterId)
    {
        byte[] buffer = new byte[33];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes(
            (ushort)PacketType.PKT_CS_CAREER_DETAIL_REQUEST
        ).CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, targetUserId ?? string.Empty, 20);
        BitConverter.GetBytes(seasonId).CopyTo(buffer, 24);
        buffer[28] = (byte)Mathf.Clamp(modeType, 0, 2);
        BitConverter.GetBytes(Mathf.Clamp(characterId, 0, 3)).CopyTo(buffer, 29);
        client.Send(buffer);
    }

    public void SendChipCraftRequest(string targetUserId = "")
    {
        byte[] buffer = new byte[24];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_CHIPCRAFT_REQUEST).CopyTo(buffer, 2);
        Packet.WriteFixedString(buffer, 4, targetUserId ?? string.Empty, 20);
        client.Send(buffer);
    }

    public void SendLevelRewardRequest() => SendHeaderOnly(PacketType.PKT_CS_LEVEL_REWARD_REQUEST);
    public void SendEventPass(int operation, int index, long week)
    {
        byte[] buffer = new byte[20];
        BitConverter.GetBytes((ushort)20).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)334).CopyTo(buffer, 2);
        BitConverter.GetBytes(operation).CopyTo(buffer, 4);
        BitConverter.GetBytes(index).CopyTo(buffer, 8);
        BitConverter.GetBytes(week).CopyTo(buffer, 12);
        client.Send(buffer);
    }

    public void SendLevelRewardClaim(int targetLevel)
    {
        byte[] buffer = new byte[8];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_LEVEL_REWARD_CLAIM).CopyTo(buffer, 2);
        BitConverter.GetBytes(targetLevel).CopyTo(buffer, 4);
        client.Send(buffer);
    }

    public void SendHonorRequest(int page = 1)
    {
        byte[] buffer = new byte[5];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_HONOR_REQUEST).CopyTo(buffer, 2);
        buffer[4] = (byte)Mathf.Clamp(page, 1, 5);
        client.Send(buffer);
    }

    public void SendShopRequest(byte operation, int currency, int count, int itemId, ulong requestId, int page)
    {
        byte[] buffer = new byte[24];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)340).CopyTo(buffer, 2);
        buffer[4] = operation; buffer[5] = (byte)currency;
        BitConverter.GetBytes((ushort)count).CopyTo(buffer, 6);
        BitConverter.GetBytes(itemId).CopyTo(buffer, 8);
        BitConverter.GetBytes(requestId).CopyTo(buffer, 12);
        BitConverter.GetBytes(page).CopyTo(buffer, 20);
        client?.Send(buffer);
    }

    public void SendHonorClaim(int grade)
    {
        byte[] buffer = new byte[8];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_HONOR_CLAIM).CopyTo(buffer, 2);
        BitConverter.GetBytes(grade).CopyTo(buffer, 4);
        client.Send(buffer);
    }

    public void SendChipCraftSave(ChipCraftPresetData preset)
    {
        if (preset == null || preset.medals.Count > 3) return;
        byte[] buffer = new byte[61];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)PacketType.PKT_CS_CHIPCRAFT_SAVE).CopyTo(buffer, 2);
        WriteChipCraftPreset(buffer, 4, preset);
        client.Send(buffer);
    }

    private static void WriteChipCraftPreset(byte[] buffer, int offset, ChipCraftPresetData preset)
    {
        buffer[offset++] = (byte)preset.cardId;
        buffer[offset++] = (byte)preset.caseId;
        buffer[offset++] = (byte)preset.medals.Count;
        for (int i = 0; i < 3; ++i)
        {
            ChipCraftMedalData medal = i < preset.medals.Count ? preset.medals[i] : new ChipCraftMedalData();
            buffer[offset++] = (byte)medal.medalId;
            BitConverter.GetBytes(medal.x).CopyTo(buffer, offset); offset += 4;
            BitConverter.GetBytes(medal.y).CopyTo(buffer, offset); offset += 4;
            BitConverter.GetBytes(medal.rotation).CopyTo(buffer, offset); offset += 4;
            BitConverter.GetBytes(medal.scale).CopyTo(buffer, offset); offset += 4;
            buffer[offset++] = medal.flipped ? (byte)1 : (byte)0;
        }
    }

    private static ChipCraftPresetData ReadChipCraftPreset(byte[] body, ref int offset)
    {
        ChipCraftPresetData preset = new();
        preset.cardId = body[offset++];
        preset.caseId = body[offset++];
        int count = Mathf.Min(body[offset++], (byte)3);
        for (int i = 0; i < 3; ++i)
        {
            ChipCraftMedalData medal = new();
            medal.medalId = body[offset++];
            medal.x = BitConverter.ToSingle(body, offset); offset += 4;
            medal.y = BitConverter.ToSingle(body, offset); offset += 4;
            medal.rotation = BitConverter.ToSingle(body, offset); offset += 4;
            medal.scale = BitConverter.ToSingle(body, offset); offset += 4;
            medal.flipped = body[offset++] != 0;
            if (i < count) preset.medals.Add(medal);
        }
        return preset;
    }
    public void SendCareerDataRequests(int matchFilterType)
    {
        SendHeaderOnly(PacketType.PKT_CS_CAREER_OVERVIEW_REQUEST);
        SendCareerRankingRequest(0, 0, 1, string.Empty);
        SendCareerMatchHistoryRequest(matchFilterType);
        SendHeaderOnly(PacketType.PKT_CS_CAREER_INVENTORY_REQUEST);
    }

    public void SendCareerMatchHistoryRequest(int filterType)
    {
        byte[] buffer = new byte[5];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes(
            (ushort)PacketType.PKT_CS_CAREER_MATCH_HISTORY_REQUEST
        ).CopyTo(buffer, 2);
        buffer[4] = (byte)Mathf.Clamp(filterType, 0, 2);
        client.Send(buffer);
    }

    public void SendCareerRankingRequest(
        int rankingType,
        int characterId,
        int page,
        string searchQuery)
    {
        const int QuerySize = 64;
        byte[] buffer = new byte[4 + 1 + 4 + 1 + QuerySize];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes(
            (ushort)PacketType.PKT_CS_CAREER_RANKING_REQUEST
        ).CopyTo(buffer, 2);
        buffer[4] = (byte)(rankingType == 1 ? 1 : 0);
        BitConverter.GetBytes(Math.Max(0, characterId)).CopyTo(buffer, 5);
        buffer[9] = (byte)Math.Clamp(page, 1, 255);

        string query = searchQuery ?? string.Empty;
        char[] queryChars = query.ToCharArray();
        byte[] queryBytes = new byte[QuerySize - 1];
        Encoding.UTF8.GetEncoder().Convert(
            queryChars,
            0,
            queryChars.Length,
            queryBytes,
            0,
            queryBytes.Length,
            true,
            out _,
            out int bytesUsed,
            out _
        );
        Array.Copy(queryBytes, 0, buffer, 10, bytesUsed);
        client.Send(buffer);
    }

    void HandleRegister(byte[] body)
    {
        byte result = body[0];

        bool success = (result == (byte)RegisterResult.SUCCESS);

        Debug.Log($"[REGISTER RESULT] success={success}");

        loginUI?.OnRegisterResult(success);

        if (!success)
        {
            string msg = result switch
            {
                (byte)RegisterResult.ID_DUPLICATE => "아이디 중복",
                (byte)RegisterResult.NICK_DUPLICATE => "닉네임 중복",
                (byte)RegisterResult.PASSWORD_MISMATCH => "비밀번호 불일치",
                (byte)RegisterResult.INVALID_INPUT => "입력 오류",
                _ => "회원가입 실패"
            };

            loginUI?.ShowError(msg);
        }
    }

    void HandleLogin(byte[] body)
    {
        byte result = body[0];

        bool success = (result == (byte)LoginResult.SUCCESS);
        IsAuthenticated = success;

        if (ReconnectBusy) return; // Automatic reauthentication must not show Login UI.

        Debug.Log($"[LOGIN RESULT] success={success}");

        loginUI?.OnLoginResult(success);

        if (!success)
        {
            myUserId = "";

            string msg = result switch
            {
                (byte)LoginResult.NO_ID => "아이디 없음",
                (byte)LoginResult.WRONG_PASSWORD => "비밀번호 틀림",
                (byte)LoginResult.INVALID_INPUT => "입력 오류",
                (byte)LoginResult.ALREADY_LOGIN => "이미 로그인된 계정입니다.",
                _ => "로그인 실패"
            };

            loginUI?.ShowError(msg);
        }
    }
    void HandleLobbyEnter(byte[] body)
    {
        if (body == null || body.Length < 52)
        {
            Debug.LogWarning("[CLIENT LOBBY ENTER] invalid packet size");
            return;
        }

        string nickname = Packet.ReadFixedString(body, 0, 20);

        int level = BitConverter.ToInt32(body, 20);
        int currentExp = BitConverter.ToInt32(body, 24);
        int requiredExp = BitConverter.ToInt32(body, 28);

        int playerIconId = BitConverter.ToInt32(body, 32);
        int lobbyCharacterModelId = BitConverter.ToInt32(body, 36);
        int lobbyBackgroundId = BitConverter.ToInt32(body, 40);
        int freeCurrency = BitConverter.ToInt32(body, 44);
        int victoryCurrency = BitConverter.ToInt32(body, 48);
        if (body.Length >= 56)
            myPlayerId = BitConverter.ToUInt32(body, 52);

        Debug.Log($"[CLIENT LOBBY ENTER] nickname={nickname}, level={level}, exp={currentExp}/{requiredExp}, icon={playerIconId}, char={lobbyCharacterModelId}, bg={lobbyBackgroundId}, coin0={freeCurrency}, coin1={victoryCurrency}");

        MoneyManager.Instance?.SetServerBalances(
            freeCurrency,
            victoryCurrency
        );

        profilePanelController?.OnRecvLobbyEnter(
            nickname,
            level,
            currentExp,
            requiredExp,
            playerIconId,
            lobbyCharacterModelId,
            lobbyBackgroundId
        );

        SendWeaponSkinLoadoutRequest(lobbyCharacterModelId);

        SendFavorLevelRequest();        // 로비 씬 입장 마다 호감도 레벨 불러오기
    }

    private void HandleWeaponSkinLoadout(byte[] body)
    {
        if (body == null || body.Length < 4 + 2 + WeaponSkinWeaponCount * 8)
        {
            Debug.LogWarning("[WEAPON SKIN LOADOUT] invalid packet size");
            return;
        }

        int offset = 0;
        int characterId = BitConverter.ToInt32(body, offset);
        offset += 4;

        if (characterId < 0 || characterId >= WeaponSkinCharacterCount)
        {
            Debug.LogWarning($"[WEAPON SKIN LOADOUT] invalid characterId={characterId}");
            return;
        }

        ushort count = BitConverter.ToUInt16(body, offset);
        offset += 2;

        if (!equippedWeaponSkinsByCharacter.TryGetValue(
                characterId,
                out Dictionary<int, int> loadout))
        {
            loadout = new Dictionary<int, int>();
            equippedWeaponSkinsByCharacter[characterId] = loadout;
        }

        loadout.Clear();

        for (int i = 0; i < WeaponSkinWeaponCount; i++)
        {
            int weaponId = BitConverter.ToInt32(body, offset);
            offset += 4;
            int skinId = BitConverter.ToInt32(body, offset);
            offset += 4;

            if (i < count && weaponId >= 0 && weaponId < WeaponSkinWeaponCount)
                loadout[weaponId] = skinId;
        }

        WeaponSkinLoadoutChanged?.Invoke(characterId);
        Debug.Log(
            $"[WEAPON SKIN LOADOUT] characterId={characterId}, received count={loadout.Count}"
        );
    }

    private void HandleWeaponSkinSelectResult(byte[] body)
    {
        if (body == null || body.Length < 13)
            return;

        bool success = body[0] != 0;
        int characterId = BitConverter.ToInt32(body, 1);
        int weaponId = BitConverter.ToInt32(body, 5);
        int skinId = BitConverter.ToInt32(body, 9);

        if (success)
        {
            if (!equippedWeaponSkinsByCharacter.TryGetValue(
                    characterId,
                    out Dictionary<int, int> loadout))
            {
                loadout = new Dictionary<int, int>();
                equippedWeaponSkinsByCharacter[characterId] = loadout;
            }

            loadout[weaponId] = skinId;
        }

        // On failure this reapplies the last confirmed value to the lobby UI.
        WeaponSkinLoadoutChanged?.Invoke(characterId);
        Debug.Log(
            $"[WEAPON SKIN SELECT] success={success}, characterId={characterId}, " +
            $"weaponId={weaponId}, skinId={skinId}"
        );
    }

    private void HandleProfile(byte[] body)
    {
        const int ExpectedSize = 711;
        if (body == null || body.Length < ExpectedSize)
        {
            Debug.LogWarning($"[PROFILE] invalid body size={body?.Length ?? 0}");
            return;
        }
        int offset = 0;
        if (body[offset++] != 0) { MyPagePanelController.Instance?.ShowLoadFailure(); return; }
        MyPageProfileData data = new();
        data.isSelf = body[offset++] != 0;
        data.privacyEnabled = body[offset++] != 0;
        data.userId = Packet.ReadFixedString(body, offset, 20); offset += 20;
        data.nickname = Packet.ReadFixedString(body, offset, 20); offset += 20;
        data.level = BitConverter.ToInt32(body, offset); offset += 4;
        data.playerIconId = BitConverter.ToInt32(body, offset); offset += 4;
        data.logoBorderId = BitConverter.ToInt32(body, offset); offset += 4;
        data.joinDate = Packet.ReadFixedString(body, offset, 16); offset += 16;
        data.region = Packet.ReadFixedString(body, offset, 20); offset += 20;
        data.birthday = Packet.ReadFixedString(body, offset, 16); offset += 16;
        data.likeCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.seasonId = BitConverter.ToInt32(body, offset); offset += 4;
        data.currentRankImageId = BitConverter.ToInt32(body, offset); offset += 4;
        data.currentTierName = Packet.ReadFixedString(body, offset, 32); offset += 32;
        data.seasonHighestRankImageId = BitConverter.ToInt32(body, offset); offset += 4;
        data.allTimeHighestRankImageId = BitConverter.ToInt32(body, offset); offset += 4;
        data.allTimeHighestSeasonId = BitConverter.ToInt32(body, offset); offset += 4;
        data.playCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.wins = BitConverter.ToInt32(body, offset); offset += 4;
        data.roundsPlayed = BitConverter.ToInt32(body, offset); offset += 4;
        data.totalKills = BitConverter.ToInt32(body, offset); offset += 4;
        data.totalAssists = BitConverter.ToInt32(body, offset); offset += 4;
        data.totalDamage = BitConverter.ToInt32(body, offset); offset += 4;
        data.mvpCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.headshotKills = BitConverter.ToInt32(body, offset); offset += 4;
        data.spikePlants = BitConverter.ToInt32(body, offset); offset += 4;
        data.spikeDefuses = BitConverter.ToInt32(body, offset); offset += 4;
        data.weaponSkinOwnedCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.weaponSkinTotalCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.characterSkinOwnedCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.characterSkinTotalCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.battleStatIds = new int[7];
        for (int i = 0; i < data.battleStatIds.Length; ++i)
            data.battleStatIds[i] = body[offset++];
        for (int i = 0; i < 4; ++i)
        {
            MyPageCharacterData character = new();
            character.characterId = BitConverter.ToInt32(body, offset); offset += 4;
            character.matches = BitConverter.ToInt32(body, offset); offset += 4;
            character.wins = BitConverter.ToInt32(body, offset); offset += 4;
            character.maxKills = BitConverter.ToInt32(body, offset); offset += 4;
            character.owned = body[offset++] != 0;
            data.characters.Add(character);
        }
        int visitorCount = Mathf.Min(body[offset++], (byte)8);
        for (int i = 0; i < 8; ++i)
        {
            MyPageVisitorData visitor = new();
            visitor.userId = Packet.ReadFixedString(body, offset, 20); offset += 20;
            visitor.nickname = Packet.ReadFixedString(body, offset, 20); offset += 20;
            visitor.playerIconId = BitConverter.ToInt32(body, offset); offset += 4;
            visitor.visitedAtUnix = BitConverter.ToInt64(body, offset); offset += 8;
            if (i < visitorCount) data.visitors.Add(visitor);
        }
        MyPagePanelController.Instance?.ApplyProfile(data);
    }

    private void HandleProfilePrivacySetResult(byte[] body)
    {
        if (body == null || body.Length < 2) return;
        MyPagePanelController.Instance?.ApplyPrivacyResult(body[0] == 0, body[1] != 0);
    }

    private void HandleProfileLikeResult(byte[] body)
    {
        if (body == null || body.Length < 25) return;
        bool success = body[0] == 0;
        string targetUserId = Packet.ReadFixedString(body, 1, 20);
        int likeCount = BitConverter.ToInt32(body, 21);
        MyPagePanelController.Instance?.ApplyLikeResult(success, targetUserId, likeCount);
    }
    private void HandleProfileBattleStatsSetResult(byte[] body)
    {
        if (body == null || body.Length < 8) return;
        int[] battleStatIds = new int[7];
        for (int i = 0; i < battleStatIds.Length; ++i) battleStatIds[i] = body[1 + i];
        MyPagePanelController.Instance?.ApplyBattleStatsResult(body[0] == 0, battleStatIds);
    }

    private void HandleCareerOverview(byte[] body)
    {
        const int ExpectedBodySize = 124;
        if (body == null || body.Length < ExpectedBodySize)
        {
            Debug.LogWarning("[CAREER OVERVIEW] invalid packet size");
            return;
        }

        int offset = 0;
        CareerOverviewData data = new();
        data.seasonId = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.seasonName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;
        data.seasonEndUnix = BitConverter.ToInt64(body, offset);
        offset += 8;
        data.remainingSeasonDays = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.tierId = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.tierName = Packet.ReadFixedString(body, offset, 32);
        offset += 32;
        data.rankImageId = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.tierMinPoints = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.tierMaxPoints = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.rankPoints = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.rankedMatches = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.wins = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.roundsPlayed = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.kills = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.totalDamage = BitConverter.ToInt32(body, offset);
        offset += 4;
        data.mvpCount = BitConverter.ToInt32(body, offset);

        CareerPanelController panel = FindFirstObjectByType<CareerPanelController>();
        panel?.ApplyOverview(data);
    }

    private void HandleCareerDetail(byte[] body)
    {
        const int ExpectedBodySize = 492;
        const int SeasonEntrySize = 37;
        const int SeasonCapacity = 8;
        const int CharacterCapacity = 4;
        if (body == null || body.Length < ExpectedBodySize)
        {
            Debug.LogWarning("[CAREER DETAIL] invalid packet size");
            return;
        }

        int offset = 0;
        CareerDetailData data = new();
        data.success = body[offset++] == 0;
        data.hasData = body[offset++] != 0;
        data.seasonId = BitConverter.ToInt32(body, offset); offset += 4;
        data.modeType = body[offset++];
        data.selectedCharacterId = BitConverter.ToInt32(body, offset); offset += 4;
        int seasonCount = Math.Min((int)body[offset++], SeasonCapacity);

        for (int i = 0; i < SeasonCapacity; ++i)
        {
            CareerDetailSeasonData season = new();
            season.seasonId = BitConverter.ToInt32(body, offset); offset += 4;
            season.seasonName = Packet.ReadFixedString(body, offset, 32); offset += 32;
            season.isActive = body[offset++] != 0;
            if (i < seasonCount) data.seasons.Add(season);
        }

        for (int i = 0; i < CharacterCapacity; ++i)
        {
            CareerDetailCharacterData character = new();
            character.characterId = BitConverter.ToInt32(body, offset); offset += 4;
            character.performanceScore = BitConverter.ToInt32(body, offset); offset += 4;
            character.matches = BitConverter.ToInt32(body, offset); offset += 4;
            character.wins = BitConverter.ToInt32(body, offset); offset += 4;
            character.kills = BitConverter.ToInt32(body, offset); offset += 4;
            character.deaths = BitConverter.ToInt32(body, offset); offset += 4;
            character.maxKills = BitConverter.ToInt32(body, offset); offset += 4;
            character.maxDamage = BitConverter.ToInt32(body, offset); offset += 4;
            data.characters.Add(character);
        }

        data.playCount = BitConverter.ToInt32(body, offset); offset += 4;
        data.wins = BitConverter.ToInt32(body, offset); offset += 4;
        data.roundsPlayed = BitConverter.ToInt32(body, offset); offset += 4;
        data.totalDamage = BitConverter.ToInt32(body, offset); offset += 4;
        data.kills = BitConverter.ToInt32(body, offset); offset += 4;
        data.deaths = BitConverter.ToInt32(body, offset); offset += 4;
        data.assists = BitConverter.ToInt32(body, offset); offset += 4;
        data.headshotKills = BitConverter.ToInt32(body, offset); offset += 4;
        data.meleeKills = BitConverter.ToInt32(body, offset); offset += 4;
        data.spikePlants = BitConverter.ToInt32(body, offset); offset += 4;
        data.spikeDefuses = BitConverter.ToInt32(body, offset); offset += 4;
        data.maxAssists = BitConverter.ToInt32(body, offset); offset += 4;
        data.maxRoundDamageHundredths = BitConverter.ToInt32(body, offset); offset += 4;
        data.maxRoundKillsHundredths = BitConverter.ToInt32(body, offset);

        MyPageCareerDataController panel =
            FindFirstObjectByType<MyPageCareerDataController>(FindObjectsInactive.Include);
        panel?.ApplyData(data);
    }

    private void HandleChipCraft(byte[] body)
    {
        if (body == null || body.Length < 82 || body[0] != 0) return;
        int offset = 1;
        ChipCraftData data = new();
        data.isSelf = body[offset++] != 0;
        data.userId = Packet.ReadFixedString(body, offset, 20); offset += 20;
        data.ownedCardMask = body[offset++];
        data.ownedCaseMask = body[offset++];
        data.ownedMedalMask = body[offset++];
        data.preset = ReadChipCraftPreset(body, ref offset);
        MyPagePanelController.Instance?.ApplyChipCraft(data);
    }

    private void HandleChipCraftSaveResult(byte[] body)
    {
        if (body == null || body.Length < 58) return;
        int offset = 1;
        ChipCraftPresetData preset = body[0] == 0 ? ReadChipCraftPreset(body, ref offset) : null;
        // Apply the authoritative appearance even if the profile page was closed
        // or switched to another user while the save response was in flight.
        if (preset != null)
        {
            FindFirstObjectByType<ProfilePanelController>(FindObjectsInactive.Include)?.ApplyPlayerIcon(preset.cardId);
            FindFirstObjectByType<GamePanelController>(FindObjectsInactive.Include)?.RefreshLocalChipCraft(preset);
        }
        MyPagePanelController.Instance?.ApplyChipCraftSaveResult(body[0] == 0, preset);
    }

    private void HandleLevelRewardState(byte[] body)
    {
        if (body == null || body.Length < 25) return;
        MyPageLevelRewardController panel = FindFirstObjectByType<MyPageLevelRewardController>(FindObjectsInactive.Include);
        panel?.ApplyState(body[0] == 0,
            BitConverter.ToInt32(body, 1), BitConverter.ToInt32(body, 5),
            BitConverter.ToInt32(body, 9), BitConverter.ToInt32(body, 13));
        if (body[0] == 0)
            MoneyManager.Instance?.SetServerBalances(BitConverter.ToInt32(body, 17), BitConverter.ToInt32(body, 21));
    }

    private void HandleLevelRewardClaimResult(byte[] body)
    {
        if (body == null || body.Length < 21) return;
        MyPageLevelRewardController panel = FindFirstObjectByType<MyPageLevelRewardController>(FindObjectsInactive.Include);
        panel?.ApplyClaim(body[0] == 0, BitConverter.ToInt32(body, 1),
            BitConverter.ToInt32(body, 5), BitConverter.ToInt32(body, 9));
        if (body[0] == 0)
            MoneyManager.Instance?.SetServerBalances(BitConverter.ToInt32(body, 13), BitConverter.ToInt32(body, 17));
    }

    private void HandleHonorState(byte[] body)
    {
        if (body == null || body.Length < 20) return;
        int offset = 0;
        bool success = body[offset++] == 0;
        int score = BitConverter.ToInt32(body, offset); offset += 4;
        int claimed = BitConverter.ToInt32(body, offset); offset += 4;
        int free = BitConverter.ToInt32(body, offset); offset += 4;
        int victory = BitConverter.ToInt32(body, offset); offset += 4;
        int page = body[offset++], pageCount = body[offset++], count = body[offset++];
        if (count > 10 || body.Length < 20 + count * 13) return;
        var changes = new System.Collections.Generic.List<HonorChangeData>(count);
        for (int index = 0; index < count; ++index)
        {
            long time = BitConverter.ToInt64(body, offset); offset += 8;
            int delta = BitConverter.ToInt32(body, offset); offset += 4;
            byte reason = body[offset++];
            changes.Add(new HonorChangeData(time, delta, reason));
        }
        FindFirstObjectByType<MyPageHonorController>(FindObjectsInactive.Include)
            ?.ApplyState(success, score, claimed, page, pageCount, changes);
        if (success) MoneyManager.Instance?.SetServerBalances(free, victory);
    }

    private void HandleHonorClaimResult(byte[] body)
    {
        if (body == null || body.Length < 21) return;
        bool success = body[0] == 0;
        int grade = BitConverter.ToInt32(body, 1);
        int freeAward = BitConverter.ToInt32(body, 5);
        int victoryAward = BitConverter.ToInt32(body, 9);
        FindFirstObjectByType<MyPageHonorController>(FindObjectsInactive.Include)
            ?.ApplyClaim(success, grade, freeAward, victoryAward);
        if (success)
            MoneyManager.Instance?.SetServerBalances(
                BitConverter.ToInt32(body, 13), BitConverter.ToInt32(body, 17));
    }
    private void HandleCareerRanking(byte[] body)
    {
        const int EntrySize = 100;
        const int HeaderSize = 12;
        const int MaxEntries = 10;
        if (body == null || body.Length < HeaderSize)
            return;

        int offset = 0;
        CareerRankingPageData pageData = new();
        pageData.rankingType = body[offset++];
        pageData.characterId = BitConverter.ToInt32(body, offset);
        offset += 4;
        pageData.page = body[offset++];
        pageData.totalPages = Math.Max(1, (int)body[offset++]);
        pageData.totalEntries = BitConverter.ToInt32(body, offset);
        offset += 4;
        int count = Math.Min((int)body[offset++], MaxEntries);

        if (body.Length < HeaderSize + count * EntrySize)
        {
            Debug.LogWarning("[CAREER RANKING] invalid packet size");
            return;
        }

        for (int i = 0; i < count; ++i)
        {
            CareerRankingEntryData entry = new();
            entry.position = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.userId = Packet.ReadFixedString(body, offset, 20);
            offset += 20;
            entry.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;
            entry.playerIconId = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.tierId = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.tierName = Packet.ReadFixedString(body, offset, 32);
            offset += 32;
            entry.rankImageId = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.rankPoints = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.rankedMatches = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.wins = BitConverter.ToInt32(body, offset);
            offset += 4;
            pageData.entries.Add(entry);
        }

        CareerPanelController panel = FindFirstObjectByType<CareerPanelController>();
        panel?.ApplyRanking(pageData);
    }

    private void HandleCareerMatchHistory(byte[] body)
    {
        const int PlayerEntrySize = 78;
        const int PlayersPerMatch = 6;
        const int MatchEntrySize = 57 + PlayerEntrySize * PlayersPerMatch;
        const int MaxMatches = 5;

        if (body == null || body.Length < 2)
            return;

        int filterType = Math.Min((int)body[0], 2);
        int count = Math.Min((int)body[1], MaxMatches);
        if (body.Length < 2 + count * MatchEntrySize)
        {
            Debug.LogWarning("[CAREER MATCH HISTORY] invalid packet size");
            return;
        }

        int offset = 2;
        List<CareerMatchData> matches = new(count);

        for (int i = 0; i < count; ++i)
        {
            CareerMatchData match = new();
            match.matchId = BitConverter.ToUInt64(body, offset);
            offset += 8;
            match.mapName = Packet.ReadFixedString(body, offset, 32);
            offset += 32;
            match.startedAtUnix = BitConverter.ToInt64(body, offset);
            offset += 8;
            match.durationSeconds = BitConverter.ToInt32(body, offset);
            offset += 4;
            match.scoreA = body[offset++];
            match.scoreB = body[offset++];
            match.winnerTeam = body[offset++];
            match.myTeam = body[offset++];
            int playerCount = Math.Min((int)body[offset++], PlayersPerMatch);

            for (int playerIndex = 0; playerIndex < PlayersPerMatch; ++playerIndex)
            {
                CareerMatchPlayerData player = new();
                player.userId = Packet.ReadFixedString(body, offset, 20);
                offset += 20;
                player.nickname = Packet.ReadFixedString(body, offset, 20);
                offset += 20;
                player.team = body[offset++];
                player.characterId = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.combatScore = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.attackScore = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.skillScore = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.teamworkScore = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.kills = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.deaths = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.assists = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.damage = BitConverter.ToInt32(body, offset);
                offset += 4;
                player.isMvp = body[offset++] != 0;

                if (playerIndex < playerCount)
                    match.players.Add(player);
            }

            matches.Add(match);
        }

        CareerPanelController panel = FindFirstObjectByType<CareerPanelController>();
        panel?.ApplyMatchHistory(filterType, matches);
    }

    private void HandleCareerInventory(byte[] body)
    {
        const int EntrySize = 148;
        const int MaxEntries = 20;
        if (body == null || body.Length < 1)
            return;

        int count = Math.Min((int)body[0], MaxEntries);
        if (body.Length < 1 + count * EntrySize)
        {
            Debug.LogWarning("[CAREER INVENTORY] invalid packet size");
            return;
        }

        int offset = 1;
        List<CareerInventoryEntryData> entries = new(count);

        for (int i = 0; i < count; ++i)
        {
            CareerInventoryEntryData entry = new();
            entry.itemId = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.itemName = Packet.ReadFixedString(body, offset, 32);
            offset += 32;
            entry.description = Packet.ReadFixedString(body, offset, 96);
            offset += 96;
            entry.iconId = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.quantity = BitConverter.ToInt32(body, offset);
            offset += 4;
            entry.acquiredAtUnix = BitConverter.ToInt64(body, offset);
            offset += 8;
            entries.Add(entry);
        }

        CareerPanelController panel = FindFirstObjectByType<CareerPanelController>();
        panel?.ApplyInventory(entries);
    }

    void HandleLobbyChat(byte[] body)
    {
        Debug.Log($"[CLIENT HANDLE LOBBY CHAT] bodyLen={body.Length}");

        uint playerId = BitConverter.ToUInt32(body, 0);
        string nickname = Packet.ReadFixedString(body, 4, 20);
        string message = Packet.ReadFixedString(body, 24, 128);

        Debug.Log($"[CLIENT CHAT PARSED] playerId={playerId} nickname={nickname} message={message}");

        chatPanelController?.OnRecvLobbyChat(playerId, nickname, message);
    }

    void HandleRoomChat(byte[] body)
    {
        if (body == null || body.Length < 156)
            return;

        uint roomId = BitConverter.ToUInt32(body, 0);
        uint playerId = BitConverter.ToUInt32(body, 4);
        string nickname = Packet.ReadFixedString(body, 8, 20);
        string message = Packet.ReadFixedString(body, 28, 128);

        chatPanelController?.OnRecvRoomChat(
            roomId,
            playerId,
            nickname,
            message
        );
    }

    void HandleFriendChat(byte[] body)
    {
        if (body == null || body.Length < 156)
            return;

        uint senderPlayerId = BitConverter.ToUInt32(body, 0);
        uint targetPlayerId = BitConverter.ToUInt32(body, 4);
        string senderNickname = Packet.ReadFixedString(body, 8, 20);
        string message = Packet.ReadFixedString(body, 28, 128);

        chatPanelController?.OnRecvFriendChat(
            senderPlayerId,
            targetPlayerId,
            senderNickname,
            message
        );
    }

    private void HandleFriendOverview(byte[] body)
    {
        const int UserSize = 89;
        const int MaxContacts = 50;
        if (body == null || body.Length < 3 + MaxContacts * UserSize * 2)
            return;

        FriendPresenceMode presenceMode = body[0] == 1
            ? FriendPresenceMode.DoNotDisturb
            : FriendPresenceMode.Online;
        int friendCount = Math.Min((int)body[1], MaxContacts);
        int blockedCount = Math.Min((int)body[2], MaxContacts);
        int offset = 3;

        List<FriendUserData> friends = new(friendCount);
        for (int i = 0; i < MaxContacts; i++)
        {
            FriendUserData user = ReadFriendUser(body, ref offset);
            if (i < friendCount)
                friends.Add(user);
        }

        List<FriendUserData> blocked = new(blockedCount);
        for (int i = 0; i < MaxContacts; i++)
        {
            FriendUserData user = ReadFriendUser(body, ref offset);
            if (i < blockedCount)
                blocked.Add(user);
        }

        friendPanelController?.ApplyOverview(presenceMode, friends, blocked);
        chatPanelController?.ApplyFriendOverview(friends);
    }

    private void HandleFriendPresenceSetResult(byte[] body)
    {
        if (body == null || body.Length < 2)
            return;

        FriendPresenceMode mode = body[1] == 1
            ? FriendPresenceMode.DoNotDisturb
            : FriendPresenceMode.Online;
        friendPanelController?.ApplyPresenceResult(body[0] == 0, mode);
    }

    private void HandleFriendSearch(byte[] body)
    {
        List<FriendUserData> users = ReadFriendUserList(body, 10);
        Debug.Log(
            $"[FRIEND SEARCH RECV] type=71 count={users.Count} bodyBytes={body?.Length ?? 0}"
        );

        if (friendPanelController == null)
        {
            Debug.LogWarning("[FRIEND SEARCH RECV] FriendPanelController is not registered");
            return;
        }

        friendPanelController.ApplySearchResults(users);
    }

    private void HandleFriendRecommend(byte[] body)
    {
        friendPanelController?.ApplyRecommendations(
            ReadFriendUserList(body, 3)
        );
    }

    private void HandleFriendOperationResult(byte[] body)
    {
        if (body == null || body.Length < 22)
            return;

        FriendOperation operation = (FriendOperation)body[0];
        FriendOperationResult result = (FriendOperationResult)body[1];
        string targetUserId = Packet.ReadFixedString(body, 2, 20);
        friendPanelController?.ApplyOperationResult(
            operation,
            result,
            targetUserId
        );
    }

    private void HandleFriendRequestList(byte[] body)
    {
        const int MaxRequests = 30;
        const int RequestSize = 90;
        if (body == null || body.Length < 1 + MaxRequests * RequestSize)
            return;

        int count = Math.Min((int)body[0], MaxRequests);
        int offset = 1;
        List<FriendRequestData> requests = new(count);

        for (int i = 0; i < MaxRequests; i++)
        {
            bool incoming = body[offset++] == 1;
            FriendUserData user = ReadFriendUser(body, ref offset);
            if (i < count)
            {
                requests.Add(new FriendRequestData
                {
                    incoming = incoming,
                    user = user
                });
            }
        }

        friendPanelController?.ApplyRequests(requests);
    }

    private void HandleFriendRecent(byte[] body)
    {
        friendPanelController?.ApplyRecent(
            ReadFriendUserList(body, 20)
        );
    }

    private void HandleFriendMemoSetResult(byte[] body)
    {
        if (body == null || body.Length < 53)
            return;

        bool success = body[0] == 0;
        string targetUserId = Packet.ReadFixedString(body, 1, 20);
        string memo = Packet.ReadFixedString(body, 21, 32);
        Debug.Log($"[FRIEND MEMO RESULT] success={success} target={targetUserId} memo='{memo}'");

        if (success)
            SendFriendOverviewRequest();
    }

    private static List<FriendUserData> ReadFriendUserList(
        byte[] body,
        int maxCount)
    {
        const int UserSize = 89;
        List<FriendUserData> users = new();
        if (body == null || body.Length < 1 + maxCount * UserSize)
            return users;

        int count = Math.Min((int)body[0], maxCount);
        int offset = 1;
        for (int i = 0; i < maxCount; i++)
        {
            FriendUserData user = ReadFriendUser(body, ref offset);
            if (i < count)
                users.Add(user);
        }
        return users;
    }

    private static FriendUserData ReadFriendUser(byte[] body, ref int offset)
    {
        FriendUserData user = new();
        user.playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;
        user.userId = Packet.ReadFixedString(body, offset, 20);
        offset += 20;
        user.nickname = Packet.ReadFixedString(body, offset, 20);
        offset += 20;
        user.level = BitConverter.ToInt32(body, offset);
        offset += 4;
        user.playerIconId = BitConverter.ToInt32(body, offset);
        offset += 4;
        user.rankImageId = BitConverter.ToInt32(body, offset);
        offset += 4;
        user.presenceStatus = (FriendPresenceStatus)body[offset++];
        user.memo = Packet.ReadFixedString(body, offset, 32);
        offset += 32;
        return user;
    }

    void HandleLobbyCharacterSelectResult(byte[] body)
    {

        byte result = body[0];
        int modelId = BitConverter.ToInt32(body, 1);


        lobbyPanelController?.OnRecvLobbyCharacterSelectResult(result, modelId);
    }

    void HandleLobbyBackgroundSelectResult(byte[] body)
    {
        byte result = body[0];
        int backgroundId = BitConverter.ToInt32(body, 1);

        lobbyPanelController?.OnRecvLobbyBackgroundSelectResult(result, backgroundId);
    }

    void HandleLobbyCharacterList(byte[] body)
    {
        ushort count = BitConverter.ToUInt16(body, 0);
        int currentCharacterModelId = BitConverter.ToInt32(body, 2);

        List<LobbyCharacterData> list = new List<LobbyCharacterData>();

        int offset = 6;
        for (int i = 0; i < count; i++)
        {
            LobbyCharacterData data = new LobbyCharacterData();
            data.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            data.owned = body[offset] != 0;
            offset += 1;

            list.Add(data);
        }

        lobbyPanelController?.OnRecvLobbyCharacterList(list, currentCharacterModelId);
    }

}
