using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Authored on NetworkManager in the Login scene; shares its persistent lifetime.
// No runtime UI creation. Credentials and channel membership come from the game server.
public sealed class VectorVoiceChat : MonoBehaviour, IVivoxTokenProvider
{
    public static VectorVoiceChat Instance { get; private set; }
    public bool Automatic { get; private set; }
    public bool MicrophoneMuted { get; private set; }
    public bool Transmitting { get; private set; }
    public int InputVolume { get; private set; }
    public int OutputVolume { get; private set; }
    public string Status { get; private set; } = "음성 연결 대기";
    public string Channel => joinedChannel;
    public byte Scope => FreshContext() ? desired.Scope : (byte)0;
    public int ChannelCapacity => Scope == 3 ? 8 : Scope == 4 ? 2 : 3;
    public string ChannelLabel => Scope == 3 ? "사설방 전체 음성" : Scope == 4 ? "관전자 음성" : Scope == 1 ? "파티 음성" : "팀 음성";
    public bool Ready => initialized && VivoxService.Instance.IsLoggedIn;
    public string InputDevice => Ready ? VivoxService.Instance.EffectiveInputDevice?.DeviceName ?? "마이크 없음" : "연결 대기";
    public string OutputDevice => Ready ? VivoxService.Instance.EffectiveOutputDevice?.DeviceName ?? "출력 장치 없음" : "연결 대기";
    public IReadOnlyList<VivoxParticipant> Participants => Ready && !string.IsNullOrEmpty(joinedChannel) &&
        VivoxService.Instance.ActiveChannels.TryGetValue(joinedChannel, out var members) ? members : Array.Empty<VivoxParticipant>();

