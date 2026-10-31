using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

public static class VectorPickExperienceEditor
{
    const string ScenePath = "Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity";
    static TMP_FontAsset font;
    static readonly Color White = new Color32(245, 243, 236, 255);
    static readonly Color Gold = new Color32(246, 208, 114, 255);
    static readonly Color Dark = new Color32(32, 32, 31, 210);
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Ref<T>(Object owner, string field) where T : Object => new SerializedObject(owner).FindProperty(field).objectReferenceValue as T;
    static void Invoke(Object owner, string method, params object[] args) => owner.GetType().GetMethod(method, Private).Invoke(owner, args);
    static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene: refused");
    }
    static T Ensure<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>(); return c != null ? c : go.AddComponent<T>();
    }
    static void Link(Object owner, string field, Object value)
    {
        var so = new SerializedObject(owner); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Pose(Transform t, float x, float y, float w, float h)
    {
        var rect = (RectTransform)t; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); rect.localScale = Vector3.one;
    }
    static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect == null) { rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); }
        Pose(rect, x, y, w, h); rect.gameObject.layer = 5; return rect;
    }
    static void TextStyle(TMP_Text text, float size, Color color)
    {
        if (text.font == null) text.font = font;
        text.fontSize = size; text.enableAutoSizing = false; text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
    }
    static TMP_Text Label(Transform parent, string name, string value, float x, float y, float w, float h, float size, Color color)
    {
        var text = Ensure<TextMeshProUGUI>(Rect(parent, name, x, y, w, h).gameObject); text.font = font; TextStyle(text, size, color); text.text = value; return text;
    }
    static Image Box(Transform parent, string name, float x, float y, float w, float h, Color color)
    {
        var image = Ensure<Image>(Rect(parent, name, x, y, w, h).gameObject); image.color = color; image.raycastTarget = false; return image;
    }
    static Transform Find(Transform parent, string name) => parent.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    static void StopLayout(Transform root)
    {
        foreach (var layout in root.GetComponentsInChildren<LayoutGroup>(true)) layout.enabled = false;
        foreach (var fitter in root.GetComponentsInChildren<ContentSizeFitter>(true)) fitter.enabled = false;
    }
    static Sprite ReferenceSurface(string name)
    {
        string path = "Assets/ProjectVector/Modules/Match/UI/Surfaces/" + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteBorder = new Vector4(1, 2, 1, 2); importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Project Vector/UI/Install Reference Pick Experience")]
    public static void InstallAndVerify()
    {
        Guard();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Fonts/Pretendard-Black SDF.asset");
        var confirmArt = ReferenceSurface("ConfirmSurface");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        // User explicitly retained the original attack/defence, score and timer header.
        // Remove only our newly authored replacement, never the original HUD objects.
        foreach (var priorView in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PickSelectionPresentation>(true)))
        {
            var replacement = priorView.transform.Find("PickHeader");
            if (replacement != null) Object.DestroyImmediate(replacement.gameObject);
        }
        var preserved = new VectorExperiencePolishEditor.Preservation(scene);
        var pick = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PickManager>(true)).Single();
        var info = Ref<GameObject>(pick, "characterInfoPanel").transform;
        var panel = info.parent;
        StopLayout(panel);
        // Decorative surfaces never intercept button input.
        foreach (var graphic in panel.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        foreach (var button in panel.GetComponentsInChildren<Button>(true))
        {
            if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
            var nav = button.navigation; nav.mode = Navigation.Mode.None; button.navigation = nav;
        }
        var view = Ensure<PickSelectionPresentation>(panel.gameObject);
        Link(pick, "presentation", view);
        var top = Ref<TopInfoUI>(pick, "topInfoUI");
        var originalHeaderGroup = top.GetComponent<CanvasGroup>();
        if (originalHeaderGroup != null) { originalHeaderGroup.alpha = 1; originalHeaderGroup.blocksRaycasts = true; }
        var map = Find(panel, "MapText"); Pose(map, 66, 35, 330, 42); TextStyle(map.GetComponent<TMP_Text>(), 31, White); view.mapLabel = map.GetComponent<TMP_Text>();
        var matchLabel = Find(panel, "GameRoundDescriptionText"); Pose(matchLabel, 66, 79, 330, 32); TextStyle(matchLabel.GetComponent<TMP_Text>(), 20, White);
        Pose(Find(panel, "GameInfoImage"), 48, 38, 5, 69); Find(panel, "GameInfoImage").GetComponent<Image>().color = Gold;
        Box(panel, "MapReadability", 38, 24, 335, 97, new Color32(32, 32, 31, 180)).transform.SetAsFirstSibling();

        var team = Find(panel, "UserPick"); Pose(team, 48, 220, 340, 428);
        Label(team, "TeamHeading", "OUR TEAM", 0, -46, 340, 36, 19, White);
        view.teammateStates = new TMP_Text[3];
        var slots = team.GetComponentsInChildren<TeamPickSlotUI>(true).OrderBy(s => s.name).ToArray();
        for (int i = 0; i < slots.Length; i++)
        {
            var row = Box(team, "TeamRow" + i, 0, i * 128, 340, 112, new Color32(32, 32, 31, 180)).transform;
            var slot = slots[i]; slot.transform.SetParent(row, false); Pose(slot.transform, 10, 17, 78, 78);
            var portrait = Ref<Image>(slot, "slotImage");
            if (portrait == null) { portrait = slot.GetComponent<Image>(); Link(slot, "slotImage", portrait); }
            portrait.type = Image.Type.Simple; portrait.preserveAspect = true; portrait.raycastTarget = false;
            preserved.AllowSpriteChange(portrait);
            portrait.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectVector/Shared/UI/Surfaces/MatchFlow/EmptySeat.png");
            var name = Ref<TMP_Text>(slot, "nicknameText"); Pose(name.transform, 94, 5, 226, 36); TextStyle(name, 25, White);
            var so = new SerializedObject(slot); so.FindProperty("normalNicknameColor").colorValue = White; so.FindProperty("myNicknameColor").colorValue = Gold; so.ApplyModifiedPropertiesWithoutUndo();
            view.teammateStates[i] = Label(row, "PickStatus", "캐릭터를 고르는 중", 105, 63, 218, 28, 18, new Color32(205, 204, 198, 255));
            foreach (string action in new[] { "changeButton", "shareButton" })
            {
                var button = Ref<Button>(slot, action); if (button != null) Pose(button.transform, action == "changeButton" ? 265 : 298, 4, 24, 24);
            }
        }
        var scroll = Find(panel, "CharacterScroll"); Pose(scroll, 510, 874, 785, 192);
        scroll.GetComponent<Image>().color = new Color32(32, 32, 31, 200);
        var viewport = Find(scroll, "Viewport"); Pose(viewport, 0, 0, 785, 192);
        viewport.GetComponent<Image>().color = Color.white;
        var stripMask = viewport.GetComponent<Mask>(); if (stripMask != null) stripMask.showMaskGraphic = false;
        var content = Find(scroll, "Content"); Pose(content, 0, 0, 785, 192);
        var stripScroll = scroll.GetComponent<ScrollRect>(); if (stripScroll != null) { stripScroll.horizontal = stripScroll.vertical = false; stripScroll.inertia = false; }
        view.characterHighlights = new GameObject[4];
        var buttons = content.GetComponentsInChildren<PickCharacterButtonUI>(true).OrderBy(b => b.name).ToArray();
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i]; Pose(button.transform, 37 + i * 191, 8, 144, 144);
            var portrait = Ref<Image>(button, "characterImage"); if (portrait == null) portrait = button.GetComponent<Image>(); portrait.type = Image.Type.Simple; portrait.preserveAspect = true;
            var label = Label(button.transform, "CharacterLabel", pick.GetCharacterData(i).characterName, 0, 144, 144, 32, 19, White); label.alignment = TextAlignmentOptions.Center;
            view.characterHighlights[i] = Box(button.transform, "CurrentPickAccent", 0, -3, 144, 5, Gold).gameObject; view.characterHighlights[i].SetActive(false);
            foreach (string field in new[] { "myPickObject", "teamPickObject" })
            {
                var badge = Ref<GameObject>(button, field); Pose(badge.transform, 0, 114, 144, 30); badge.GetComponent<Image>().color = field == "myPickObject" ? new Color32(152, 24, 39, 245) : Dark;
            }
            var nickname = Ref<TMP_Text>(button, "nicknameText"); Pose(nickname.transform, 0, 114, 144, 30); TextStyle(nickname, 18, White); nickname.alignment = TextAlignmentOptions.Center;
        }
        var select = Ref<Button>(pick, "selectButton"); Pose(select.transform, 730, 759, 345, 66);
        var surface = select.GetComponent<Image>(); surface.color = new Color32(188, 27, 49, 255); view.confirmSurface = surface;
        var confirmFill = Box(select.transform, "ReferenceConfirmFill", 0, 0, 345, 66, Color.white); confirmFill.sprite = confirmArt; confirmFill.type = Image.Type.Sliced;
        confirmFill.transform.SetAsFirstSibling(); view.confirmSurface = confirmFill;
        var colors = select.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1, .91f, .85f); colors.selectedColor = Color.white; colors.disabledColor = new Color(.7f, .7f, .7f, 1); select.colors = colors;
        var selectText = select.GetComponentInChildren<TMP_Text>(true); TextStyle(selectText, 28, White); selectText.alignment = TextAlignmentOptions.Center; selectText.text = "선택 확정";
        view.selectionStatus = Label(panel, "SelectionStatus", "캐릭터를 고른 뒤 선택을 확정하세요", 550, 832, 700, 34, 21, White); view.selectionStatus.alignment = TextAlignmentOptions.Center;

        Pose(info, 1380, 174, 490, 820); info.GetComponent<Image>().color = new Color32(32, 30, 27, 241);
        Pose(Ref<Image>(pick, "characterImage").transform, 24, 22, 82, 82);
        Ref<Image>(pick, "characterImage").type = Image.Type.Simple; Ref<Image>(pick, "characterImage").preserveAspect = true;
        var charName = Ref<TMP_Text>(pick, "characterNameText"); Pose(charName.transform, 128, 54, 335, 43); TextStyle(charName, 33, White);
        var caption = Find(info, "CharacterRole").GetComponent<TMP_Text>(); Pose(caption.transform, 165, 20, 280, 28); TextStyle(caption, 19, Gold); view.roleCaption = caption;
        var skillRow = Find(info, "CharacterSkills"); Pose(skillRow, 26, 142, 440, 112);
        var role = Ref<Image>(pick, "characterRoleImage"); role.transform.SetParent(info, false); Pose(role.transform, 126, 17, 28, 30); role.color = Gold;
        foreach (var roleText in role.GetComponentsInChildren<TMP_Text>(true)) roleText.text = "";
        view.skillHighlights = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            string key = new[] { "c", "q", "e", "x" }[i];
            var button = Ref<Button>(pick, key + "Button"); Pose(button.transform, 7 + i * 110, 0, 85, 76);
            var frame = Box(skillRow, "SkillFrame" + i, i * 110, -6, 99, 106, new Color32(255, 255, 255, 15)); frame.transform.SetAsFirstSibling();
            var icon = Ref<Image>(pick, key + "Image"); icon.color = White; icon.type = Image.Type.Simple; icon.preserveAspect = true; icon.raycastTarget = true;
            var keyLabel = button.GetComponentInChildren<TMP_Text>(true); Pose(keyLabel.transform, 0, 77, 85, 23); TextStyle(keyLabel, 18, White); keyLabel.alignment = TextAlignmentOptions.Center;
            var highlight = Box(skillRow, "SelectedSkill" + i, i * 110, 102, 99, 4, Gold); view.skillHighlights[i] = highlight.gameObject; highlight.gameObject.SetActive(i == 0);
        }
        view.skillKey = Label(info, "SkillKey", "C  /  스킬 상세", 26, 287, 420, 29, 18, Gold);
        var skillName = Ref<TMP_Text>(pick, "skillNameText"); Pose(skillName.transform, 26, 323, 438, 48); TextStyle(skillName, 30, White);
        var skillScroll = Rect(info, "SkillDescriptionScroll", 26, 388, 438, 347);
        var scrollRect = Ensure<ScrollRect>(skillScroll.gameObject);
        var clip = Rect(skillScroll, "Viewport", 0, 0, 420, 347); Ensure<RectMask2D>(clip.gameObject);
        var scrollHit = Ensure<Image>(clip.gameObject); scrollHit.color = new Color(0, 0, 0, .001f); scrollHit.raycastTarget = true;
        var textRoot = Rect(clip, "Content", 0, 0, 414, 347);
        var description = Ref<TMP_Text>(pick, "skillInfoText"); description.transform.SetParent(textRoot, false); Pose(description.transform, 0, 0, 414, 347);
        TextStyle(description, 24, White); description.alignment = TextAlignmentOptions.TopLeft; description.lineSpacing = 12;
        // Saved native text-fitting component: no runtime construction or scripted screen layout.
        var textLayout = Ensure<VerticalLayoutGroup>(textRoot.gameObject); textLayout.enabled = true;
        textLayout.childControlWidth = textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true; textLayout.childForceExpandHeight = false;
        var textFit = Ensure<ContentSizeFitter>(textRoot.gameObject); textFit.enabled = true;
        textFit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; textFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scrollbar = Ensure<Scrollbar>(Box(skillScroll, "Scrollbar", 432, 0, 4, 347, new Color32(255, 255, 255, 25)).gameObject);
        var handle = Box(scrollbar.transform, "Handle", 0, 0, 4, 90, new Color32(235, 226, 208, 180)); handle.raycastTarget = true;
        handle.rectTransform.anchorMin = Vector2.zero; handle.rectTransform.anchorMax = Vector2.one;
        handle.rectTransform.offsetMin = handle.rectTransform.offsetMax = Vector2.zero;
        scrollbar.handleRect = handle.rectTransform; scrollbar.targetGraphic = handle; scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollRect.viewport = clip; scrollRect.content = textRoot; scrollRect.horizontal = false; scrollRect.vertical = true; scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped; scrollRect.scrollSensitivity = 30; scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        view.skillScroll = scrollRect;
        Box(info, "FooterRule", 26, 764, 438, 1, new Color32(255, 255, 255, 40));
        Label(info, "SkillHelp", "아이콘을 클릭해 스킬 확인  ·  휠로 스크롤", 26, 777, 440, 24, 17, new Color32(190, 188, 179, 255));
        EditorUtility.SetDirty(view);
        // Direct editor property writes on prefab instances need explicit saved overrides.
        foreach (var transform in panel.GetComponentsInChildren<Transform>(true))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject)) PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject);
            foreach (var component in transform.GetComponents<Component>())
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        preserved.Check();
        EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Pick redesign save failed");
        AssetDatabase.SaveAssets();
        Verify();
    }

    public static void Verify()
    {
        VectorPickMapEditor.Verify();
        Debug.Log("PICK_EXPERIENCE_VERIFIED: 4 characters x C/Q/E/X pointer hits, snapshots/lock/timer retain skill, new character resets skill, scroll; original upper HUD retained.");
    }

    internal static void VerifySkillInteraction(PickManager pick, PickStateData state)
    {
        var view = Ref<PickSelectionPresentation>(pick, "presentation");
        if (view == null) return;
        foreach (var portraitButton in view.GetComponentsInChildren<PickCharacterButtonUI>(true))
        {
            var portrait = Ref<Image>(portraitButton, "characterImage"); if (portrait == null) portrait = portraitButton.GetComponent<Image>();
            if (portrait.type != Image.Type.Simple || !portrait.preserveAspect) throw new Exception("Character portrait can stretch after scene reload");
            if (Mathf.Abs(portrait.rectTransform.rect.width - portrait.rectTransform.rect.height) > .01f) throw new Exception("Portrait thumbnail frame is not square");
        }
        foreach (var slot in view.GetComponentsInChildren<TeamPickSlotUI>(true))
        {
            var portrait = Ref<Image>(slot, "slotImage");
            if (portrait.type != Image.Type.Simple || !portrait.preserveAspect || portrait.sprite == null) throw new Exception("Team portrait/default artwork missing after reload");
        }
        if (view.transform.Find("PickHeader") != null) throw new Exception("User's original header was replaced");
        var top = Ref<TopInfoUI>(pick, "topInfoUI");
        var headerGroup = top.GetComponent<CanvasGroup>();
        if (headerGroup != null && headerGroup.alpha != 1) throw new Exception("Original upper HUD hidden");
        var canvas = view.GetComponentInParent<Canvas>();
        using var pointerCanvas = new PointerCanvasScope(canvas);
        var raycaster = canvas.GetComponent<GraphicRaycaster>();
        var eventSystem = pick.gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EventSystem>(true)).Single();
        var name = Ref<TMP_Text>(pick, "skillNameText");
        var body = Ref<TMP_Text>(pick, "skillInfoText");
        var me = state.members.Single(m => m.isMe);
        var character = pick.GetCharacterData(me.hoverCharacterId);
        var skills = new[] { character.cSkill, character.qSkill, character.eSkill, character.xSkill };
        int Count() => pick.gameObject.scene.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<Transform>(true).Length);
        int before = Count();
        void Rebuild()
        {
            Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(view.skillScroll.content);
            LayoutRebuilder.ForceRebuildLayoutImmediate(view.skillScroll.transform as RectTransform); Canvas.ForceUpdateCanvases();
            pointerCanvas.Render();
        }
        for (int index = 0; index < 4; index++)
        {
            string key = new[] { "c", "q", "e", "x" }[index];
            var button = Ref<Button>(pick, key + "Button"); Rebuild();
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(eventSystem) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : raycaster.eventCamera, rect.TransformPoint(rect.rect.center)) };
            var hits = new List<RaycastResult>(); raycaster.Raycast(pointer, hits);
            if (hits.Count == 0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != button.gameObject)
                throw new Exception($"Skill {key} pointer intercepted by {(hits.Count > 0 ? Path(hits[0].gameObject.transform) : "nothing")}; pointer={pointer.position} screen={Screen.width}x{Screen.height} canvas={((RectTransform)canvas.transform).rect} scale={canvas.scaleFactor}");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Rebuild();
            if (name.text != skills[index].skillName || body.text != skills[index].skillDescription)
                throw new Exception("Wrong skill after clicking " + key);
            if (!view.skillHighlights[index].activeSelf || view.skillHighlights.Count(g => g.activeSelf) != 1)
                throw new Exception("Skill highlight does not match description");
            float beforeScroll = view.skillScroll.verticalNormalizedPosition;
            if (view.skillScroll.content.rect.height > view.skillScroll.viewport.rect.height + 5)
            {
                pointer.scrollDelta = new Vector2(0, -2);
                ExecuteEvents.ExecuteHierarchy(view.skillScroll.viewport.gameObject, pointer, ExecuteEvents.scrollHandler);
                if (view.skillScroll.verticalNormalizedPosition >= beforeScroll) throw new Exception("Long skill description does not scroll");
                Debug.Log($"PICK_LONG_SKILL_SCROLL: character={character.characterId} skill={key} content={view.skillScroll.content.rect.height} viewport={view.skillScroll.viewport.rect.height} before={beforeScroll} after={view.skillScroll.verticalNormalizedPosition}");
            }
            float readingPosition = view.skillScroll.verticalNormalizedPosition;
            for (int tick = 0; tick < 3; tick++)
            {
                pick.OnRecvPickState(state);
                pick.OnRecvPickTimer(PickPhase.PICKING, (ushort)(19 - tick));
                pick.OnRecvPickLockResult(0, character.characterId);
                Rebuild();
                if (name.text != skills[index].skillName || body.text != skills[index].skillDescription)
                    throw new Exception("Server refresh reset selected skill " + key);
                if (Mathf.Abs(view.skillScroll.verticalNormalizedPosition - readingPosition) > .01f)
                    throw new Exception("Server refresh reset reading scroll position");
            }
            me.isLocked = true; me.lockedCharacterId = character.characterId; pick.OnRecvPickState(state);
            pick.OnRecvPickTimer(PickPhase.PREPARE, 0); pick.OnRecvPickTimer(PickPhase.COMPLETE, 0);
            if (name.text != skills[index].skillName || !button.interactable) throw new Exception("Locking prevents reading skills");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            if (name.text != skills[index].skillName) throw new Exception("Locked skill click wrong");
            me.isLocked = false; me.lockedCharacterId = -1;
            pick.OnRecvPickTimer(PickPhase.PICKING, 20); pick.OnRecvPickState(state);
        }
        int other = (character.characterId + 1) % 4;
        Invoke(pick, "ApplyCharacterInfo", other);
        if (name.text != pick.GetCharacterData(other).cSkill.skillName || !view.skillHighlights[0].activeSelf)
            throw new Exception("Character change retained previous character's skill");
        Invoke(pick, "ApplyCharacterInfo", character.characterId);
        Ref<Button>(pick, "qButton").onClick.Invoke(); Rebuild();
        // Stress a longer future/localized description without changing the saved character data.
        string originalDescription = character.qSkill.skillDescription;
        try
        {
            character.qSkill.skillDescription = string.Join("\n", Enumerable.Repeat(originalDescription, 6));
            Ref<Button>(pick, "qButton").onClick.Invoke(); Rebuild();
            if (view.skillScroll.content.rect.height <= view.skillScroll.viewport.rect.height)
                throw new Exception("Authored scroll content does not grow for long descriptions");
            view.skillScroll.verticalNormalizedPosition = 1;
            var wheel = new PointerEventData(eventSystem) { scrollDelta = new Vector2(0, -2) };
            ExecuteEvents.ExecuteHierarchy(view.skillScroll.viewport.gameObject, wheel, ExecuteEvents.scrollHandler);
            float scrolled = view.skillScroll.verticalNormalizedPosition;
            if (scrolled >= .999f) throw new Exception("Long description wheel input failed");
            pick.OnRecvPickState(state); Rebuild();
            if (Mathf.Abs(view.skillScroll.verticalNormalizedPosition - scrolled) > .01f)
                throw new Exception("Refresh lost long-description scroll position");
            Debug.Log($"PICK_LONG_SKILL_VERIFIED: character={character.characterId} content={view.skillScroll.content.rect.height} viewport={view.skillScroll.viewport.rect.height} normalized={scrolled}; state refresh preserves reading position");
        }
        finally
        {
            character.qSkill.skillDescription = originalDescription;
            Ref<Button>(pick, "qButton").onClick.Invoke(); Rebuild(); view.skillScroll.verticalNormalizedPosition = 1;
        }
        if (Count() != before) throw new Exception("Skill browsing constructed UI");
        Debug.Log($"PICK_SKILLS_VERIFIED: character={character.characterId} all 4 raycast hits, correct details/highlight, scroll, refresh, lock and phase persistence");
    }

    // A render-backed UI fixture: batch mode has no Game view, so overlay graphics otherwise have depth -1.
    // Changes are restored and never saved; runtime receives the authored screen-space Canvas.
    sealed class PointerCanvasScope : IDisposable
    {
        readonly Canvas canvas;
        readonly Camera camera, oldCamera;
        readonly RenderMode mode;
        readonly Vector3 position, scale;
        readonly Vector2 size;
        readonly RenderTexture target;
        readonly UnityEngine.Rendering.RenderPipelineAsset pipeline, quality;
        public PointerCanvasScope(Canvas source)
        {
            canvas = source; mode = source.renderMode; oldCamera = source.worldCamera;
            position = source.transform.position; scale = source.transform.localScale; size = ((RectTransform)source.transform).sizeDelta;
            pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline; quality = QualitySettings.renderPipeline;
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null; QualitySettings.renderPipeline = null;
            source.renderMode = RenderMode.WorldSpace; source.transform.position = Vector3.zero; source.transform.localScale = Vector3.one;
            ((RectTransform)source.transform).sizeDelta = new Vector2(1920, 1080);
            camera = new GameObject("DiagnosticPickPointerCamera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 540;
            camera.aspect = 16f / 9; camera.farClipPlane = 100; camera.cullingMask = 1 << 5;
            target = new RenderTexture(1920, 1080, 24); camera.targetTexture = target; source.worldCamera = camera;
        }
        public void Render()
        {
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>()) { graphic.SetAllDirty(); graphic.Rebuild(CanvasUpdate.PreRender); }
            foreach (var text in canvas.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases(); camera.Render();
        }
        public void Dispose()
        {
            canvas.renderMode = mode; canvas.worldCamera = oldCamera; canvas.transform.position = position; canvas.transform.localScale = scale;
            ((RectTransform)canvas.transform).sizeDelta = size;
            camera.targetTexture = null; Object.DestroyImmediate(camera.gameObject); Object.DestroyImmediate(target);
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = quality;
            Canvas.ForceUpdateCanvases();
        }
    }
    public static void InspectAndReproduce()
    {
        Guard();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var pick = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<PickManager>(true)).Single();
        var so = new SerializedObject(pick);
        var buttons = new[] { "cButton", "qButton", "eButton", "xButton" }.Select(p => Ref<Button>(pick, p)).ToArray();
        foreach (var button in buttons) Debug.Log($"PICK_SKILL_BUTTON: {Path(button.transform)} / {button.GetInstanceID()} rect={((RectTransform)button.transform).rect} pos={((RectTransform)button.transform).anchoredPosition}");
        if (buttons.Distinct().Count() != 4) throw new Exception("Skill button references overlap");
        typeof(PickManager).GetField("currentInfoCharacterId", Private).SetValue(pick, 0);
        Invoke(pick, "InitSkillButtons");
        Invoke(pick, "ApplyCharacterInfo", 0);
        buttons[3].onClick.Invoke();
        string before = Ref<TMP_Text>(pick, "skillNameText").text;
        Invoke(pick, "ApplyCharacterInfo", 0);
        Debug.Log($"PICK_SKILL_REPRO: X clicked='{before}', same-character state refresh='{Ref<TMP_Text>(pick, "skillNameText").text}'");
        var panel = Ref<GameObject>(pick, "characterInfoPanel").transform.parent;
        foreach (var t in panel.GetComponentsInChildren<RectTransform>(true))
            Debug.Log($"PICK_LAYOUT: {Path(t)} xy={t.anchoredPosition} size={t.sizeDelta} Image={t.GetComponent<Image>() != null} Text={t.GetComponent<TMP_Text>() != null}");
        foreach (var slot in panel.GetComponentsInChildren<TeamPickSlotUI>(true))
            Debug.Log($"PICK_TEAM_REFS: {slot.name} portrait={Ref<Image>(slot, "slotImage") != null} nickname={Ref<TMP_Text>(slot, "nicknameText") != null}");
    }
}
