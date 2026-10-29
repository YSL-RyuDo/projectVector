using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Explicit edit-mode checks. Does not authenticate, open microphone or modify saved scenes.
public static class VectorVoiceVerification
{
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        Require(Application.isBatchMode, "Dedicated batch only");
        Require(VectorVoiceChat.TransmissionAllowed(true,false,true,false,true,false,true,false), "PTT press");
        Require(!VectorVoiceChat.TransmissionAllowed(true,false,true,false,true,false,false,false), "PTT release");
        Require(!VectorVoiceChat.TransmissionAllowed(true,false,true,false,true,false,true,true), "Held key on channel entry");
        foreach (bool automatic in new[] { false, true })
        {
            Require(!VectorVoiceChat.TransmissionAllowed(false,false,true,false,true,automatic,true,false), "No authorized channel");
            Require(!VectorVoiceChat.TransmissionAllowed(true,true,true,false,true,automatic,true,false), "Self mute");
            Require(!VectorVoiceChat.TransmissionAllowed(true,false,false,false,true,automatic,true,false), "Focus loss");
            Require(!VectorVoiceChat.TransmissionAllowed(true,false,true,true,true,automatic,true,false), "Modal/chat input");
            Require(!VectorVoiceChat.TransmissionAllowed(true,false,true,false,false,automatic,true,false), "No microphone");
        }
        Require(VectorVoiceChat.TransmissionAllowed(true,false,true,false,true,true,false,false), "Automatic mode");
        Require(VectorVoiceChat.ParticipantSdkVolume(100) == 0 && VectorVoiceChat.ParticipantSdkVolume(0) == -50 &&
            VectorVoiceChat.ParticipantSdkVolume(200) == 50 && VectorVoiceChat.ParticipantSdkVolume(999) == 50, "Peer volume boundaries");
        Require(VectorVoiceChat.ParticipantPreferenceKey("a", "b", "Volume") != VectorVoiceChat.ParticipantPreferenceKey("c", "b", "Volume"), "Per-listener isolation");
        Require(VectorVoiceChat.ParticipantPreferenceKey("a", "b", "Volume") != VectorVoiceChat.ParticipantPreferenceKey("a", "c", "Volume"), "Per-peer isolation");
        Debug.Log("VOICE_VERIFY: PASS input policy: PTT/release/re-entry/automatic/mute/focus/modal/missing mic");
        var scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Login/Scenes/Login.unity");
        var network = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<NetworkManager>(true)).Single();
        Require(network.GetComponent<VectorVoiceChat>() != null, "Missing persistent voice service");
        foreach (string name in new[] { "Lobby", "Game_TestVersion", "Result", "RankResult" })
        {
            scene = EditorSceneManager.OpenScene(ProjectEditorPaths.Scene(name));
            var ui = scene.GetRootGameObjects().Single(r => r.name == "VoiceUI").GetComponent<VectorVoicePanel>();
            Require(ui != null && !ui.panel.activeSelf, name + ": default modal must be closed");
            Require(ui.openButton && ui.closeButton && ui.modeButton && ui.microphoneButton && ui.inputDeviceButton && ui.outputDeviceButton, "Missing authored buttons");
            Require(ui.statusText && ui.modeText && ui.microphoneText && ui.inputDeviceText && ui.outputDeviceText && ui.speakingText, "Missing authored labels");
            Require(ui.participantTexts.Length == 8 && ui.participantTexts.All(t => t) && ui.participantMuteButtons.Length == 8 && ui.participantMuteButtons.All(b => b), "Eight authored participant rows");
            Require(!ui.openButton.gameObject.activeSelf, "Voice shortcut must default hidden");
            Require(ui.panel.GetComponentInChildren<ScrollRect>(true).movementType == ScrollRect.MovementType.Clamped, "Bounded participant scrolling");
            Require(ui.inputVolume.minValue == -50 && ui.inputVolume.maxValue == 50 && ui.outputVolume.handleRect, "Volume range/handle");
            Require(ui.participantCount && ui.participantVolumes.All(s => s && s.minValue == 0 && s.maxValue == 200 && s.handleRect && s.fillRect), "Peer sliders");
            Require(ui.participantStates.All(t => t) && ui.participantVolumeLabels.All(t => t) && ui.participantIcons.All(t => t) &&
                ui.participantActivity.All(t => t) && ui.participantResetButtons.All(t => t) && ui.participantControls.All(t => t), "Peer presentation references");
            if (name == "Lobby") Require(ui.partyButton && ui.partyView && ui.privateRoomView, "Party/private shortcut references");
            if (name == "Game_TestVersion") Require(ui.hudRows.Length == 3 && ui.hudRows.All(r => r) &&
                ui.hudNames.All(t => t) && ui.hudIcons.All(i => i) && ui.hudActivity.All(i => i), "In-game voice HUD");
            foreach (var label in ui.GetComponentsInChildren<TMP_Text>(true)) Require(label.font != null, "Missing font: " + label.name);
            Debug.Log("VOICE_VERIFY: saved UI references valid: " + name);
        }
        // Only clone the authored voice UI into an empty editor scene for a deterministic preview.
        scene = EditorSceneManager.OpenScene("Assets/ProjectVector/Modules/Match/Scenes/Game_TestVersion.unity");
        var source = scene.GetRootGameObjects().Single(r => r.name == "VoiceUI");
        var preview = UnityEngine.Object.Instantiate(source);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(preview,
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive));
        EditorSceneManager.CloseScene(scene, true);
        var binding = preview.GetComponent<VectorVoicePanel>();
        binding.panel.SetActive(true);
        binding.statusText.text = "팀 음성";
        binding.inputDeviceText.text = "기본 마이크"; binding.outputDeviceText.text = "기본 헤드셋";
        binding.participantCount.text = "3 / 3";
        string[] names = { "LYS  (나)", "KSW", "CJJ" };
        string[] states = { "내 마이크 · 왼쪽에서 설정", "말하는 중", "음소거됨" };
        for (int i = 0; i < 3; i++)
        {
            binding.participantTexts[i].text = names[i];
            binding.participantStates[i].text = states[i];
            binding.participantIcons[i].sprite = Resources.Load<Sprite>("PlayerIcon/" + i);
            binding.participantIcons[i].enabled = true;
            binding.participantControls[i].SetActive(i != 0);
            binding.participantVolumes[i].SetValueWithoutNotify(i == 1 ? 80 : 100);
            binding.participantVolumeLabels[i].text = i == 1 ? "80%" : "100%";
            binding.hudRows[i].SetActive(true); binding.hudNames[i].text = names[i];
            binding.hudIcons[i].sprite = binding.participantIcons[i].sprite;
        }
        binding.participantStates[1].color = binding.participantActivity[1].color = new Color(.33f,.85f,.72f);
        binding.participantActivity[2].color = new Color(.88f,.32f,.40f);
        binding.participantMuteButtons[2].GetComponentInChildren<TMP_Text>().text = "음소거 해제";
        binding.hudNames[1].color = binding.hudActivity[1].color = new Color(.33f,.85f,.72f);
        binding.hudActivity[2].color = new Color(.88f,.32f,.40f); binding.hudNames[2].text = "CJJ · 음소거";
        var camera = new GameObject("PreviewCamera", typeof(Camera)).GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.15f, .17f, .2f);
        var canvas = preview.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 2;
        var target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;
        Canvas.ForceUpdateCanvases();
        foreach (var text in preview.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
        camera.Render();
        var previous = RenderTexture.active; RenderTexture.active = target;
        var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0,0,1920,1080),0,0); texture.Apply();
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", "VectorVoice-Settings.png");
        File.WriteAllBytes(path, texture.EncodeToPNG());
        RenderTexture.active = previous;
        UnityEngine.Object.DestroyImmediate(texture); camera.targetTexture = null;
        target.Release(); UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("VOICE_VERIFY: PASS. UI preview: " + path + ". Live audio has NOT been tested.");
    }
}