    private VoiceServerContext desired;
    private VoiceServerContext loginContext;
    private string joinedChannel = "", loggedAccount = "";
    private bool initialized, polling, reconciling, disposed, changingDevice;
    private bool focused = true, requireKeyRelease = true;
    private float nextPoll, nextReconcile, contextTime = -100, operationStarted;
    private uint sessionPlayer;
    private int contextGeneration;
    // Cosmetic lookup only; membership/mute/volume are always keyed by Vivox account IDs.
    private readonly Dictionary<string, int> profileIcons = new(StringComparer.Ordinal);
    private float nextParticipantSettings;
    private IVivoxService Voice => VivoxService.Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        Automatic = PlayerPrefs.GetInt("VectorVoice.Automatic", 0) == 1;
        MicrophoneMuted = PlayerPrefs.GetInt("VectorVoice.Muted", 0) == 1;
        InputVolume = Mathf.Clamp(PlayerPrefs.GetInt("VectorVoice.Input", 0), -50, 50);
        OutputVolume = Mathf.Clamp(PlayerPrefs.GetInt("VectorVoice.Output", 0), -50, 50);
        SceneManager.activeSceneChanged += SceneChanged;
    }

    private void Update()
    {
        if (disposed) return;
        var network = NetworkManager.Instance;
        bool authenticated = network != null && network.IsConnected && network.IsAuthenticated;
        uint currentPlayer = authenticated ? network.MyPlayerId : 0;
        if (!authenticated || currentPlayer != sessionPlayer)
        {
            if (desired != null || currentPlayer != sessionPlayer) ++contextGeneration;
            desired = null; contextTime = -100; sessionPlayer = currentPlayer;
            requireKeyRelease = true; nextPoll = 0;
        }
        if (authenticated && !polling && Time.unscaledTime >= nextPoll) _ = PollAsync(network, currentPlayer);
        // Expired membership is fail-closed even while a native SDK operation is in flight.
        if (!FreshContext()) desired = null;
        if (initialized)
        {
            bool canHear = FreshContext() && desired != null && desired.Channel == joinedChannel && joinedChannel.Length > 0;
            if (canHear && Voice.IsOutputDeviceMuted) Voice.UnmuteOutputDevice();
            else if (!canHear && !Voice.IsOutputDeviceMuted) Voice.MuteOutputDevice();
        }
        if (!reconciling && Time.unscaledTime >= nextReconcile) _ = ReconcileAsync();
        bool held = Keyboard.current?.backquoteKey.isPressed == true;
        if (!held) requireKeyRelease = false;
        bool blocked = InputBlocked();
        if (blocked && held) requireKeyRelease = true;
        bool channelReady = Ready && !reconciling && !changingDevice && FreshContext() &&
            desired != null && joinedChannel == desired.Channel && joinedChannel.Length > 0;
        bool transmit = TransmissionAllowed(channelReady, MicrophoneMuted, focused && Application.isFocused,
            blocked, Ready && Voice.AvailableInputDevices.Count > 0, Automatic, held, requireKeyRelease);
        SetTransmitting(transmit);
        if (Ready && !reconciling && !string.IsNullOrEmpty(loggedAccount) && Time.unscaledTime >= nextParticipantSettings)
        {
            nextParticipantSettings = Time.unscaledTime + .2f;
            foreach (var member in Participants)
                if (!member.IsSelf) ApplyParticipantSettings(member);
        }
        if (reconciling && Time.unscaledTime - operationStarted > 20) Status = "음성 연결 지연 · 게임은 계속할 수 있습니다";
    }

    private bool FreshContext() => desired != null && Time.unscaledTime - contextTime < 4f;
    public static bool TransmissionAllowed(bool channelReady, bool muted, bool hasFocus, bool inputBlocked,
        bool hasMicrophone, bool automatic, bool held, bool needsRelease) =>
        channelReady && !muted && hasFocus && !inputBlocked && hasMicrophone && (automatic || (held && !needsRelease));
    private static bool InputBlocked()
    {
        if (VectorVoicePanel.IsAnyOpen || InGameChatUI.IsChatOpen ||
            (LobbySocialController.Instance != null && LobbySocialController.Instance.HasModal) ||
            (SettingsMenu.Instance != null && SettingsMenu.Instance.IsOpen)) return true;
        var selected = EventSystem.current?.currentSelectedGameObject;
        if (selected != null && (selected.GetComponentInParent<TMP_InputField>() != null || selected.GetComponentInParent<InputField>() != null)) return true;
        return VectorVoicePanel.HasBlockingPopup;
    }

    private async Task PollAsync(NetworkManager network, uint player)
    {
        int generation = contextGeneration;
        polling = true; nextPoll = Time.unscaledTime + 1f;
        try
        {
            var context = await network.RequestVoiceAsync(0);
            if (disposed || generation != contextGeneration || network != NetworkManager.Instance || !network.IsConnected || !network.IsAuthenticated || network.MyPlayerId != player) return;
            if (context.Status != 0)
            {
                desired = null;
                Status = context.Status == 1 ? "음성 서비스 준비 중" : "음성 연결을 다시 확인하고 있습니다";
                nextPoll = Time.unscaledTime + 5f;
                return;
            }
            desired = context; contextTime = Time.unscaledTime;
        }
        catch { desired = null; Status = "음성 연결 끊김 · 다시 연결 중"; nextPoll = Time.unscaledTime + 5f; }
        finally { polling = false; }
    }

    private async Task ReconcileAsync()
    {
        reconciling = true; operationStarted = Time.unscaledTime;
        try
        {
            var context = desired;
            if (context == null)
            {
                if (initialized && Voice.IsLoggedIn) await StopSessionAsync();
                return;
            }
            if (!initialized)
            {
                Status = "음성 연결 중";
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync(new InitializationOptions().SetVivoxCredentials(context.Server, context.Domain, context.Issuer));
                await Voice.InitializeAsync();
                initialized = true;
                Voice.SetTokenProvider(this);
                Voice.MuteInputDevice();
                Voice.AvailableInputDevicesChanged += DevicesChanged;
                Voice.LoggedOut += LoggedOut;
            }
            if (!ContextMatches(context)) return;
            if (Voice.IsLoggedIn && loggedAccount != context.Account) await StopSessionAsync();
            if (!Voice.IsLoggedIn)
            {
                loginContext = context;
                await Voice.LoginAsync(new LoginOptions { PlayerId = context.Account, DisplayName = context.Nickname, EnableTTS = false });
                loggedAccount = context.Account;
                Voice.MuteInputDevice();
                await Voice.EnableAutoVoiceActivityDetectionAsync();
                Voice.SetInputDeviceVolume(InputVolume); Voice.SetOutputDeviceVolume(OutputVolume);
                await RestoreDevicesAsync();
            }
            if (!ContextMatches(context)) { await StopSessionAsync(); return; }
            if (joinedChannel != context.Channel || (joinedChannel.Length > 0 && !Voice.ActiveChannels.ContainsKey(joinedChannel)))
            {
                SetTransmitting(false);
                await Voice.LeaveAllChannelsAsync(); joinedChannel = "";
                if (!ContextMatches(context)) return;
                if (!string.IsNullOrEmpty(context.Channel))
                {
                    Status = "팀 음성 연결 중";
                    await Voice.JoinGroupChannelAsync(context.Channel, ChatCapability.AudioOnly, new ChannelOptions { MakeActiveChannelUponJoining = false });
                    if (!ContextMatches(context)) { await Voice.LeaveAllChannelsAsync(); return; }
                    joinedChannel = context.Channel;
                    await Voice.SetChannelTransmissionModeAsync(TransmissionMode.Single, joinedChannel);
                    Voice.MuteInputDevice(); requireKeyRelease = true;
                }
            }
            Status = joinedChannel.Length == 0 ? "같은 파티·팀에 들어가면 음성 연결" : ChannelLabel;
        }
        catch
        {
            // Never log bearer tokens, URIs or SDK exception payloads.
            SetTransmitting(false); Status = "음성 연결 실패 · 잠시 후 재시도";
            nextReconcile = Time.unscaledTime + 5f;
            if (initialized) { try { await StopSessionAsync(); } catch { } }
        }
        finally { reconciling = false; }
    }

    private bool ContextMatches(VoiceServerContext context) => !disposed && FreshContext() &&
        desired.Account == context.Account && desired.Channel == context.Channel &&
        NetworkManager.Instance != null && NetworkManager.Instance.IsAuthenticated && NetworkManager.Instance.IsConnected;

    public async Task<string> GetTokenAsync(string issuer = null, TimeSpan? expiration = null,
        string targetUserUri = null, string action = null, string channelUri = null, string fromUserUri = null, string realm = null)
    {
        var context = desired ?? loginContext;
        if (disposed || context == null || !FreshContext() || (action != "login" && action != "join"))
            throw new InvalidOperationException("Voice authorization expired");
        string user = "sip:." + context.Issuer + "." + context.Account + ".@" + context.Domain;
        string channel = "sip:confctl-g-" + context.Issuer + "." + context.Channel + "@" + context.Domain;
        if ((!string.IsNullOrEmpty(fromUserUri) && fromUserUri != user) ||
            (!string.IsNullOrEmpty(issuer) && issuer != context.Issuer) ||
            (action == "join" && (context.Channel.Length == 0 || channelUri != channel)))
            throw new InvalidOperationException("Voice authorization denied");
        var reply = await NetworkManager.Instance.RequestVoiceAsync((byte)(action == "login" ? 1 : 2), action == "join" ? context.Channel : "");
        if (!ContextMatches(context) || reply.Status != 0 || reply.Account != context.Account || string.IsNullOrEmpty(reply.Token))
            throw new InvalidOperationException("Voice authorization denied");
        return reply.Token;
    }

    private async Task StopSessionAsync()
    {
        SetTransmitting(false); joinedChannel = ""; loggedAccount = ""; loginContext = null;
        if (Voice.IsLoggedIn) { await Voice.LeaveAllChannelsAsync(); await Voice.LogoutAsync(); }
    }
    private void SetTransmitting(bool value)
    {
        if (!initialized) { Transmitting = false; return; }
        if (value == Transmitting && Voice.IsInputDeviceMuted == !value) return;
        try { if (value) Voice.UnmuteInputDevice(); else Voice.MuteInputDevice(); Transmitting = value; }
        catch { Transmitting = false; }
    }
    private void LoggedOut() { SetTransmitting(false); joinedChannel = ""; loggedAccount = ""; }
    private void DevicesChanged() { SetTransmitting(false); requireKeyRelease = true; }
    public void RefreshMembership()
    {
        ++contextGeneration; SetTransmitting(false); requireKeyRelease = true;
        desired = null; contextTime = -100; nextPoll = 0;
        if (initialized) Voice.MuteOutputDevice();
    }
    private void SceneChanged(Scene before, Scene after) => RefreshMembership();
    private void OnApplicationFocus(bool hasFocus) { focused = hasFocus; if (!hasFocus) { SetTransmitting(false); requireKeyRelease = true; } }
    private void OnApplicationPause(bool paused) { if (paused) SetTransmitting(false); }
    private void OnApplicationQuit() { PlayerPrefs.Save(); disposed = true; SetTransmitting(false); }
    private async void OnDestroy()
    {
        if (Instance != this) return;
        disposed = true; Instance = null; SetTransmitting(false);
        SceneManager.activeSceneChanged -= SceneChanged;
        if (!initialized) return;
        Voice.AvailableInputDevicesChanged -= DevicesChanged; Voice.LoggedOut -= LoggedOut;
        try { await StopSessionAsync(); } catch { }
    }

    public void ToggleMode()
    {
        SetTransmitting(false); requireKeyRelease = true;
        Automatic = !Automatic; PlayerPrefs.SetInt("VectorVoice.Automatic", Automatic ? 1 : 0); PlayerPrefs.Save();
    }
    public void ToggleMicrophone()
    {
        MicrophoneMuted = !MicrophoneMuted; SetTransmitting(false);
        PlayerPrefs.SetInt("VectorVoice.Muted", MicrophoneMuted ? 1 : 0); PlayerPrefs.Save();
    }
    public void SetInputVolume(float value) { InputVolume = Mathf.RoundToInt(value); PlayerPrefs.SetInt("VectorVoice.Input", InputVolume); if (Ready) Voice.SetInputDeviceVolume(InputVolume); }
    public void SetOutputVolume(float value) { OutputVolume = Mathf.RoundToInt(value); PlayerPrefs.SetInt("VectorVoice.Output", OutputVolume); if (Ready) Voice.SetOutputDeviceVolume(OutputVolume); }
    public void ToggleParticipant(string account)
    {
        if (!CanAdjustParticipant(account)) return;
        PlayerPrefs.SetInt(PeerKey(account, "Mute"), ParticipantMuted(account) ? 0 : 1);
        RefreshParticipant(account); PlayerPrefs.Save();
    }
    private string PeerKey(string account, string setting) => ParticipantPreferenceKey(loggedAccount, account, setting);
    public static string ParticipantPreferenceKey(string owner, string account, string setting) =>
        "VectorVoice.Peer." + owner + "." + account + "." + setting;
    public static int ParticipantSdkVolume(int value) => Mathf.RoundToInt((Mathf.Clamp(value, 0, 200) - 100) * .5f);
    public int ParticipantVolume(string account) => Mathf.Clamp(PlayerPrefs.GetInt(PeerKey(account, "Volume"), 100), 0, 200);
    public bool ParticipantMuted(string account) => PlayerPrefs.GetInt(PeerKey(account, "Mute"), 0) != 0;
    private bool CanAdjustParticipant(string account) => Ready && !string.IsNullOrEmpty(loggedAccount) &&
        Participants.Any(p => !p.IsSelf && p.PlayerId == account);
    public void SetParticipantVolume(string account, float value)
    {
        if (!CanAdjustParticipant(account)) return;
        PlayerPrefs.SetInt(PeerKey(account, "Volume"), Mathf.Clamp(Mathf.RoundToInt(value), 0, 200));
        RefreshParticipant(account);
    }
    public void ResetParticipant(string account)
    {
        if (!CanAdjustParticipant(account)) return;
        PlayerPrefs.DeleteKey(PeerKey(account, "Volume")); PlayerPrefs.DeleteKey(PeerKey(account, "Mute"));
        RefreshParticipant(account); PlayerPrefs.Save();
    }
    private void RefreshParticipant(string account)
    {
        foreach (var member in Participants.Where(p => !p.IsSelf && p.PlayerId == account)) ApplyParticipantSettings(member);
    }
    private void ApplyParticipantSettings(VivoxParticipant member)
    {
        int value = ParticipantVolume(member.PlayerId);
        bool muted = ParticipantMuted(member.PlayerId) || value == 0;
        if (member.LocalVolume != ParticipantSdkVolume(value)) member.SetLocalVolume(ParticipantSdkVolume(value));
        if (muted && !member.IsMuted) member.MutePlayerLocally();
        else if (!muted && member.IsMuted) member.UnmutePlayerLocally();
    }
    public void ObserveProfile(string nickname, int icon)
    {
        if (!string.IsNullOrEmpty(nickname) && icon >= 0 && icon <= 2) profileIcons[nickname] = icon;
    }
    public Sprite ParticipantIcon(string nickname) => profileIcons.TryGetValue(nickname ?? "", out int icon)
        ? Resources.Load<Sprite>("PlayerIcon/" + icon) : null;
    public async void CycleDevice(bool input)
    {
        if (!Ready || changingDevice || reconciling) return;
        changingDevice = true; SetTransmitting(false);
        try
        {
            if (input && Voice.AvailableInputDevices.Count > 0)
            {
                var devices = Voice.AvailableInputDevices;
                int index = devices.ToList().FindIndex(d => d.DeviceID == Voice.ActiveInputDevice?.DeviceID);
                var device = devices[(index + 1) % devices.Count];
                await Voice.SetActiveInputDeviceAsync(device); PlayerPrefs.SetString("VectorVoice.InputDevice", device.DeviceID);
            }
            else if (!input && Voice.AvailableOutputDevices.Count > 0)
            {
                var devices = Voice.AvailableOutputDevices;
                int index = devices.ToList().FindIndex(d => d.DeviceID == Voice.ActiveOutputDevice?.DeviceID);
                var device = devices[(index + 1) % devices.Count];
                await Voice.SetActiveOutputDeviceAsync(device); PlayerPrefs.SetString("VectorVoice.OutputDevice", device.DeviceID);
            }
            PlayerPrefs.Save();
        }
        catch { Status = "장치를 연결한 뒤 다시 선택해 주세요"; }
        finally { changingDevice = false; requireKeyRelease = true; }
    }
    private async Task RestoreDevicesAsync()
    {
        var input = Voice.AvailableInputDevices.FirstOrDefault(d => d.DeviceID == PlayerPrefs.GetString("VectorVoice.InputDevice", ""));
        var output = Voice.AvailableOutputDevices.FirstOrDefault(d => d.DeviceID == PlayerPrefs.GetString("VectorVoice.OutputDevice", ""));
        if (input != null) await Voice.SetActiveInputDeviceAsync(input);
        if (output != null) await Voice.SetActiveOutputDeviceAsync(output);
    }
}
