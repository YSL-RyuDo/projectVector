using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Incremental authoring migration: preserve the existing controls and their bindings.
public static class VoiceRoomUpgrade
{
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) throw new Exception("Unsaved scene; refusing overwrite");
        foreach (string name in new[] { "Lobby", "Game_TestVersion", "Result", "RankResult" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var roots = scene.GetRootGameObjects();
            var ui = roots.SelectMany(r => r.GetComponentsInChildren<VectorVoicePanel>(true)).Single();
            ui.openButton.gameObject.SetActive(false);
            ui.panel.SetActive(false);
            if (name == "Lobby")
            {
                var room = roots.SelectMany(r => r.GetComponentsInChildren<RoomPanelUI>(true)).Single();
                ui.privateRoomView = (GameObject)new SerializedObject(room).FindProperty("roomPanel").objectReferenceValue;
            }
            if (ui.participantTexts.Length < 8)
            {
                var viewport = new GameObject("ParticipantViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
                var vr = (RectTransform)viewport.transform; vr.SetParent(ui.panel.transform, false);
                vr.anchorMin = vr.anchorMax = vr.pivot = new Vector2(0, 1);
                vr.anchoredPosition = new Vector2(428, -230); vr.sizeDelta = new Vector2(642, 408);
                viewport.GetComponent<Image>().color = Color.clear;
                var content = new GameObject("Participants", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                var cr = (RectTransform)content.transform; cr.SetParent(vr, false);
                cr.anchorMin = new Vector2(0, 1); cr.anchorMax = Vector2.one; cr.pivot = new Vector2(.5f, 1);
                cr.sizeDelta = Vector2.zero;
                var layout = content.GetComponent<VerticalLayoutGroup>(); layout.spacing = 14;
                layout.childControlWidth = true; layout.childControlHeight = true;
                layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
                content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var scroll = viewport.GetComponent<ScrollRect>(); scroll.viewport = vr; scroll.content = cr;
                scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 32;
                var rows = ui.participantTexts.Select(t => t.transform.parent.gameObject).ToList();
                foreach (var row in rows) row.transform.SetParent(cr, false);
                while (rows.Count < 8)
                {
                    var row = UnityEngine.Object.Instantiate(rows[0], cr); row.name = "ParticipantRow" + rows.Count; rows.Add(row);
                }
                foreach (var row in rows) { var le = row.AddComponent<LayoutElement>(); le.preferredHeight = le.minHeight = 122; }
                ui.participantTexts = rows.Select(r => r.transform.Find("Nickname").GetComponent<TMP_Text>()).ToArray();
                ui.participantStates = rows.Select(r => r.transform.Find("State").GetComponent<TMP_Text>()).ToArray();
                ui.participantIcons = rows.Select(r => r.transform.Find("ProfileIcon").GetComponent<Image>()).ToArray();
                ui.participantActivity = rows.Select(r => r.transform.Find("Activity").GetComponent<Image>()).ToArray();
                ui.participantControls = rows.Select(r => r.transform.Find("ListeningControls").gameObject).ToArray();
                ui.participantMuteButtons = rows.Select(r => r.transform.Find("ListeningControls/Mute").GetComponent<Button>()).ToArray();
                ui.participantResetButtons = rows.Select(r => r.transform.Find("ListeningControls/Reset").GetComponent<Button>()).ToArray();
                ui.participantVolumes = rows.Select(r => r.transform.Find("ListeningControls/Volume").GetComponent<Slider>()).ToArray();
                ui.participantVolumeLabels = rows.Select(r => r.transform.Find("ListeningControls/VolumeValue").GetComponent<TMP_Text>()).ToArray();
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("VOICE_ROOM_UPGRADE_PASS: saved 8 participant rows, clamped viewport and private-room binding");
    }
}
