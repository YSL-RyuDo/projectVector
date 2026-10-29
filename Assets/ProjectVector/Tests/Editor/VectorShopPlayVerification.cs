using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Activated only by the explicit batch entry point, never in a normal editor session.
[InitializeOnLoad]
public static class VectorShopPlayVerification
{
    private const string Flag = "VectorShop.PlayVerification";
    private static int step;
    private static double deadline;
    private static double captureAt;
    private static GameObject canvasRoot;
    private static GachaManager manager;
    private static VectorShopPreview preview;
    private static List<VectorShopItem> items;
    private static List<VectorShopReward> rewards;
    private static bool completed, sawWarp, sawHits, sawReveal;
    private static int advanced;
    private static double holdUntil;
    private static bool capturedCells;
    private static int characterCapture;
    private static readonly string[] characterPrefabs = { "Casual1", "PicoChan", "unitychan", "Controller" };
    static VectorShopPlayVerification() { EditorApplication.update += Update; }
    public static void RunBatch()
    {
        SessionState.SetBool(Flag, true);
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity", OpenSceneMode.Single);
        var shop = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<VectorShopPanel>(true)).Single();
        VectorShopHierarchyInstaller.Validate(shop.transform);
        // Isolate the saved shop fixture from login/match controllers; never save this scene.
        foreach (var script in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
            if (script != null && script.GetType().Assembly == typeof(VectorShopPanel).Assembly &&
                script is not VectorShopPanel && script is not VectorShopPreview && script is not GachaManager && script is not VectorShopNavigation && script is not StorePanelController && script is not MoneyManager)
                script.enabled = false;
        EditorApplication.EnterPlaymode();
    }
    private static void Update()
    {
        if (!SessionState.GetBool(Flag, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (step == 0)
            {
                deadline = EditorApplication.timeSinceStartup + 120;
                var panel = UnityEngine.Object.FindFirstObjectByType<VectorShopPanel>(FindObjectsInactive.Include);
                VectorShopHierarchyInstaller.Validate(panel.transform);
                canvasRoot = panel.gameObject;
                for (var t = panel.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
                manager = panel.sequence;
                preview = panel.preview;
                SeedPreviewFixture(panel);
                if(MoneyManager.Instance==null || MoneyManager.Instance.Money0!=1000000 || MoneyManager.Instance.Money1!=10000) throw new Exception("Shared wallet UI did not synchronize");
                VerifyNavigation(panel);
                panel.ShowDetails();
                var selectedSlot = panel.rewardSlots.First(s => s.itemId >= 1000);
                selectedSlot.root.GetComponent<Button>().onClick.Invoke();
                if (!selectedSlot.root.GetComponent<UnityEngine.UI.Outline>().enabled) throw new Exception("Saved reward button did not select its card");
                var pivot = GameObject.Find("RotationPivot"); var before = pivot.transform.rotation;
                preview.OnDrag(new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, delta = new Vector2(40, 0) });
                if (Quaternion.Angle(before, pivot.transform.rotation) < 1) throw new Exception("Left drag did not rotate model");
                VerifyUnavailableStatDrag();
                items = new List<VectorShopItem> {
                    new VectorShopItem { id = 100, grade = 0, name = "MYTH ONE" },
                    new VectorShopItem { id = 101, grade = 0, name = "MYTH TWO" },
                    new VectorShopItem { id = 102, grade = 2, name = "STANDARD" } };
                rewards = new List<VectorShopReward>(); for (int i = 0; i < 10; ++i) rewards.Add(new VectorShopReward { item = i < 2 ? 100+i : 102 });
                captureAt = EditorApplication.timeSinceStartup + 3;
                step = -1;
            }
            else if (step == -1 && EditorApplication.timeSinceStartup >= captureAt)
            {
                CaptureSavedUI();
                preview.Show(Resources.Load<GameObject>("LobbyCharacter/"+characterPrefabs[0]));
                step = -3;
                captureAt = EditorApplication.timeSinceStartup + 1;
            }
            else if (step == -3 && EditorApplication.timeSinceStartup >= captureAt)
            {
                CaptureSavedUI("GachaFacing_"+characterPrefabs[characterCapture]+".png");
                ++characterCapture;
                captureAt = EditorApplication.timeSinceStartup + 1;
                if(characterCapture==characterPrefabs.Length) step=-2;
                else preview.Show(Resources.Load<GameObject>("LobbyCharacter/"+characterPrefabs[characterCapture]));
            }
            else if (step == -2 && EditorApplication.timeSinceStartup >= captureAt)
            {
                var panel = canvasRoot.GetComponent<VectorShopPanel>();
                panel.CloseDialogs();
                manager.Play(rewards, items, _ => Resources.Load<GameObject>("LobbyCharacter/Casual1"), () =>
                {
                    typeof(VectorShopPanel).GetMethod("ShowResults", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(panel, new object[] { rewards });
                    completed = true;
                });
                step = 1;
            }
            else if (step == 1)
            {
                if(!sawWarp && manager.CurrentPhase == GachaManager.Phase.Warp) {CaptureSavedUI("VectorShopWarp.png");sawWarp=true;}
                if(!sawHits && manager.CurrentPhase == GachaManager.Phase.RingHits)
                {
                    if (manager.ActivatedCellCount != 0) throw new Exception("Ring cells lit before impact");
                    CaptureSavedUI("VectorShopRing.png");sawHits=true;
                }
                if (!capturedCells && manager.ActivatedCellCount >= 4)
                { CaptureSavedUI("VectorShopRingHits.png"); capturedCells=true; }
                if(!sawReveal && manager.CurrentPhase == GachaManager.Phase.Materialize) {CaptureSavedUI("VectorShopReveal.png");sawReveal=true;}
                if (manager.AwaitingAdvance)
                {
                    if (manager.ActivatedCellCount != 10) throw new Exception("Each result must light one separate cell");
                    if (holdUntil == 0) holdUntil = EditorApplication.timeSinceStartup + 3;
                    if (completed || manager.RevealedMythCount != advanced + 1) throw new Exception("Myth advanced without input");
                    if (EditorApplication.timeSinceStartup >= holdUntil)
                    { CaptureSavedUI("VectorShopSettledReveal.png"); manager.Skip(); ++advanced; holdUntil = 0; }
                }
                if (completed)
                {
                    if (advanced != 2 || !manager.IsPlaying || manager.overlay.activeSelf) throw new Exception("Summary transition left the presentation covering results");
                    if (holdUntil == 0) { holdUntil = EditorApplication.timeSinceStartup + 3; return; }
                    if (EditorApplication.timeSinceStartup < holdUntil) return;
                    var panel = canvasRoot.GetComponent<VectorShopPanel>();
                    manager.Skip();
                    if (!panel.resultPanel.activeInHierarchy || panel.resultSlots.Count(s => s.root.activeSelf) != 10 ||
                        panel.resultSlots.Any(s => s.icon.sprite == null || string.IsNullOrWhiteSpace(s.label.text)))
                        throw new Exception("Result cards missing after escape transition");
                    CaptureSavedUI("VectorShopResultSummary.png");
                    panel.CloseDialogs();
                    if (!sawWarp || !sawHits || !sawReveal) throw new Exception("Missing animation phase");
                    completed = false;
                    manager.Play(rewards, items, _ => Resources.Load<GameObject>("LobbyCharacter/Casual1"), () => completed = true);
                    manager.Skip();
                    if (completed) throw new Exception("Intro skip discarded myth rewards");
                    manager.Dismiss();
                    canvasRoot.SetActive(false); step = 2;
                }
            }
            else if (step == 2)
            {
                if (GameObject.Find("GachaPresentationStage") != null || GameObject.Find("ShopPreviewStage") != null) throw new Exception("Leaked presentation stage");
                VectorShopHierarchyInstaller.Validate(canvasRoot.transform);
                Debug.Log("[SHOP PLAY VERIFY] PASS: two myth rewards each wait for input, summary persists, intro skip preserves rewards, unavailable stat drag leaves position unchanged, cleanup.");
                SessionState.SetBool(Flag, false); EditorApplication.Exit(0);
            }
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Shop animation timeout: " + manager.CurrentPhase);
        }
        catch (Exception e) { SessionState.SetBool(Flag, false); Debug.LogException(e); EditorApplication.Exit(1); }
    }
    private static void VerifyUnavailableStatDrag()
    {
        var item = UnityEngine.Object.FindFirstObjectByType<MyPageStatDragItem>(FindObjectsInactive.Include);
        if (item == null) throw new Exception("Missing saved stat drag item");
        var rect = (RectTransform)item.transform;
        var parent = rect.parent; var position = rect.anchoredPosition; int sibling = rect.GetSiblingIndex();
        item.SetAvailable(false);
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, delta = new Vector2(0, 120) };
        item.OnBeginDrag(data); item.OnDrag(data); item.OnEndDrag(data);
        if (rect.parent != parent || rect.anchoredPosition != position || rect.GetSiblingIndex() != sibling)
            throw new Exception("Unavailable stat moved after rejected drag");
    }
    private static void VerifyNavigation(VectorShopPanel panel)
    {
        var nav = panel.GetComponent<VectorShopNavigation>();
        if (nav == null) throw new Exception("Reference shop layout has not been saved to Lobby");
        VectorShopLayoutEditor.Validate(panel.transform);
        nav.tabs[0].GetComponent<Button>().onClick.Invoke();
        CaptureSavedUI("VectorShopRecommendation.png");
        nav.tabs[4].GetComponent<Button>().onClick.Invoke();
        if (nav.CurrentPage != 4 || nav.pages.Count(p => p.activeSelf) != 1) throw new Exception("Catalog tab did not switch saved pages");
        nav.filters[1].GetComponent<Button>().onClick.Invoke();
        if (nav.products.Count(p => p.root.activeSelf) != 4) throw new Exception("Character filter mismatch");
        nav.filters[2].GetComponent<Button>().onClick.Invoke();
        if (nav.products.Count(p => p.root.activeSelf) != 66) throw new Exception("Weapon filter mismatch");
        nav.filters[0].GetComponent<Button>().onClick.Invoke();
        nav.TogglePriceOrder();
        var ordered = nav.products.OrderBy(p => p.root.transform.GetSiblingIndex()).ToArray();
        if (ordered[0].itemId < 1000 || (ordered[0].itemId - 1000) % 100 <= 3) throw new Exception("Ascending price sort mismatch");
        nav.DefaultOrder();
        CaptureSavedUI("VectorShopCatalog.png");
        nav.products[0].root.GetComponent<Button>().onClick.Invoke();
        if (!panel.rewardDetails.activeSelf) throw new Exception("Catalog product did not open reward details");
        for (int i = 1; i <= 3; ++i)
        {
            nav.tabs[i].GetComponent<Button>().onClick.Invoke();
            if (nav.CurrentPage != i || !nav.pages[i].activeSelf || panel.rewardDetails.activeSelf) throw new Exception("Existing shop section navigation mismatch");
        }
        nav.SelectPage(1);
        CaptureSavedUI("VectorShopDrawLayout.png");
        Debug.Log("[SHOP NAV VERIFY] PASS: five saved pages, 70 catalog buttons, character/weapon filters, price order, details and original section navigation.");
    }
    private static void CaptureSavedUI(string fileName = "VectorShopSavedHierarchy.png")
    {
        var canvas = canvasRoot.GetComponentInParent<Canvas>().rootCanvas;
        var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
        var cameraObject = new GameObject("ShopVerificationCamera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>(); cameraObject.transform.position = new Vector3(-3000,-3000,-3000);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.gray;
        var target = new RenderTexture(1920,1080,24); camera.targetTexture = target;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
        // The legacy shop uses already-authored cameras and render textures, not VectorShopPreview.
        foreach(var legacyCamera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if(legacyCamera!=camera && legacyCamera.enabled && legacyCamera.targetTexture!=null) legacyCamera.Render();
        Canvas.ForceUpdateCanvases(); camera.Render();
        var oldTarget = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(1920,1080,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(System.IO.Path.GetTempPath(),fileName),image.EncodeToPNG());
        RenderTexture.active = oldTarget; canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
        camera.targetTexture=null; target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(image); UnityEngine.Object.Destroy(cameraObject);
    }
    private static void SeedPreviewFixture(VectorShopPanel panel)
    {
        // Local response fixture only: never sends a transaction or mutates a server account.
        using var stream = new System.IO.MemoryStream();
        using var writer = new System.IO.BinaryWriter(stream);
        writer.Write(0UL); writer.Write((byte)0); writer.Write(1000000); writer.Write(10000);
        writer.Write((ushort)panel.rewardSlots.Length); writer.Write((ushort)0); writer.Write((ushort)0); writer.Write((ushort)0);
        writer.Write(0); writer.Write(1);
        foreach (var slot in panel.rewardSlots)
        {
            bool weapon=slot.itemId>=1000; int skin=weapon?(slot.itemId-1000)%100:0;
            int grade=weapon?(skin==1?0:skin<=3?1:2):0;
            writer.Write(slot.itemId); writer.Write((byte)(weapon?1:0)); writer.Write((byte)grade);
            writer.Write(weapon?(slot.itemId-1000)/100:slot.itemId-100); writer.Write(skin); writer.Write(grade==0?5000:grade==1?2000:800);
            var name=new byte[64]; var encoded=System.Text.Encoding.UTF8.GetBytes(slot.label.text.Split('\n')[0]);
            Array.Copy(encoded,name,Math.Min(encoded.Length,name.Length)); writer.Write(name);
        }
        writer.Flush(); VectorShopPanel.Receive(stream.ToArray());
    }
}
