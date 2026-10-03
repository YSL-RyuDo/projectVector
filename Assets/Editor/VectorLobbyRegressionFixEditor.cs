using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Surgical authoring repair: no runtime layout creation and no account writes.
public static class VectorLobbyRegressionFixEditor
{
    const string ScenePath = "Assets/Scenes/Lobby.unity";
    const string FriendPath = "Assets/Resources/Friend/FriendListItem.prefab";
    static readonly string[] MenuFields = { "memoButton", "messageButton", "profileButton", "careerButton", "deleteButton", "blockButton", "inviteButton", "contextCloseButton" };
    static T Ref<T>(UnityEngine.Object owner, string field) where T : UnityEngine.Object =>
        (T)new SerializedObject(owner).FindProperty(field).objectReferenceValue;
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static object Invoke(object owner, string method, params object[] args) => owner.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, args);
    static void Place(RectTransform rect, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    public static void LayoutFriendMenu(FriendListItemUI item)
    {
        var menu = Ref<GameObject>(item, "contextMenu").GetComponent<RectTransform>();
        float width = menu.rect.width;
        menu.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 472);
        Place(Ref<TMP_Text>(item, "contextNameText").rectTransform, 8, width - 24, 44);
        for (int i = 0; i < MenuFields.Length; ++i)
        {
            var button = Ref<Button>(item, MenuFields[i]);
            Place((RectTransform)button.transform, 60 + i * 50, width - 24, 44);
            // The original labels have differing anchors. Keep each inside its button.
            foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(6, 0);
                label.rectTransform.offsetMax = new Vector2(-6, 0);
                label.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    [MenuItem("Project Vector/UI/Repair Party and Profile Layout")]
    public static void RepairAndVerify()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play first");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; ++i)
            Require(!UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty, "Unsaved scene; refusing overwrite");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (string name in new[] { "BattleCustomScroll", "SkinCustomScroll" })
        {
            var scroll = all.Single(t => t.name == name).GetComponent<ScrollRect>();
            scroll.movementType = ScrollRect.MovementType.Clamped;
            EditorUtility.SetDirty(scroll);
        }
        var game = all.Select(t => t.GetComponent<GamePanelController>()).First(c => c != null);
        var ready = Ref<GameObject>(game, "tempGameRoomPanel").transform;
        for (int i = 1; i <= 2; ++i)
        {
            var waiting = ready.Find("TeamPosition" + i + "/Waiting");
            Require(waiting != null, "Missing authored waiting label");
            waiting.gameObject.SetActive(false);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        Require(EditorSceneManager.SaveScene(scene), "Scene save failed");
        var prefab = PrefabUtility.LoadPrefabContents(FriendPath);
        try { LayoutFriendMenu(prefab.GetComponent<FriendListItemUI>()); PrefabUtility.SaveAsPrefabAsset(prefab, FriendPath); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        Verify();
    }

    public static void Verify()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach (string name in new[] { "BattleCustomScroll", "SkinCustomScroll" })
            Require(all.Single(t => t.name == name).GetComponent<ScrollRect>().movementType == ScrollRect.MovementType.Clamped, name + " overscroll");
        var prefab = PrefabUtility.LoadPrefabContents(FriendPath);
        try
        {
            var item = prefab.GetComponent<FriendListItemUI>();
            float previousBottom = 0;
            foreach (string field in MenuFields)
            {
                var rect = (RectTransform)Ref<Button>(item, field).transform;
                float top = -rect.anchoredPosition.y;
                Require(top >= previousBottom + 5, "Overlapping menu button: " + field);
                previousBottom = top + rect.rect.height;
            }
            Require(previousBottom < Ref<GameObject>(item, "contextMenu").GetComponent<RectTransform>().rect.height, "Menu overflow");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        var test = new GameObject("OfflineNetworkRegression");
        try
        {
            var net = test.AddComponent<NetworkManager>();
            byte[] state = new byte[17];
            BitConverter.GetBytes(42u).CopyTo(state, 0);
            BitConverter.GetBytes(42u).CopyTo(state, 5);
            state[4] = 1;
            Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_STATE, state);
            Require(net.MyPlayerId == 42 && net.IsPartyLeader && net.PartyReadyToQueue, "Solo identity/start");
            for (byte count = 2; count <= 3; count++)
            {
                state[4] = count;
                for (int i = 1; i < count; i++) BitConverter.GetBytes((uint)(42 + i)).CopyTo(state, 5 + i * 4);
                Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_STATE, state);
                Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_AVAILABILITY, new byte[] { count, (byte)((1 << count) - 1) });
                Require(net.IsPartyLeader && net.PartyReadyToQueue, "Leader cannot start group size " + count);
                Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_AVAILABILITY, new byte[] { count, 1 });
                Require(!net.PartyReadyToQueue, "Non-lobby member must block start");
            }
            typeof(NetworkManager).GetField("myPlayerId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(net, 43u);
            Require(!net.IsPartyLeader, "Member must not start leader's party");
            Invoke(net, "ResetSocialState");
            Require(net.MyPlayerId == 0, "Identity leaked across reconnect");
        }
        finally { UnityEngine.Object.DestroyImmediate(test); }
        var craft = all.Select(t => t.GetComponent<MyPageChipCraftController>()).First(c => c != null);
        craft.SetTarget("offline-test");
        craft.ApplyData(new ChipCraftData { userId = "offline-test", isSelf = true, ownedCardMask = 7, ownedCaseMask = 1, ownedMedalMask = 1, preset = new ChipCraftPresetData() });
        craft.Open();
        Invoke(craft, "SelectCard", 2);
        Invoke(craft, "EnterEdit");
        var draft = (ChipCraftPresetData)typeof(MyPageChipCraftController).GetField("draft", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(craft);
        Require(draft.cardId == 2, "Browse selection lost entering edit");
        craft.ApplySaveResult(true, draft);
        var applied = (ChipCraftData)typeof(MyPageChipCraftController).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(craft);
        Require(applied.preset.cardId == 2, "Save acknowledgement not applied");
        var profile = all.Select(t => t.GetComponent<ProfilePanelController>()).First(c => c != null);
        var friends = all.Select(t => t.GetComponent<FriendPanelController>()).First(c => c != null);
        var appearanceTest = new GameObject("OfflineAppearanceRegression");
        try
        {
            var net = appearanceTest.AddComponent<NetworkManager>();
            for (byte i = 0; i < 3; i++)
            {
                var response = new byte[58]; response[1] = i;
                Invoke(net, "HandleChipCraftSaveResult", response);
                var expected = Resources.Load<Sprite>("PlayerIcon/" + i);
                Require(expected != null && profile.currentplayerIconId == i && Ref<Image>(profile, "playerIcon").sprite == expected, "Header icon not synchronized");
                Require(Ref<Image>(friends, "myProfileIcon").sprite == expected, "Open friend pane icon stale");
            }
            var failed = new byte[58]; failed[0] = 1; failed[1] = 0;
            Invoke(net, "HandleChipCraftSaveResult", failed);
            Require(profile.currentplayerIconId == 2, "Failed save changed appearance");
        }
        finally { UnityEngine.Object.DestroyImmediate(appearanceTest); }
        Debug.Log("LOBBY_REGRESSION_OK: saved menu spacing, clamped scrolls, solo/duo/trio leader gating, nonleader/availability guards, reconnect reset, chipcraft selection/save. No account writes.");
        // All test state after the earlier SaveScene is intentionally discarded.
    }

    public static void VerifyPartyMode()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var game = all.Select(t => t.GetComponent<GamePanelController>()).First(c => c != null);
        var ui = all.Select(t => t.GetComponent<LobbySocialController>()).First(c => c != null);
        var profile = all.Select(t => t.GetComponent<ProfilePanelController>()).First(c => c != null);
        var modeField = typeof(GamePanelController).GetField("currentGameMode", BindingFlags.NonPublic | BindingFlags.Instance);
        string Mode() => modeField.GetValue(game).ToString();
        var host = new GameObject("OfflinePartyModeTest");
        var previousNetwork = NetworkManager.Instance;
        try
        {
            var net = host.AddComponent<NetworkManager>(); NetworkManager.Instance = net;
            typeof(GamePanelController).GetField("_profilePanelController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(game, profile);
            typeof(NetworkManager).GetField("myPlayerId", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(net, 43u);
            Invoke(ui, "Awake");
            var state = new byte[17]; state[4] = 2;
            BitConverter.GetBytes(42u).CopyTo(state, 0); BitConverter.GetBytes(42u).CopyTo(state, 5); BitConverter.GetBytes(43u).CopyTo(state, 9);
            Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_STATE, state);
            var mode = new byte[5]; BitConverter.GetBytes(42u).CopyTo(mode, 0); mode[4] = 1;
            Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_MODE, mode);
            Require(Mode() == "Rank" && !Ref<Button>(game, "rankGameButton").interactable, "Leader mode did not select member rank tab");
            game.OnClickNormalGameButton();
            Require(ui.confirmationPanel.activeSelf && Mode() == "Rank", "Member mode switch skipped leave confirmation");
            ui.cancelButton.onClick.Invoke();
            Require(!ui.confirmationPanel.activeSelf && Mode() == "Rank" && net.PartyPlayers.Count == 2, "Cancel changed party/mode");
            game.OnClickUserRoomButton(); ui.confirmButton.onClick.Invoke();
            Require(Mode() == "Rank", "Navigation happened before server leave acknowledgement");
            var solo = new byte[17]; solo[4] = 1;
            BitConverter.GetBytes(43u).CopyTo(solo, 0); BitConverter.GetBytes(43u).CopyTo(solo, 5);
            Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_STATE, solo);
            Require(Mode() == "Rank", "Must wait for complete membership/card snapshot");
            BitConverter.GetBytes(43u).CopyTo(mode, 0);
            Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_MODE, mode);
            Require(Mode() == "Custom" && Ref<GameObject>(game, "roomListPanel").activeSelf, "Confirmed leave did not open custom browser");
            // A stale packet from the previous party must not restore its tab.
            BitConverter.GetBytes(42u).CopyTo(mode, 0);
            Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_MODE, mode);
            Require(Mode() == "Custom", "Stale party packet changed the selected mode");
            Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_STATE, state);
            Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_MODE, mode);
            game.OnClickNormalGameButton(); ui.confirmButton.onClick.Invoke();
            game.CancelPendingPartyNavigation();
            Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_STATE, solo);
            BitConverter.GetBytes(43u).CopyTo(mode, 0);
            Invoke(net, "HandleSocialPacket", PacketType.PKT_SC_PARTY_MODE, mode);
            Require(Mode() == "Rank", "Failed leave retained a deferred navigation callback");
            Require(all.Length == scene.GetRootGameObjects().Where(r => r != host).Sum(r => r.GetComponentsInChildren<Transform>(true).Length), "Mode navigation created replacement UI");
            Debug.Log("PARTY_MODE_UI_OK: leader tab sync, member leave prompt/cancel, wait-for-server navigation, stale packet rejection, failure cleanup; existing authored UI only.");
        }
        finally { NetworkManager.Instance = previousNetwork; UnityEngine.Object.DestroyImmediate(host); }
    }

    public static void VerifyPartyAppearance()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("PARTY_APPEARANCE_TEST: scene loaded");
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var game = all.Select(t => t.GetComponent<GamePanelController>()).First(c => c != null);
        var profile = all.Select(t => t.GetComponent<ProfilePanelController>()).First(c => c != null);
        var navigation = game.GetComponent<NavigationPanelController>();
        var slotsProperty = new SerializedObject(game).FindProperty("tempGamePlayerSlots");
        var slots = Enumerable.Range(0, 3).Select(i => (TempGamePlayerSlotUI)slotsProperty.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        Debug.Log("PARTY_APPEARANCE_TEST: authored references loaded");
        var host = new GameObject("OfflinePartyAppearanceTest");
        var previousNetwork = NetworkManager.Instance;
        try
        {
            var net = host.AddComponent<NetworkManager>(); NetworkManager.Instance = net;
            Debug.Log("PARTY_APPEARANCE_TEST: offline network ready");
            net.SetGamePanelController(game);
            typeof(GamePanelController).GetField("_profilePanelController", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(game, profile);
            profile.currentNickname = "LYS";
            navigation.currentLobbyMainMenu = LobbyMainMenu.Game;
            byte[] Snapshot(int count, bool modern)
            {
                int stride = modern ? 90 : 33;
                var body = new byte[2 + 3 * stride]; body[1] = (byte)count;
                for (int i = 0; i < count; i++)
                {
                    int offset = 2 + i * stride; body[offset] = (byte)i;
                    System.Text.Encoding.UTF8.GetBytes(i == 0 ? "LYS" : "TEAM" + i).CopyTo(body, offset + 1);
                    BitConverter.GetBytes(2 - i).CopyTo(body, offset + 21);
                    if (modern) body[offset + 33] = (byte)(2 - i);
                }
                return body;
            }
            foreach (bool modern in new[] { false, true })
                for (int count = 1; count <= 3; count++)
                {
                    Invoke(net, "HandleTempGameRoomEnter", Snapshot(count, modern));
                    for (int i = 0; i < 3; i++)
                    {
                        Require(slots[i].gameObject.activeSelf == (i < count), "Incorrect occupied slot visibility");
                        if (i >= count) continue;
                        Require(slots[i].Nickname == (i == 0 ? "LYS" : "TEAM" + i), "Empty fixed-capacity slot overwrote nickname");
                        Require(Ref<Image>(slots[i], "iconImage").sprite == Resources.Load<Sprite>("IconCard/icon" + (2 - i) + "_card"), "Occupied card art overwritten");
                    }
                }
            var duo = Snapshot(2, true);
            Invoke(net, "HandleTempGameRoomEnter", duo);
            game.OnRecvTempGameStartResult(0, 0, 1, 12, 0, 0);
            navigation.currentLobbyMainMenu = LobbyMainMenu.Lobby;
            Ref<GameObject>(game, "gamePanel").SetActive(false);
            Invoke(net, "HandleTempGameRoomEnter", duo);
            Require(!Ref<GameObject>(game, "gamePanel").activeSelf, "Appearance snapshot interrupted another page");
            Require(GamePanelController.IsMatchmakingActive, "Appearance snapshot cancelled matchmaking");
            // Saving while the party page is hidden must update the existing local slot.
            for (byte card = 0; card < 3; card++)
            {
                var ack = new byte[58]; ack[1] = card; ack[3] = 1;
                BitConverter.GetBytes(.1f + card * .1f).CopyTo(ack, 5);
                BitConverter.GetBytes(.5f).CopyTo(ack, 17);
                Invoke(net, "HandleChipCraftSaveResult", ack);
                Require(slots[0].Nickname == "LYS", "Save cleared local nickname");
                Require(Ref<Image>(slots[0], "iconImage").sprite == Resources.Load<Sprite>("IconCard/icon" + card + "_card"), "Hidden local card did not refresh");
                Require(profile.currentplayerIconId == card, "Header/card mapping diverged");
                var medal = (Image)new SerializedObject(slots[0]).FindProperty("medalImages").GetArrayElementAtIndex(0).objectReferenceValue;
                Require(medal.gameObject.activeSelf && Mathf.Approximately(medal.rectTransform.localScale.x, .5f), "Saved medal transform not applied");
            }
            // A peer update uses the same authoritative snapshot without losing the self card.
            BitConverter.GetBytes(0).CopyTo(duo, 113); duo[125] = 0;
            Invoke(net, "HandleTempGameRoomEnter", duo);
            Require(slots[0].Nickname == "LYS" && slots[1].Nickname == "TEAM1", "Live peer update lost names");
            Require(Ref<Image>(slots[1], "iconImage").sprite == Resources.Load<Sprite>("IconCard/icon0_card"), "Live peer art not updated");
            Require(all.Length == scene.GetRootGameObjects().Where(r => r != host).Sum(r => r.GetComponentsInChildren<Transform>(true).Length), "Appearance refresh created replacement UI");
            navigation.SelectLobbyMainMenu(LobbyMainMenu.Game);
            game.OnRecvTempGameStartResult(0, 2, 1, 0, 0, 0);
            Invoke(net, "HandleTempGameRoomEnter", duo);
            // Batch execution does not advance a frame for the normal LateUpdate sync.
            foreach (var reflection in Ref<GameObject>(game, "gamePanel").GetComponentsInChildren<VectorCardReflection>(true))
            {
                reflection.Refresh();
                for (int i = 0; i < Mathf.Min(reflection.sources.Length, reflection.reflections.Length); i++)
                    Require(reflection.sources[i].sprite == reflection.reflections[i].sprite, "Card reflection retained previous art");
            }
            if (UnityEngine.SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                VectorExperiencePolishEditor.CaptureCanvas(Ref<GameObject>(game, "gamePanel").GetComponentInParent<Canvas>(), "VectorPartyAppearanceFixed.png");
            Debug.Log("PARTY_APPEARANCE_OK: legacy/modern solo-duo-trio zero-filled slots, live self/peer art, nickname, icon mapping, medal transform, hidden-page and queued-state preservation; no scene/account writes.");
        }
        finally { NetworkManager.Instance = previousNetwork; UnityEngine.Object.DestroyImmediate(host); }
    }

    public static void VerifyAndCapture()
    {
        Verify();
        var prefab = PrefabUtility.LoadPrefabContents(FriendPath);
        var canvas = new GameObject("DiagnosticMenuCanvas", typeof(Canvas)).GetComponent<Canvas>();
        var camera = new GameObject("DiagnosticMenuCamera", typeof(Camera)).GetComponent<Camera>();
        try
        {
            canvas.gameObject.layer = 5;
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(720, 650);
            camera.orthographic = true; camera.orthographicSize = 325; camera.aspect = 1920f / 1080;
            camera.transform.position = new Vector3(0, 0, -10); camera.cullingMask = 1 << 5; camera.farClipPlane = 50;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(30, 34, 42, 255);
            canvas.worldCamera = camera;
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (root != canvas.gameObject && root != camera.gameObject) root.SetActive(false);
            var menu = UnityEngine.Object.Instantiate(Ref<GameObject>(prefab.GetComponent<FriendListItemUI>(), "contextMenu"), canvas.transform);
            menu.SetActive(true);
            var rect = (RectTransform)menu.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero; rect.localScale = Vector3.one;
            typeof(VectorSocialPresentationEditor).GetMethod("Render", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { camera, menu, "VectorFriendMenuFixed.png" });
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
            UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
        }
    }
}
