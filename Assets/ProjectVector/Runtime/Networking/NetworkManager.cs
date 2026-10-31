using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using System.Security.Principal;
using System.Text;

public partial class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    NetworkClient client = new NetworkClient();
    Queue<(ushort, byte[])> packetQueue = new();

    LoginUI loginUI;

    LobbyPanelController lobbyPanelController;
    GamePanelController gamePanelController;
    ProfilePanelController profilePanelController;
    ChatPanelController chatPanelController;
    FriendPanelController friendPanelController;
    LobbyManager lobbyManager;

    private LobbyRoomChatKind lobbyRoomChatKind = LobbyRoomChatKind.None;
    private uint lobbyRoomChatId;
    private string lobbyRoomChatName = "";

    public LobbyRoomChatKind CurrentLobbyRoomChatKind => lobbyRoomChatKind;
    public uint CurrentLobbyRoomChatId => lobbyRoomChatId;
    public string CurrentLobbyRoomChatName => lobbyRoomChatName;

    private PickManager pickManager;
    private MatchManager matchManager;
    private ScoreboardData latestScoreboardData;

    private uint myPlayerId;
    public uint MyPlayerId => myPlayerId;
    public PlayerIdentity MyPlayer => FindPlayerIdentityById(myPlayerId);

    private string myUserId = "";
    public string MyUserId => myUserId;
    public bool IsConnected => client != null && client.IsConnected;
    public bool IsAuthenticated { get; private set; }

    private uint pendingReturnRoomId = 0;

    public bool HasPendingReturnRoom => pendingReturnRoomId != 0;
    public uint PendingReturnRoomId => pendingReturnRoomId;

    private readonly Dictionary<uint, GameObject> droppedItemMap = new();
    private readonly Dictionary<uint, PlayerCombatSnapshot> playerCombatSnapshots = new();
    private const int WeaponSkinWeaponCount = 14;
    private const int WeaponSkinCharacterCount = 4;
    private readonly Dictionary<int, Dictionary<int, int>> equippedWeaponSkinsByCharacter = new();

    public event Action<int> WeaponSkinLoadoutChanged;


    public bool HasWeaponSkinLoadout(int characterId)
    {
        return equippedWeaponSkinsByCharacter.ContainsKey(characterId);
    }

    public int GetEquippedWeaponSkinId(int characterId, int weaponId)
    {
        return equippedWeaponSkinsByCharacter.TryGetValue(
                   characterId,
                   out Dictionary<int, int> loadout) &&
               loadout.TryGetValue(weaponId, out int skinId)
            ? skinId
            : 0;
    }

    public Dictionary<int, int> CopyEquippedWeaponSkins(int characterId)
    {
        return equippedWeaponSkinsByCharacter.TryGetValue(
            characterId,
            out Dictionary<int, int> loadout)
            ? new Dictionary<int, int>(loadout)
            : new Dictionary<int, int>();
    }
    public void ClearPendingReturnRoom()
    {
        pendingReturnRoomId = 0;
    }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        client.OnPacket += (type, body) =>
        {
            lock (packetQueue)
            {
                packetQueue.Enqueue((type, body));
            }
        };

        client.Connect("127.0.0.1", 9000);
        //client.Connect("211.247.47.240", 9000);
    }

    void Update()
    {
        UpdateReconnect();
        while (true)
        {
            (ushort, byte[]) packet;

            lock (packetQueue)
            {
                if (packetQueue.Count == 0)
                    break;

                packet = packetQueue.Dequeue();
            }

            HandlePacket(packet.Item1, packet.Item2);
        }
    }











    public void SetReadyRoomChatContext()
    {
        lobbyRoomChatKind = LobbyRoomChatKind.ReadyRoom;
        lobbyRoomChatId = 0;
        lobbyRoomChatName = "준비방";
        SyncLobbyRoomChatContext();
    }

    public void SetPrivateRoomChatContext(uint roomId, string roomName)
    {
        if (roomId == 0)
            return;

        lobbyRoomChatKind = LobbyRoomChatKind.PrivateRoom;
        lobbyRoomChatId = roomId;
        lobbyRoomChatName = roomName ?? "";
        SyncLobbyRoomChatContext();
    }

    public void ClearTemporaryRoomChatContext()
    {
        if (lobbyRoomChatKind == LobbyRoomChatKind.ReadyRoom)
            ClearLobbyRoomChatContext();
    }

    public void ClearPrivateRoomChatContext(uint roomId = 0)
    {
        if (lobbyRoomChatKind != LobbyRoomChatKind.PrivateRoom)
            return;

        if (roomId != 0 && lobbyRoomChatId != roomId)
            return;

        ClearLobbyRoomChatContext();
    }

    public void ClearLobbyRoomChatContext()
    {
        lobbyRoomChatKind = LobbyRoomChatKind.None;
        lobbyRoomChatId = 0;
        lobbyRoomChatName = "";
        SyncLobbyRoomChatContext();
    }

    private void SyncLobbyRoomChatContext()
    {
        chatPanelController?.SetRoomChatContext(
            lobbyRoomChatKind,
            lobbyRoomChatId,
            lobbyRoomChatName
        );
    }


    private void SendHeaderOnly(PacketType packetType)
    {
        byte[] buffer = new byte[4];
        BitConverter.GetBytes((ushort)buffer.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes((ushort)packetType).CopyTo(buffer, 2);
        client.Send(buffer);
    }

    void HandlePacket(ushort type, byte[] body)
    {
        if (HandleReconnectPacket(type, body)) return;
        if (type == (ushort)PacketType.PKT_SC_PICK_ACTIONS) { HandlePickActions(body); return; }
        if (type == (ushort)PacketType.PKT_SC_PICK_ACTION_RESULT) { HandlePickActionResult(body); return; }
        if (type == (ushort)PacketType.PKT_SC_PICK_SHOWCASE) { HandlePickShowcase(body); return; }
        if (type == (ushort)PacketType.PKT_SC_TEAM_PURCHASES) { HandleTeamPurchases(body); return; }
        if (type == (ushort)PacketType.PKT_SC_TEAM_PURCHASE_RESULT) { HandleTeamPurchaseResult(body); return; }
        if (type == (ushort)PacketType.PKT_SC_KILL_FEED) { HandleKillFeed(body); return; }
        if (type == (ushort)PacketType.PKT_SC_ROUND_RULES) { HandleRoundRules(body); return; }
        if (type == (ushort)PacketType.PKT_SC_TEAM_LOADOUT) { HandleTeamLoadout(body); return; }
        if (type == (ushort)PacketType.PKT_SC_TEAM_EQUIPMENT) { HandleTeamEquipment(body); return; }
        if (type == (ushort)PacketType.PKT_SC_TEAM_PING) { HandleTeamPing(body); return; }
        switch (type)
        {
            case (ushort)PacketType.PKT_SC_REGISTER_RESULT:
                HandleRegister(body);
                break;

            case (ushort)PacketType.PKT_SC_LOGIN_RESULT:
                HandleLogin(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_ENTER:
                HandleLobbyEnter(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_CHAT:
                HandleLobbyChat(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_CHAT:
                HandleRoomChat(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_CHAT:
                HandleFriendChat(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_OVERVIEW:
                HandleFriendOverview(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_PRESENCE_SET_RESULT:
                HandleFriendPresenceSetResult(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_SEARCH:
                HandleFriendSearch(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_RECOMMEND:
                HandleFriendRecommend(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_OPERATION_RESULT:
                HandleFriendOperationResult(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_REQUEST_LIST:
                HandleFriendRequestList(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_RECENT:
                HandleFriendRecent(body);
                break;

            case (ushort)PacketType.PKT_SC_FRIEND_MEMO_SET_RESULT:
                HandleFriendMemoSetResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_MAP_SET_RESULT:
                HandleRoomMapSetResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PROFILE:
                HandleProfile(body);
                break;
            case (ushort)PacketType.PKT_SC_PROFILE_BIRTHDAY_SET_RESULT:
                HandleProfileBirthdayResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PROFILE_PRIVACY_SET_RESULT:
                HandleProfilePrivacySetResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PROFILE_LIKE_RESULT:
                HandleProfileLikeResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PROFILE_BATTLE_STATS_SET_RESULT:
                HandleProfileBattleStatsSetResult(body);
                break;

            case (ushort)PacketType.PKT_SC_CHIPCRAFT:
                HandleChipCraft(body);
                break;

            case (ushort)PacketType.PKT_SC_CHIPCRAFT_SAVE_RESULT:
                HandleChipCraftSaveResult(body);
                break;

            case (ushort)PacketType.PKT_SC_LEVEL_REWARD_STATE:
                HandleLevelRewardState(body);
                break;

            case (ushort)PacketType.PKT_SC_LEVEL_REWARD_CLAIM_RESULT:
                HandleLevelRewardClaimResult(body);
                break;

            case (ushort)PacketType.PKT_SC_HONOR_STATE:
                HandleHonorState(body);
                break;

            case 335:
                FindFirstObjectByType<VectorEventPass>(FindObjectsInactive.Include)?.ApplyState(body);
                break;
            case 341: // Cosmetic shop response, handled by the main-thread packet queue.
                VectorShopPanel.Receive(body);
                break;

            case (ushort)PacketType.PKT_SC_HONOR_CLAIM_RESULT:
                HandleHonorClaimResult(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_CHARACTER_SELECT_RESULT:
                HandleLobbyCharacterSelectResult(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_BACKGROUND_SELECT_RESULT:
                HandleLobbyBackgroundSelectResult(body);
                break;

            case (ushort)PacketType.PKT_SC_LOBBY_CHARACTER_LIST:
                HandleLobbyCharacterList(body);
                break;

            case (ushort)PacketType.PKT_SC_WEAPON_SKIN_LOADOUT:
                HandleWeaponSkinLoadout(body);
                break;

            case (ushort)PacketType.PKT_SC_WEAPON_SKIN_SELECT_RESULT:
                HandleWeaponSkinSelectResult(body);
                break;

            case (ushort)PacketType.PKT_SC_CAREER_OVERVIEW:
                HandleCareerOverview(body);
                break;

            case (ushort)PacketType.PKT_SC_CAREER_DETAIL:
                HandleCareerDetail(body);
                break;
            case (ushort)PacketType.PKT_SC_CAREER_RANKING:
                HandleCareerRanking(body);
                break;

            case (ushort)PacketType.PKT_SC_CAREER_MATCH_HISTORY:
                HandleCareerMatchHistory(body);
                break;

            case (ushort)PacketType.PKT_SC_CAREER_INVENTORY:
                HandleCareerInventory(body);
                break;

            case (ushort)PacketType.PKT_SC_TEMP_GAME_ROOM_ENTER:
                HandleTempGameRoomEnter(body);
                break;

            case (ushort)PacketType.PKT_SC_MATCH_APPEARANCE:
                HandleMatchAppearance(body);
                break;
            case (ushort)PacketType.PKT_SC_MATCH_READY_CARDS:
                HandleReadyCards(body);
                break;

            case (ushort)PacketType.PKT_SC_VOICE_RESPONSE:
                HandleVoiceResponse(body);
                break;

            case (ushort)PacketType.PKT_SC_PARTY_INVITE:
            case (ushort)PacketType.PKT_SC_SOCIAL_RESULT:
            case (ushort)PacketType.PKT_SC_PARTY_STATE:
            case (ushort)PacketType.PKT_SC_PARTY_AVAILABILITY:
            case (ushort)PacketType.PKT_SC_PARTY_MODE:
            case (ushort)PacketType.PKT_SC_MATCH_READY:
                HandleSocialPacket((PacketType)type, body);
                break;

            case (ushort)PacketType.PKT_SC_TEMP_GAME_START_RESULT:
                HandleTempGameStartResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_LIST:
                HandleRoomList(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_CREATE_RESULT:
                HandleRoomCreateResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_LEAVE_RESULT:
                HandleRoomLeaveResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_ENTER_RESULT:
                HandleRoomEnterResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_STATE:
                HandleRoomState(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_SLOT_CHANGE_RESULT:
                HandleRoomSlotChangeResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_SLOT_SWAP_REQUEST:
                HandleRoomSlotSwapRequest(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_TRANSFER_LEADER_RESULT:
                HandleRoomTransferLeaderResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_KICK_RESULT:
                HandleRoomKickResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_KICKED:
                HandleRoomKicked(body);
                break;

            case (ushort)PacketType.PKT_SC_ROOM_GAME_START_RESULT:
                HandleRoomGameStartResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PICK_STATE:
                HandlePickState(body);
                break;

            case (ushort)PacketType.PKT_SC_PICK_LOCK_RESULT:
                HandlePickLockResult(body);
                break;

            case (ushort)PacketType.PKT_SC_PICK_TIMER:
                HandlePickTimer(body);
                break;

            case (ushort)PacketType.PKT_SC_MATCH_LOADING_INFO:
                HandleMatchLoadingInfo(body);
                break;

            case (ushort)PacketType.PKT_SC_MATCH_START:
                HandleMatchStart(body);
                break;

            case (ushort)PacketType.PKT_SC_GAME_ALL_READY:
                HandleGameAllReady(body);
                break;

            case (ushort)PacketType.PKT_SC_GAME_PREPARE_TIMER:
                HandleGamePrepareTimer(body);
                break;

            case (ushort)PacketType.PKT_SC_MOVE_STATE:
                HandleMoveState(body);
                break;
            case 397:
                HandleMovementCorrection(body);
                break;

            case (ushort)PacketType.PKT_SC_HP_UPDATE:
                HandleHPUpdate(body);
                break;

            case (ushort)PacketType.PKT_SC_SHOT_CONFIRMED:
                HandleShotConfirmed(body);
                break;

            case (ushort)PacketType.PKT_SC_ROUND_RESULT:
                HandleRoundResult(body);
                break;

            case (ushort)PacketType.PKT_SC_ROUND_RESET:
                HandleRoundReset(body);
                break;

            case (ushort)PacketType.PKT_SC_MATCH_RESULT:
                HandleMatchResult(body);
                break;

            case (ushort)PacketType.PKT_SC_AMMO_UPDATE:
                HandleAmmoUpdate(body);
                break;

            case (ushort)PacketType.PKT_SC_WEAPON_SWITCH_NOTIFY:
                HandleWeaponSwitchNotify(body);
                break;

            case (ushort)PacketType.PKT_SC_ADS_STATE:
                HandleADSState(body);
                break;

            case (ushort)PacketType.PKT_SC_MATCH_RESULT_DETAIL:
                HandleMatchResultDetail(body);
                break;

            case (ushort)PacketType.PKT_SC_RESULT_LEAVE_DONE:
                HandleResultLeaveDone(body);
                break;

            case (ushort)PacketType.PKT_SC_SCOREBOARD_UPDATE:
                HandleScoreboardUpdate(body);
                break;

            case (ushort)PacketType.PKT_SC_ROUND_TIMER:
                HandleRoundTimer(body);
                break;

            case (ushort)PacketType.PKT_SC_SPIKE_PICKUP_NOTIFY:
                HandleSpikePickupNotify(body);
                break;

            case (ushort)PacketType.PKT_SC_SPIKE_PLANTED:
                HandleSpikePlanted(body);
                break;
            case (ushort)PacketType.PKT_SC_SPIKE_PLANT_STATE:
                if (body.Length == 5) SpikeInteractionPresentation.ServerPlant(BitConverter.ToUInt32(body, 0), body[4] != 0);
                break;
            case (ushort)PacketType.PKT_SC_SPIKE_CLOCK:
                if (body.Length == 4 && Spike.PlantedSpike) Spike.PlantedSpike.GetComponent<SpikePresentation>()?.Sync(BitConverter.ToSingle(body, 0));
                break;

            case (ushort)PacketType.PKT_SC_SPIKE_DROPPED:
                HandleSpikeDropped(body);
                break;

            case (ushort)PacketType.PKT_SC_SPIKE_DEFUSE_STATE:
                HandleSpikeDefuseState(body);
                break;

            case (ushort)PacketType.PKT_SC_SPIKE_DEFUSED:
                HandleSpikeDefused(body);
                break;

            case (ushort)PacketType.PKT_SC_DIE:
                HandleDie(body);
                break;

            case (ushort)PacketType.PKT_SC_WEAPON_DROPPED:
                HandleWeaponDropped(body);
                break;

            case (ushort)PacketType.PKT_SC_WEAPON_PICKUP_NOTIFY:
                HandleWeaponPickupNotify(body);
                break;

            case (ushort)PacketType.PKT_SC_STORE_BUY_RESULT:
                HandleStoreBuyResult(body);
                break;

            case (ushort)PacketType.PKT_SC_SKILL_STATE:
                HandleSkillState(body);
                break;

            case (ushort)PacketType.PKT_SC_KNIFE_ATTACK_NOTIFY:
                HandleKnifeAttackNotify(body);
                break;

            case (ushort)PacketType.PKT_SC_FIRE_NOTIFY:
                HandleFireNotify(body);
                break;

            case (ushort)PacketType.PKT_SC_RELOAD_VISUAL:
                HandleReloadVisual(body);
                break;

            case (ushort)PacketType.PKT_SC_IN_GAME_CHAT:
                HandleInGameChat(body);
                break;

            case (ushort)PacketType.PKT_SC_DRONE_SPAWN:
                HandleDroneSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_DRONE_STATE:
                HandleDroneState(body);
                break;

            case (ushort)PacketType.PKT_SC_DRONE_DESTROY:
                HandleDroneDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_DRONE_TASER_FIRE:
                HandleDroneTaserFire(body);
                break;

            case (ushort)PacketType.PKT_SC_DRONE_HP_UPDATE:
                HandleDroneHpUpdate(body);
                break;

            case (ushort)PacketType.PKT_SC_SCANNER_SPAWN:
                HandleScannerSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_SCANNER_ANCHORED:
                HandleScannerAnchored(body);
                break;

            case (ushort)PacketType.PKT_SC_SCANNER_ZAP:
                HandleScannerZap(body);
                break;

            case (ushort)PacketType.PKT_SC_SCANNER_DESTROY:
                HandleScannerDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_EMP_CAST:
                HandleEmpCast(body);
                break;
            case (ushort)PacketType.PKT_SC_EMP_GESTURE:
                if(body!=null && body.Length==5 && body[4]<=1)
                    FindPlayerIdentityById(BitConverter.ToUInt32(body,0))?.GetComponent<InitiatorSkill_E>()?.ReceiveGesture(body[4]==1);
                break;
            case (ushort)PacketType.PKT_SC_SCANNER_GESTURE:
                if(body!=null && body.Length==5 && body[4]<=2)
                    FindPlayerIdentityById(BitConverter.ToUInt32(body,0))?.GetComponent<InitiatorSkill_C>()?.ReceiveGesture(body[4]);
                break;

            case (ushort)PacketType.PKT_SC_ELECTRIC_STATUS:
                HandleElectricStatus(body);
                break;

            case (ushort)PacketType.PKT_SC_DISCHARGE_GESTURE:
            case (ushort)PacketType.PKT_SC_DISCHARGE_BEGIN:
            case (ushort)PacketType.PKT_SC_DISCHARGE_STRIKE:
            case (ushort)PacketType.PKT_SC_DISCHARGE_END:
                DischargeDirector.Instance?.Receive(type, body);
                break;
            case (ushort)PacketType.PKT_SC_ULTIMATE_CAST:
                HandleUltimateCast(body);
                break;

            case (ushort)PacketType.PKT_SC_BEAST_STEALTH_STATE:
                HandleBeastStealthState(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_SMOKE_SPAWN:
                HandleEntrySmokeSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_SMOKE_ACTIVATE:
                HandleEntrySmokeActivate(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_SMOKE_DESTROY:
                HandleEntrySmokeDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_SMOKE_STATE:
                HandleEntrySmokeState(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_DASH_CAST:
                HandleEntryDashCast(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_DASH_END:
                HandleEntryDashEnd(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_BLADE_STATE:
                HandleEntryBladeState(body);
                break;

            case (ushort)PacketType.PKT_SC_ENTRY_BLADE_FIRE:
                HandleEntryBladeFire(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_SWAMP_SPAWN:
                HandleControllerSwampSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_SWAMP_ACTIVATE:
                HandleControllerSwampActivate(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_SWAMP_DESTROY:
                HandleControllerSwampDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_SMOKE_SPAWN:
                HandleControllerSmokeSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_SMOKE_DESTROY:
                HandleControllerSmokeDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_DECOY_SPAWN:
                HandleControllerDecoySpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_DECOY_ACTIVATE:
                HandleControllerDecoyActivate(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_DECOY_DESTROY:
                HandleControllerDecoyDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_DECOY_TRIGGER:
                if (body != null && body.Length == 8)
                    ControllerDecoyNetworkManager.Instance?.OnDecoyTrigger(BitConverter.ToUInt32(body, 0), BitConverter.ToSingle(body, 4));
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_ULTIMATE_SPAWN:
                HandleControllerUltimateSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_CONTROLLER_ULTIMATE_DESTROY:
                HandleControllerUltimateDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_C_SPAWN:
                HandleSentinelCSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_TRACK:
                SentinelCNetworkManager.Instance?.ReceiveTracking(body);
                break;
            case (ushort)PacketType.PKT_SC_SENTINEL_STATUS:
                SentinelCNetworkManager.Instance?.ReceiveStatus(body);
                break;
            case (ushort)PacketType.PKT_SC_SENTINEL_C_REVEAL:
                SentinelCNetworkManager.Instance?.ReceiveReveal(body);
                break;
            case (ushort)PacketType.PKT_SC_SENTINEL_GESTURE:
                if(body.Length==5 && body[4]<=2)FindPlayerIdentityById(BitConverter.ToUInt32(body,0))?.GetComponent<SentinelCastMotion>()?.ReceiveGesture(body[4]);
                break;
            case (ushort)PacketType.PKT_SC_SENTINEL_C_ANCHOR:
                HandleSentinelCAnchor(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_C_TRIGGER:
                HandleSentinelCTrigger(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_C_DESTROY:
                HandleSentinelCDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_Q_SPAWN:
                HandleSentinelQSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_Q_ACTIVATE:
                HandleSentinelQActivate(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_Q_DESTROY:
                HandleSentinelQDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_E_SPAWN:
                HandleSentinelESpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_E_ACTIVATE:
                HandleSentinelEActivate(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_E_DESTROY:
                HandleSentinelEDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_X_SPAWN:
                HandleSentinelXSpawn(body);
                break;

            case (ushort)PacketType.PKT_SC_SENTINEL_X_DESTROY:
                HandleSentinelXDestroy(body);
                break;

            case (ushort)PacketType.PKT_SC_FAVOR_LEVEL_RESPONSE:    // 호감도 레벨
                HandleFavorLevelResponse(body);
                break;

            case 371:
                HandleCombatReport(body);
                break;

            default:
                Debug.LogWarning($"처리 안 된 패킷 type={type}");
                break;


        }
    }

    public void SendGameLoadReady()
    {
        if (ReconnectBusy) return; // Reconnect has its own two-stage ready handshake.
        byte[] buffer = new byte[4];

        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_GAME_LOAD_READY;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, 2, 2);

        client.Send(buffer);

        Debug.Log("[SEND] GAME LOAD READY");
    }

    private void HandleGameAllReady(byte[] body)
    {
        movementRound=1;
        Debug.Log("[RECV] GAME ALL READY");

        SkillInventoryState.Reset();
        playerCombatSnapshots.Clear();

        int offset = 0;

        GameAllReadyData data = new GameAllReadyData();

        data.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        data.mapName = Packet.ReadFixedString(body, offset, 32);
        resultMapName = data.mapName;
        offset += 32;

        data.prepareSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        byte memberCount = body[offset];
        offset += 1;

        for (int i = 0; i < 6; i++)
        {
            GameReadyMemberData member =
                new GameReadyMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.userId = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.team = body[offset];
            offset += 1;

            member.slotIndex = body[offset];
            offset += 1;

            member.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.rankImageId = BitConverter.ToInt32(body, offset);
            offset += 4;

            for (int weaponId = 0; weaponId < WeaponSkinWeaponCount; weaponId++)
            {
                member.weaponSkinLoadout[weaponId] = BitConverter.ToInt32(body, offset);
                offset += 4;
            }

            Debug.Log(
                $"[GAME ALL READY MEMBER PARSED] " +
                $"index={i}, " +
                $"playerId={member.playerId}, " +
                $"userId={member.userId}, " +
                $"nickname={member.nickname}, " +
                $"team={member.team}, " +
                $"slot={member.slotIndex}, " +
                $"characterId={member.characterId}, " +
                $"rankImageId={member.rankImageId}"
            );

            if (i < memberCount)
            {
                data.members.Add(member);
            }
        }

        Debug.Log(
            $"[RECV] GAME ALL READY " +
            $"roomId={data.roomId}, " +
            $"memberCount={data.members.Count}"
        );

        if (matchManager == null)
        {
            Debug.LogError(
                "[NETWORK] matchManager is null"
            );

            return;
        }

        matchManager.OnRecvGameAllReady(
            data
        );
        reconnectRoster = data;

        foreach (GameReadyMemberData member in data.members)
        {
            ApplyTeamToPlayer(member.playerId, member.team);

            if (member.playerId == MyPlayerId && MiniMapCamera.Instance != null)
            {
                byte firstRoundSpawnTeam = member.team == 0 ? (byte)0 : (byte)1;

                MiniMapCamera.Instance.SetLocalRoundRole(firstRoundSpawnTeam);
            }
        }

        RefreshPlayerTagsByTeam();

        if (GameController.Instance != null)
        {
            GameController.Instance.EnterPlayingPhase();
        }
        else
        {
            Debug.LogWarning(
                "[GAME ALL READY] " +
                "GameController.Instance is null"
            );
        }

        Debug.Log(
            "[GAME ALL READY] " +
            "all player data applied, " +
            "game state changed to Playing"
        );
    }

    private void HandleGamePrepareTimer(byte[] body)
    {
        int offset = 0;

        ushort remainingSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        Debug.Log($"[GAME PREPARE TIMER] remaining={remainingSeconds}");

        if (TopInfoUI.Instance != null)
        {
            TopInfoUI.Instance.SetTimer(remainingSeconds);

            if (remainingSeconds == 0)
            {
                TopInfoUI.Instance.SetGameDescription("전투 단계");
            }
        }

        if (matchManager != null)
        {
            matchManager.OnRecvGamePrepareTimer(remainingSeconds);
        }
        else
        {
            Debug.LogError("[NETWORK] matchManager is null. PrepareTimer ignored.");
        }
    }

    public void SendMoveState(
    float x,
    float y,
    float z,
    float yaw,
    float pitch,
    byte animIndex,
    bool isCrouching)
    {
        ushort size = 4 + 4 + 4 + 4 + 4 + 4 + 1 + 1 + 8;
        ushort type = (ushort)PacketType.PKT_CS_MOVE_STATE;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(yaw), 0, buffer, offset, 4);
        offset += 4;

        Buffer.BlockCopy(BitConverter.GetBytes(pitch), 0, buffer, offset, 4);
        offset += 4;

        buffer[offset] = animIndex;
        offset += 1;

        buffer[offset] = isCrouching ? (byte)1 : (byte)0;
        offset += 1;

        Buffer.BlockCopy(BitConverter.GetBytes(Time.realtimeSinceStartupAsDouble),0,buffer,offset,8);

        client.Send(buffer);
    }

    private void HandleMoveState(byte[] body)
    {
        if (body==null || (body.Length!=26 && body.Length!=34)) return;
        int offset = 0;

        uint playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        float x = BitConverter.ToSingle(body, offset);
        offset += 4;

        float y = BitConverter.ToSingle(body, offset);
        offset += 4;

        float z = BitConverter.ToSingle(body, offset);
        offset += 4;

        float yaw = BitConverter.ToSingle(body, offset);
        offset += 4;

        float pitch = BitConverter.ToSingle(body, offset);
        offset += 4;

        byte animIndex = body[offset];
        offset += 1;

        bool isCrouching = body[offset] != 0; offset += 1;
        if (playerId == MyPlayerId)
            return;

        PlayerMovement movement = FindPlayerMovementById(playerId);

        if (movement == null)
        {
            Debug.LogWarning($"[MOVE RECV] PlayerMovement not found. playerId={playerId}");
            return;
        }

        movement.OnReceiveRemoteMove(
            new Vector3(x, y, z),
            yaw,
            pitch,
            animIndex,
            isCrouching,
            body.Length==34 ? BitConverter.ToDouble(body,26) : -1
        );
    }

    private void HandleKnifeAttackNotify(
    byte[] body)
    {
        if (body == null ||
            body.Length < 4)
        {
            Debug.LogWarning(
                "[KNIFE ATTACK NOTIFY] invalid body"
            );

            return;
        }

        int offset = 0;

        uint playerId =
            BitConverter.ToUInt32(
                body,
                offset
            );
        offset += 4;


        if (playerId == MyPlayerId)
            return;

        PlayerIdentity identity =
            FindPlayerIdentityById(
                playerId
            );

        if (identity == null)
        {
            Debug.LogWarning(
                $"[KNIFE ATTACK NOTIFY] " +
                $"PlayerIdentity not found. " +
                $"playerId={playerId}"
            );

            return;
        }


        PlayerMovement movement =
            identity.GetComponent<PlayerMovement>();

        if (movement == null)
        {
            movement =
                identity.GetComponentInChildren<PlayerMovement>(
                    true
                );
        }

        if (movement != null)
        {
            movement.PlayKnifeAttackAnimation();
        }


        PlayerItem playerItem =
            identity.GetComponent<PlayerItem>();

        if (playerItem == null)
        {
            playerItem =
                identity.GetComponentInChildren<PlayerItem>(
                    true
                );
        }

        if (playerItem != null)
        {
            playerItem.PlayKnifeAttackVisualFromServer();
        }
        else
        {
            Debug.LogWarning(
                $"[KNIFE ATTACK NOTIFY] " +
                $"PlayerItem not found. " +
                $"playerId={playerId}"
            );
        }

        Debug.Log(
            $"[KNIFE ATTACK NOTIFY] " +
            $"playerId={playerId}, " +
            $"thirdPerson={(movement != null)}, " +
            $"firstPerson={(playerItem != null)}"
        );
    }

    private void HandleHPUpdate(byte[] body)
    {
        if (body == null || body.Length < 20)
        {
            Debug.LogWarning($"[HP UPDATE] invalid body length={body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int hp = BitConverter.ToInt32(body, offset);
        offset += 4;

        int barrier = BitConverter.ToInt32(body, offset);
        offset += 4;

        int maxHp = BitConverter.ToInt32(body, offset);
        offset += 4;

        int maxBarrier = BitConverter.ToInt32(body, offset);
        offset += 4;

        PlayerCombatSnapshot snapshot = GetOrCreateCombatSnapshot(playerId);

        int previousTotalHealth = snapshot.hp + snapshot.barrier;
        int newTotalHealth = hp + barrier;

        bool tookDamage =
            snapshot.hasHpState &&
            newTotalHealth < previousTotalHealth;

        snapshot.hp = hp;
        snapshot.barrier = barrier;
        snapshot.maxHp = maxHp;
        snapshot.maxBarrier = maxBarrier;
        snapshot.hasHpState = true;

        PlayerIdentity target = FindPlayerIdentityById(playerId);

        var loadout = latestScoreboardData?.members.Find(m=>m.playerId==playerId);
        if (loadout != null) {
            loadout.hasLifeState=true;loadout.equipmentAlive=hp>0;
            if(loadout.hasLoadout){loadout.armor=barrier;loadout.maxArmor=maxBarrier;}
            InGameScoreboardUI.Instance?.OnRecvScoreboardUpdate(latestScoreboardData);
            StorePanelUI.Instance?.RefreshPlayerInfo();
        }

        if (target != null)
        {
            PlayerHP hpComponent = target.GetComponent<PlayerHP>();

            if (hpComponent == null)
                hpComponent = target.GetComponentInChildren<PlayerHP>(true);

            if (hpComponent != null)
            {
                hpComponent.SetServerHP(
                    hp,
                    barrier,
                    maxHp,
                    maxBarrier
                );
            }
        }

        bool isSpectating =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating;

        uint displayedPlayerId = isSpectating
            ? SpectatorManager.Instance.CurrentTargetPlayerId
            : MyPlayerId;

        bool hpUpdateBelongsToDisplayedPlayer =
            playerId == displayedPlayerId;

        if (playerId == MyPlayerId && !isSpectating)
        {
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.SetHpBarrier(
                    hp,
                    barrier,
                    maxHp,
                    maxBarrier
                );
            }

            if (StorePanelUI.Instance != null)
            {
                StorePanelUI.Instance.SetOwnedArmorFromServer(
                    maxBarrier
                );
            }
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyPlayerStateChanged(
                playerId
            );
        }

        if (tookDamage &&
            hpUpdateBelongsToDisplayedPlayer &&
            InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.PlayDamageFeedback();
        }

        Debug.Log(
            $"[HP UPDATE] playerId={playerId}, " +
            $"previousTotal={previousTotalHealth}, " +
            $"newTotal={newTotalHealth}, " +
            $"tookDamage={tookDamage}, " +
            $"displayedPlayerId={displayedPlayerId}"
        );
    }

    private void ApplyHPUpdate(
    uint playerId,
    int hp,
    int barrier,
    int maxHp,
    int maxBarrier)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        for (int i = 0; i < identities.Length; i++)
        {
            PlayerIdentity identity = identities[i];

            if (identity == null)
                continue;

            if (identity.PlayerId != playerId)
                continue;

            PlayerHP playerHP = identity.GetComponent<PlayerHP>();

            if (playerHP != null)
            {
                playerHP.SetServerHP(
                    hp,
                    barrier,
                    maxHp,
                    maxBarrier
                );
            }

            break;
        }

        if (playerId == MyPlayerId)
        {
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.SetHpBarrier(
                    hp,
                    barrier,
                    maxHp,
                    maxBarrier
                );
            }
        }
    }

    private PlayerMovement FindPlayerMovementById(uint playerId)
    {
        PlayerIdentity[] identities = FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None);

        for (int i = 0; i < identities.Length; i++)
        {
            PlayerIdentity identity = identities[i];

            if (identity == null)
                continue;

            if (identity.PlayerId != playerId)
                continue;

            PlayerMovement movement = identity.GetComponent<PlayerMovement>();

            if (movement == null)
                movement = identity.GetComponentInChildren<PlayerMovement>(true);

            return movement;
        }

        return null;
    }

    private uint nextTrackedShot = unchecked((uint)Environment.TickCount);
    private readonly Dictionary<ulong, (uint target, float time)> pendingTrackedHits = new Dictionary<ulong, (uint, float)>();

    private void HandleShotConfirmed(byte[] body)
    {
        if (body == null || body.Length != 18) return;
        uint shot = BitConverter.ToUInt32(body, 0);
        uint target = BitConverter.ToUInt32(body, 4);
        int damage = BitConverter.ToInt32(body, 12);
        byte pellet = body[16];
        ulong key = ((ulong)shot << 8) | pellet;
        if (!pendingTrackedHits.TryGetValue(key, out var pending) || pending.target != target) return;
        pendingTrackedHits.Remove(key);
        if (damage > 0 && Time.unscaledTime - pending.time <= 5f)
            HitManager.TriggerEnemyHit();
    }

    public uint SendTrackedFire(int weaponId, Vector3? origin = null)
    {
        if (StorePanelUI.IsBlockingGameplayInput) return 0;
        uint shot = unchecked(++nextTrackedShot);
        if (shot == 0) shot = unchecked(++nextTrackedShot);
        byte[] packet = new byte[origin.HasValue ? 24 : 12];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)packet.Length), 0, packet, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_FIRE), 0, packet, 2, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(shot), 0, packet, 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(weaponId), 0, packet, 8, 4);
        if (origin.HasValue)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(origin.Value.x), 0, packet, 12, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(origin.Value.y), 0, packet, 16, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(origin.Value.z), 0, packet, 20, 4);
        }
        client.Send(packet);
        return shot;
    }

    public void SendTrackedHit(uint targetPlayerId, bool isHeadshot, uint shot, byte pellet)
        => SendTrackedHit(targetPlayerId, isHeadshot ? (byte)1 : (byte)0, shot, pellet);

    // Wire regions stay 0=body, 1=head, 2=legs; authored HitPart values differ.
    public void SendTrackedHit(uint targetPlayerId, byte hitZone, uint shot, byte pellet, Vector3? point = null)
    {
        if (shot == 0 || hitZone > 2) return;
        byte[] packet = new byte[point.HasValue ? 26 : 14];
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)packet.Length), 0, packet, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes((ushort)PacketType.PKT_CS_HIT), 0, packet, 2, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(targetPlayerId), 0, packet, 4, 4);
        packet[8] = hitZone;
        Buffer.BlockCopy(BitConverter.GetBytes(shot), 0, packet, 9, 4);
        packet[13] = pellet;
        if (point.HasValue)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(point.Value.x), 0, packet, 14, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(point.Value.y), 0, packet, 18, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(point.Value.z), 0, packet, 22, 4);
        }
        // Bounded even if a server rejects all requests; no speculative player hit marker.
        if (pendingTrackedHits.Count >= 2048) pendingTrackedHits.Clear();
        pendingTrackedHits[((ulong)shot << 8) | pellet] = (targetPlayerId, Time.unscaledTime);
        client.Send(packet);
    }

    public void SendHit(uint targetPlayerId, bool isHeadshot)
        => SendHit(targetPlayerId, isHeadshot ? 1 : 0);

    public void SendHit(uint targetPlayerId, int hitPart = 0)
    {
        if (hitPart < 0 || hitPart > 2) return;
        ushort size = 4 + 4 + 1;
        ushort type = (ushort)PacketType.PKT_CS_HIT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(targetPlayerId), 0, buffer, offset, 4);
        offset += 4;

        buffer[offset] = (byte)hitPart;

        client.Send(buffer);

        Debug.Log($"[SEND HIT] targetPlayerId={targetPlayerId} hitPart={hitPart}");
    }

    public void SendWeaponSwitch(int weaponId)
    {
        ushort size = 4 + 4;
        ushort type = (ushort)PacketType.PKT_CS_WEAPON_SWITCH;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(weaponId), 0, buffer, offset, 4);

        client.Send(buffer);

        Debug.Log($"[SEND WEAPON SWITCH] weaponId={weaponId}");
    }

    private void HandleRoundResult(byte[] body)
    {
        ScannerNetworkManager.Instance?.ClearAll();
        DischargeDirector.Instance?.ClearAll();
        ScannerFeedbackUI.Instance?.ResetFeedback();
        int offset = 0;

        byte winnerTeam = body[offset];
        offset += 1;

        byte scoreA = body[offset];
        offset += 1;

        byte scoreB = body[offset];
        offset += 1;

        byte currentRound = body[offset];
        offset += 1;

        byte matchEnd = body[offset];
        offset += 1;

        RoundWinReason winReason = (RoundWinReason)body[offset];
        offset += 1;

        SpikeInteractionPresentation.StopAll(false);
        if (Spike.PlantedSpike)
        {
            var visual = Spike.PlantedSpike.GetComponent<SpikePresentation>();
            if (winReason == RoundWinReason.ATTACK_SPIKE_EXPLODE) visual?.Resolve(true);
            else visual?.Silence();
            Spike.PlantedSpike.ForceCancelInteraction();
        }

        ushort delaySeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        Debug.Log(
            $"[ROUND RESULT] winnerTeam={winnerTeam}, " +
            $"scoreA={scoreA}, scoreB={scoreB}, round={currentRound}, " +
            $"matchEnd={matchEnd}, delay={delaySeconds}"
        );

        byte localTeam = GetLocalTeamForRoundResult();

        if (CombatFeedbackUI.Instance != null)
        {
            CombatFeedbackUI.Instance.ShowRoundResult(
                localTeam,
                winnerTeam,
                winReason,
                scoreA,
                scoreB
            );
        }

        if (TopInfoUI.Instance != null)
        {
            TopInfoUI.Instance.StopLocalCountdown();
            TopInfoUI.Instance.SetScoreByTeam(scoreA, scoreB);
        }
        else
        {
            Debug.LogError("[ROUND RESULT] TopInfoUI.Instance is null");
        }

        if (InGameScoreboardUI.Instance != null)
        {
            InGameScoreboardUI.Instance.OnRoundResult(
                currentRound,
                winnerTeam,
                winReason,
                scoreA,
                scoreB
            );
        }

        if (matchManager != null)
        {
            matchManager.OnRecvRoundResult(
                winnerTeam,
                scoreA,
                scoreB,
                currentRound,
                matchEnd != 0,
                delaySeconds
            );
        }
        else
        {
            Debug.LogError("[NETWORK] matchManager is null. RoundResult ignored.");
        }
    }

    private void HandleRoundReset(byte[] body)
    {
        ResetLocalCombatReport();

        Purchases.Reset();
        ScannerNetworkManager.Instance?.ClearAll();
        DischargeDirector.Instance?.ClearAll();
        ScannerFeedbackUI.Instance?.ResetFeedback();
        if(body!=null && body.Length>=4) movementRound=body[0];
        if (CombatFeedbackUI.Instance != null)
            CombatFeedbackUI.Instance.ResetAll();

        if (KillLogManager.Instance != null)
        {
            KillLogManager.Instance.ClearAll();
        }

        if (WeaponDropSpawner.Instance != null)
        {
            WeaponDropSpawner.Instance.ClearAllDroppedWeapons();
        }

        droppedItemMap.Clear();

        if (StorePanelUI.Instance != null)
        {
            StorePanelUI.Instance.ResetOwnedItemsForNewRound();
        }

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.ResetAllPlayerDeadVisuals();
        }

        int offset = 0;

        byte currentRound = body[offset];
        offset += 1;

        byte scoreA = body[offset];
        offset += 1;

        byte scoreB = body[offset];
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        Debug.Log(
            $"[ROUND RESET] round={currentRound}, scoreA={scoreA}, scoreB={scoreB}, memberCount={memberCount}"
        );

        ClearAllSpikeObjectsForNewRound();

        if (TopInfoUI.Instance != null)
        {
            TopInfoUI.Instance.SetScoreByTeam(scoreA, scoreB);
            TopInfoUI.Instance.SetGameDescription("구매 단계");
        }

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnServerRoundReset(
                currentRound,
                scoreA,
                scoreB
            );
        }

        for (int i = 0; i < memberCount; i++)
        {
            uint playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            byte actualTeam = body[offset];
            offset += 1;

            byte spawnTeam = body[offset];
            offset += 1;

            byte slotIndex = body[offset];
            offset += 1;

            int hp = BitConverter.ToInt32(body, offset);
            offset += 4;

            int barrier = BitConverter.ToInt32(body, offset);
            offset += 4;

            int maxHp = BitConverter.ToInt32(body, offset);
            offset += 4;

            int maxBarrier = BitConverter.ToInt32(body, offset);
            offset += 4;

            int currentWeaponId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int magAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int reserveAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int primaryWeaponId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int primaryMagAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int primaryReserveAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int primarySkinId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int pistolWeaponId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int pistolMagAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int pistolReserveAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int pistolSkinId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int knifeWeaponId = BitConverter.ToInt32(body, offset);
            offset += 4;

            int knifeMagAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int knifeReserveAmmo = BitConverter.ToInt32(body, offset);
            offset += 4;

            int knifeSkinId = BitConverter.ToInt32(body, offset);
            offset += 4;

            byte purchasedSkillMask = body[offset];
            offset += 1;

            byte usedSkillMask = body[offset];
            offset += 1;

            if (playerId == MyPlayerId)
            {
                SkillInventoryState.ApplyServerState(
                    purchasedSkillMask,
                    usedSkillMask
                );
            }

            if (playerId == MyPlayerId && MiniMapCamera.Instance != null)
            {
                MiniMapCamera.Instance.SetLocalRoundRole(spawnTeam);
            }

            PlayerCombatSnapshot snapshot = GetOrCreateCombatSnapshot(playerId);

            snapshot.hp = hp;
            snapshot.barrier = barrier;
            snapshot.maxHp = maxHp;
            snapshot.maxBarrier = maxBarrier;

            snapshot.weaponId = currentWeaponId;
            snapshot.magAmmo = magAmmo;
            snapshot.reserveAmmo = reserveAmmo;
            snapshot.isADS = false;

            ApplyRoundResetMember(
                playerId,
                actualTeam,
                spawnTeam,
                slotIndex,
                hp,
                barrier,
                maxHp,
                maxBarrier,
                currentWeaponId,
                magAmmo,
                reserveAmmo
            );

            ApplyRoundResetInventoryToPlayer(
                playerId,
                currentWeaponId,
                primaryWeaponId,
                primaryMagAmmo,
                primaryReserveAmmo,
                primarySkinId,
                pistolWeaponId,
                pistolMagAmmo,
                pistolReserveAmmo,
                pistolSkinId,
                knifeWeaponId,
                knifeMagAmmo,
                knifeReserveAmmo,
                knifeSkinId
            );
        }

        RefreshPlayerTagsByTeam();
    }

    private void ApplyRoundResetMember(
    uint playerId,
    byte actualTeam,
    byte spawnTeam,
    byte slotIndex,
    int hp,
    int barrier,
    int maxHp,
    int maxBarrier,
    int currentWeaponId,
    int magAmmo,
    int reserveAmmo)
    {
        PlayerIdentity[] identities =
            FindObjectsByType<PlayerIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        PlayerIdentity targetIdentity = null;

        for (int i = 0; i < identities.Length; i++)
        {
            if (identities[i] == null)
                continue;

            if (identities[i].PlayerId == playerId)
            {
                targetIdentity = identities[i];
                break;
            }
        }

        if (targetIdentity == null)
        {
            Debug.LogWarning($"[ROUND RESET] player not found. playerId={playerId}");
            return;
        }

        targetIdentity.Team = actualTeam;

        bool isMine = playerId == MyPlayerId;

        if (isMine && SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.StopSpectating();
        }

        MapInfo mapInfo = FindFirstObjectByType<MapInfo>();

        if (mapInfo == null)
        {
            Debug.LogError("[ROUND RESET] MapInfo not found");
            return;
        }

        Transform spawnPoint = mapInfo.GetSpawnPoint(spawnTeam, slotIndex);

        if (spawnPoint == null)
        {
            Debug.LogError($"[ROUND RESET] spawnPoint not found slot={slotIndex}");
            return;
        }

        CharacterController cc =
            targetIdentity.GetComponent<CharacterController>();

        if (cc != null)
            cc.enabled = false;

        targetIdentity.transform.position = spawnPoint.position;
        targetIdentity.transform.rotation = spawnPoint.rotation;

        if (cc != null)
            cc.enabled = true;

        PlayerHP playerHP = targetIdentity.GetComponent<PlayerHP>();

        if (playerHP != null)
        {
            playerHP.SetServerHP(
                hp,
                barrier,
                maxHp,
                maxBarrier
            );
        }

        PlayerDeathController death =
            targetIdentity.GetComponent<PlayerDeathController>();

        if (death != null)
            death.OnRespawn();

        PlayerMovement movement =
            targetIdentity.GetComponent<PlayerMovement>();

        PlayerCameraController cameraController =
            targetIdentity.GetComponent<PlayerCameraController>();

        if (isMine)
        {
            if (InGameUIManager.Instance != null)
            {
                InGameUIManager.Instance.SetHpBarrier(
                    hp,
                    barrier,
                    maxHp,
                    maxBarrier
                );

                InGameUIManager.Instance.SetBulletText(
                    magAmmo,
                    reserveAmmo
                );
            }

            if (movement != null)
            {
                movement.SetLocalPlayer(true);
                movement.CanMove = true;
            }

            if (cameraController != null)
            {
                cameraController.SetLocalPlayer(true);
            }

            FPSCameraLook look =
                targetIdentity.GetComponentInChildren<FPSCameraLook>(true);

            if (look != null)
                look.ResetLookRotation(spawnPoint.rotation.eulerAngles.y);
        }
        else
        {
            if (movement != null)
                movement.SetLocalPlayer(false);

            if (cameraController != null)
                cameraController.SetLocalPlayer(false);
        }

        Debug.Log(
            $"[ROUND RESET APPLY] playerId={playerId}, team={actualTeam}, slot={slotIndex}, pos={spawnPoint.position}, isMine={isMine}"
        );
    }

    private void ApplyRoundResetInventoryToPlayer(
    uint playerId,
    int currentWeaponId,
    int primaryWeaponId,
    int primaryMagAmmo,
    int primaryReserveAmmo,
    int primarySkinId,
    int pistolWeaponId,
    int pistolMagAmmo,
    int pistolReserveAmmo,
    int pistolSkinId,
    int knifeWeaponId,
    int knifeMagAmmo,
    int knifeReserveAmmo,
    int knifeSkinId)
    {
        PlayerIdentity target = FindPlayerIdentityById(playerId);

        if (target == null)
        {
            Debug.LogWarning($"[ROUND RESET INVENTORY] player not found. playerId={playerId}");
            return;
        }

        PlayerItem item = target.GetComponent<PlayerItem>();

        if (item == null)
            item = target.GetComponentInChildren<PlayerItem>(true);

        if (item == null)
        {
            Debug.LogWarning($"[ROUND RESET INVENTORY] PlayerItem not found. playerId={playerId}");
            return;
        }

        item.ApplyRoundResetInventoryFromServer(
            currentWeaponId,
            primaryWeaponId,
            primaryMagAmmo,
            primaryReserveAmmo,
            primarySkinId,
            pistolWeaponId,
            pistolMagAmmo,
            pistolReserveAmmo,
            pistolSkinId,
            knifeWeaponId,
            knifeMagAmmo,
            knifeReserveAmmo,
            knifeSkinId
        );

        // Reapply the current weapon only after the round inventory is rebuilt.
        item.SetWeaponVisibility(true, this);

        PlayerCameraController roundResetCamera =
            target.GetComponent<PlayerCameraController>();

        if (roundResetCamera == null)
        {
            roundResetCamera =
                target.GetComponentInChildren<PlayerCameraController>(true);
        }

        if (roundResetCamera != null)
            roundResetCamera.RefreshViewVisuals();

        if (playerId == MyPlayerId)
        {
            if (StorePanelUI.Instance != null)
            {
                StorePanelUI.Instance.ResetOwnedItemsForNewRound();

                if (primaryWeaponId >= 0)
                    StorePanelUI.Instance.SetOwnedWeaponFromServer(primaryWeaponId);

                if (pistolWeaponId >= 0)
                    StorePanelUI.Instance.SetOwnedWeaponFromServer(pistolWeaponId);

                if (knifeWeaponId >= 0)
                    StorePanelUI.Instance.SetOwnedWeaponFromServer(knifeWeaponId);

                // 방어구를 0으로 초기화
                StorePanelUI.Instance.SetOwnedArmorFromServer(0);
            }
        }

        Debug.Log(
            $"[ROUND RESET INVENTORY] playerId={playerId}, current={currentWeaponId}, " +
            $"primary={primaryWeaponId}, pistol={pistolWeaponId}, knife={knifeWeaponId}"
        );
    }

    private void HandleMatchResult(byte[] body)
    {
        int offset = 0;

        byte winnerTeam = body[offset];
        offset += 1;

        byte scoreA = body[offset];
        offset += 1;

        byte scoreB = body[offset];
        offset += 1;

        Debug.Log(
            $"[MATCH RESULT] winnerTeam={winnerTeam}, scoreA={scoreA}, scoreB={scoreB}"
        );

        SceneNavigation.Load(ProjectVector.Core.ProjectScene.Result);
    }

    public void SendReload()
    {
        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_RELOAD;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        client.Send(buffer);

        Debug.Log("[SEND RELOAD]");
    }

    private void HandleAmmoUpdate(byte[] body)
    {
        if (body == null || body.Length < 16)
        {
            Debug.LogWarning(
                $"[AMMO UPDATE] invalid body length={body?.Length ?? 0}"
            );
            return;
        }

        int offset = 0;

        uint playerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        int weaponId = BitConverter.ToInt32(body, offset);
        offset += 4;

        int magAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        int reserveAmmo = BitConverter.ToInt32(body, offset);
        offset += 4;

        PlayerCombatSnapshot snapshot =
            GetOrCreateCombatSnapshot(playerId);

        snapshot.weaponId = weaponId;
        snapshot.magAmmo = magAmmo;
        snapshot.reserveAmmo = reserveAmmo;

        PlayerIdentity identity =
            FindPlayerIdentityById(playerId);

        if (identity != null)
        {
            PlayerItem item =
                identity.GetComponent<PlayerItem>();

            if (item == null)
                item = identity.GetComponentInChildren<PlayerItem>(true);

            if (item != null)
            {
                item.SetWeaponAmmoFromServer(
                    weaponId,
                    magAmmo,
                    reserveAmmo
                );
            }
        }

        bool isSpectating =
            SpectatorManager.Instance != null &&
            SpectatorManager.Instance.IsSpectating;

        if (playerId == MyPlayerId &&
            !isSpectating &&
            InGameUIManager.Instance != null)
        {
            InGameUIManager.Instance.SetBulletText(
                magAmmo,
                reserveAmmo
            );
        }

        if (SpectatorManager.Instance != null)
        {
            SpectatorManager.Instance.NotifyPlayerStateChanged(
                playerId
            );
        }

        Debug.Log(
            $"[AMMO UPDATE] playerId={playerId}, " +
            $"weaponId={weaponId}, ammo={magAmmo}/{reserveAmmo}"
        );
    }

    public void SendFire()
    {
        if (StorePanelUI.IsBlockingGameplayInput)
            return;

        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_FIRE;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        client.Send(buffer);

        Debug.Log("[SEND FIRE]");
    }

    public void SendKnifeAttack()
    {
        if (StorePanelUI.IsBlockingGameplayInput)
            return;

        ushort size = 4;
        ushort type =
            (ushort)PacketType.PKT_CS_KNIFE_ATTACK;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(
            BitConverter.GetBytes(size),
            0,
            buffer,
            offset,
            2
        );
        offset += 2;

        Buffer.BlockCopy(
            BitConverter.GetBytes(type),
            0,
            buffer,
            offset,
            2
        );

        client.Send(buffer);

        Debug.Log("[SEND KNIFE ATTACK]");
    }

    private string resultMapName;
    public void SendRoomStateRequest(uint roomId)
    {
        ushort size = 8;
        ushort type = (ushort)PacketType.PKT_CS_ROOM_STATE_REQUEST;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(roomId), 0, buffer, offset, 4);
        offset += 4;

        client.Send(buffer);

        Debug.Log($"[SEND ROOM STATE REQUEST] roomId={roomId}");
    }

    private void OnApplicationQuit()
    {
        Debug.Log("[NETWORK] OnApplicationQuit");
        client?.Close();
    }

    private void HandleScoreboardUpdate(byte[] body)
    {
        int offset = 0;

        ScoreboardData data = new ScoreboardData();

        data.roomId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        data.myTeam = body[offset];
        offset += 1;

        byte memberCount = body[offset];
        offset += 1;

        for (int i = 0; i < memberCount; i++)
        {
            ScoreboardMemberData member = new ScoreboardMemberData();

            member.playerId = BitConverter.ToUInt32(body, offset);
            offset += 4;

            member.team = body[offset];
            offset += 1;

            member.slotIndex = body[offset];
            offset += 1;

            member.characterId = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.nickname = Packet.ReadFixedString(body, offset, 20);
            offset += 20;

            member.kills = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.deaths = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.assists = BitConverter.ToInt32(body, offset);
            offset += 4;

            member.credits = BitConverter.ToInt32(body, offset);
            offset += 4;

            if(playerCombatSnapshots.TryGetValue(member.playerId,out var state) && state.hasHpState){member.hasLifeState=true;member.equipmentAlive=state.hp>0;}
            data.members.Add(member);
        }

        if(latestScoreboardData!=null && latestScoreboardData.roomId==data.roomId)
        { data.firstHalfRounds=latestScoreboardData.firstHalfRounds;data.secondHalfRounds=latestScoreboardData.secondHalfRounds; }
        latestScoreboardData = data;

        if (InGameScoreboardUI.Instance != null)
            InGameScoreboardUI.Instance.OnRecvScoreboardUpdate(data);

        if (StorePanelUI.Instance != null)
            StorePanelUI.Instance.RefreshPlayerInfo();
    }

    private void HandleRoundTimer(byte[] body)
    {
        int offset = 0;

        ushort remainingSeconds = BitConverter.ToUInt16(body, offset);
        offset += 2;

        Debug.Log($"[ROUND TIMER] remaining={remainingSeconds}");

        if (TopInfoUI.Instance != null)
        {
            TopInfoUI.Instance.SetGameDescription("전투 단계");
            TopInfoUI.Instance.SetTimer(remainingSeconds);
        }
        else
        {
            Debug.LogError("[ROUND TIMER] TopInfoUI.Instance is null");
        }
    }

    public void SendInGameChat(InGameChatChannel channel, string message)
    {
        message = message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(message))
            return;

        const int messageSize = 256;

        ushort size = 4 + 1 + messageSize;

        ushort type = (ushort)PacketType.PKT_CS_IN_GAME_CHAT;

        byte[] buffer = new byte[size];

        int offset = 0;

        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, offset, 2);
        offset += 2;

        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, offset, 2);
        offset += 2;

        buffer[offset] = (byte)channel;
        offset += 1;

        Packet.WriteFixedString(buffer, offset, message, messageSize);

        client.Send(buffer);

        Debug.Log($"[SEND IN GAME CHAT] " + $"channel={channel}, message={message}");
    }

    private void HandleInGameChat(byte[] body)
    {
        const int nicknameSize = 20;
        const int messageSize = 256;
        const int requiredBodySize = 4 + 1 + 1 + nicknameSize + messageSize;

        if (body == null || body.Length < requiredBodySize)
        {
            Debug.LogWarning($"[IN GAME CHAT] invalid body length=" + $"{body?.Length ?? 0}");
            return;
        }

        int offset = 0;

        uint senderPlayerId = BitConverter.ToUInt32(body, offset);
        offset += 4;

        InGameChatChannel channel = (InGameChatChannel)body[offset];
        offset += 1;

        byte senderTeam = body[offset];
        offset += 1;

        string nickname = Packet.ReadFixedString(body, offset, nicknameSize);
        offset += nicknameSize;

        string message = Packet.ReadFixedString(body, offset, messageSize);
        offset += messageSize;

        if (InGameChatUI.Instance != null)
        {
            InGameChatUI.Instance.OnReceiveChat(senderPlayerId, channel, senderTeam, nickname, message);
        }
        else
        {
            Debug.LogWarning(
                "[IN GAME CHAT] " +
                "InGameChatUI.Instance is null"
            );
        }
    }

    public void SendFavorLevelRequest()
    {
        ushort size = 4;
        ushort type = (ushort)PacketType.PKT_CS_FAVOR_LEVEL_REQUEST;

        byte[] buffer = new byte[size];
        Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(type), 0, buffer, 2, 2);

        client.Send(buffer);
        Debug.Log("[SEND] FavorLevelRequest");
    }

    private void HandleFavorLevelResponse(byte[] body)
    {
        if (body == null || body.Length < 1) return;

        int offset = 0;
        int count = body[offset++];

        var favorDict = new Dictionary<int, int>();

        for (int i = 0; i < count; i++)
        {
            if (offset + 8 > body.Length) break;

            int characterId = BitConverter.ToInt32(body, offset); offset += 4;
            int favorLevel = BitConverter.ToInt32(body, offset); offset += 4;

            favorDict[characterId] = favorLevel;

            Debug.Log($"[RECV] FavorLevel characterId={characterId}, level={favorLevel}");
        }

        // DialogueManager에 전달
        DialogueManager.Instance?.OnRecvFavorLevels(favorDict);
    }

}
