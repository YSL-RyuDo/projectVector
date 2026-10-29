using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class NetworkManager
{
    public bool ReconnectBusy { get; private set; }
    public int CompletedReconnects { get; private set; }
    string reconnectAccount, reconnectPassword;
    GameAllReadyData reconnectRoster;
    bool reconnectLoading, reconnectFailed, reconnectBootstrap;
    float reconnectDeadline;
    byte reconnectPhase;
    Coroutine reconnectRoutine;
    string reconnectMessage = "연결을 복구하고 있습니다";

    void UpdateReconnect()
    {
        string scene=SceneManager.GetActiveScene().name;
        if (!ReconnectBusy && IsAuthenticated && !IsConnected && reconnectRoster != null &&
            (scene == "Game_TestVersion" || scene == "Result" || scene == "RankResult")) RetryReconnect();
        if (!ReconnectBusy) return;
        if (MyPlayer != null) {
            var movement=MyPlayer.GetComponent<PlayerMovement>();
            if(movement!=null) movement.CanMove=false;
        }
        if (!reconnectFailed && Time.realtimeSinceStartup > reconnectDeadline) FailReconnect();
        VectorReconnectView.Show(reconnectRoster, reconnectMessage, reconnectFailed);
        if (GameController.Instance != null && GameController.Instance.CurrentState != GameController.GameFlowState.Loading)
            GameController.Instance.EnterLoadingPhase();
    }

    public void RetryReconnect()
    {
        if (reconnectRoutine != null) StopCoroutine(reconnectRoutine);
        ReconnectBusy = true; reconnectFailed = false; reconnectLoading = false; reconnectBootstrap = false;
        reconnectDeadline = Time.realtimeSinceStartup + 45;
        reconnectMessage = "서버 연결을 복구하고 있습니다";
        reconnectRoutine = StartCoroutine(ReconnectTransport());
    }

    NetworkClient NewReconnectTransport()
    {
        var connection = new NetworkClient(); client = connection;
        connection.OnPacket += (type, body) => {
            lock (packetQueue) { if (client == connection) packetQueue.Enqueue((type, body)); }
        };
        return connection;
    }

    IEnumerator ReconnectTransport()
    {
        string host = client.LastHost; int port = client.LastPort;
        client.Close(); IsAuthenticated = false;
        lock (packetQueue) packetQueue.Clear();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Environment.GetEnvironmentVariable("VECTOR_CAPTURE_RECONNECT") == "1" &&
            Environment.GetEnvironmentVariable("VECTOR_CAPTURE_ACCOUNT") == "qwer")
            yield return new WaitForSecondsRealtime(6); // Opt-in QA outage; not a production loading delay.
#endif
        while (ReconnectBusy && !reconnectFailed && !reconnectLoading)
        {
            var connection = NewReconnectTransport();
            var task = Task.Run(() => connection.Connect(host, port));
            while (!task.IsCompleted && !reconnectFailed) yield return null;
            if (reconnectFailed) { connection.Close(); yield break; }
            if (task.IsFaulted) Debug.LogWarning("[RECONNECT] Transport setup failed: " + task.Exception.GetBaseException().GetType().Name);
            if (connection.IsConnected && !string.IsNullOrEmpty(reconnectPassword))
            {
                SendLogin(reconnectAccount, reconnectPassword);
                float waitUntil = Time.realtimeSinceStartup + 5;
                while (!reconnectLoading && Time.realtimeSinceStartup < waitUntil && !reconnectFailed) yield return null;
                if (reconnectLoading) yield break;
            }
            connection.Close();
            yield return new WaitForSecondsRealtime(2);
        }
    }

    void FailReconnect()
    {
        reconnectFailed = true;
        reconnectMessage = "연결을 복구하지 못했습니다. 다시 시도해 주세요.";
        client.Close();
        Debug.LogWarning("[RECONNECT] Timed out; gameplay remains blocked.");
    }

    public void ReturnToLoginAfterDisconnect()
    {
        if (reconnectRoutine != null) StopCoroutine(reconnectRoutine);
        string host=client.LastHost; int port=client.LastPort;
        ResetLocalCombatReport();
        client.Close(); ReconnectBusy = false; IsAuthenticated = false;
        reconnectRoster = null; reconnectPassword = null; reconnectAccount = null;
        lock (packetQueue) packetQueue.Clear();
        SceneManager.LoadScene("Login");
        var connection=NewReconnectTransport();
        Task.Run(() => connection.Connect(host,port));
    }

    void SendReconnectStage(byte stage) => client.Send(new byte[] {5,0,135,1,stage}); // 391

    bool HandleReconnectPacket(ushort type, byte[] body)
    {
        if (type == 390 && body.Length == 40)
        {
            if (reconnectLoading) return true;
            myPlayerId=BitConverter.ToUInt32(body,0);
            ReconnectBusy=true; reconnectFailed=false; reconnectLoading=true; reconnectBootstrap=false;
            reconnectDeadline=Time.realtimeSinceStartup+45;
            reconnectMessage="경기 맵과 캐릭터를 불러오고 있습니다";
            StartCoroutine(LoadReconnectScene()); return true;
        }
        if (!ReconnectBusy) return false;
        if (type == 392 && body.Length == 2)
        {
            reconnectPhase=body[1];
            reconnectMessage=body[0]==2?"현재 라운드 · 장비 · 전투 상태 복구 중":"경기 맵과 캐릭터를 불러오고 있습니다";
            return true;
        }
        if (type == 49)
        {
            if (!reconnectBootstrap) { reconnectBootstrap=true; StartCoroutine(RequestReconnectSnapshot()); }
            return false;
        }
        if (type == 393) { ApplyReconnectPoses(body); return true; }
        if (type == 394 && body.Length == 1)
        {
            reconnectPhase=body[0]; reconnectMessage="복구 확인 완료 · 경기에 접속 중";
            client.Send(new byte[] {4,0,139,1}); return true; // 395
        }
        if (type == 396)
        {
            ReconnectBusy=false; reconnectLoading=false; reconnectFailed=false; CompletedReconnects++;
            VectorReconnectView.Hide();
            if(MyPlayer!=null) {
                var movement=MyPlayer.GetComponent<PlayerMovement>();
                var hp=MyPlayer.GetComponent<PlayerHP>();
                if(movement!=null) movement.CanMove=hp!=null && !hp.IsDead;
            }
            if (reconnectPhase==0) GameController.Instance?.EnterPickPhase();
            else if (SceneManager.GetActiveScene().name == "Game_TestVersion") GameController.Instance?.EnterPlayingPhase();
            else GameController.Instance?.EnterOutsideGamePhase();
            if (MyPlayer && MyPlayer.GetComponent<PlayerHP>()?.IsDead == true) BeginLocalCombatReport();
            Debug.Log("[RECONNECT] Complete; same identity and authoritative state restored.");
            return true;
        }
        return false;
    }

    IEnumerator LoadReconnectScene()
    {
        lock(packetQueue) packetQueue.Clear();
        var op=SceneManager.LoadSceneAsync("Game_TestVersion");
        while(!op.isDone) yield return null;
        yield return null;
        GameController.Instance?.EnterLoadingPhase();
        VectorReconnectView.Show(reconnectRoster,reconnectMessage,false);
        SendReconnectStage(1);
    }

    IEnumerator RequestReconnectSnapshot()
    {
        yield return null; yield return null;
        if (MyPlayer==null || reconnectRoster==null || FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None).Length<reconnectRoster.members.Count)
        { FailReconnect(); yield break; }
        VectorReconnectView.Show(reconnectRoster,reconnectMessage,false);
        if(ScreenFader.Instance!=null && ScreenFader.Instance.fadeImage!=null) ScreenFader.Instance.fadeImage.gameObject.SetActive(false);
        SendReconnectStage(2);
    }

    void ApplyReconnectPoses(byte[] body)
    {
        if(body.Length<1 || body[0]>6 || body.Length<1+body[0]*24) {FailReconnect();return;}
        int offset=1;
        for(int i=0;i<body[0];i++,offset+=24)
        {
            uint id=BitConverter.ToUInt32(body,offset);
            var actor=FindPlayerIdentityById(id); if(actor==null) {FailReconnect();return;}
            var position=new Vector3(BitConverter.ToSingle(body,offset+4),BitConverter.ToSingle(body,offset+8),BitConverter.ToSingle(body,offset+12));
            float yaw=BitConverter.ToSingle(body,offset+16), pitch=BitConverter.ToSingle(body,offset+20);
            var controller=actor.GetComponent<CharacterController>(); if(controller!=null) controller.enabled=false;
            actor.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            var hp=actor.GetComponent<PlayerHP>(); bool dead=hp!=null && hp.IsDead;
            if(controller!=null) controller.enabled=!dead;
            if(id==MyPlayerId) actor.GetComponentInChildren<FPSCameraLook>(true)?.ResetLookRotation(yaw);
            else {
                // Restored poses are not samples from the peer's running clock.
                // Do not mix local arrival time into its timestamped stream.
                actor.GetComponent<PlayerMovement>()?.ResetNetworkInterpolation();
                var look=actor.GetComponentInChildren<FPSCameraLook>(true);
                if(look!=null) look.transform.localRotation=Quaternion.Euler(pitch,0,0);
            }
        }
        // Dead actors also need their presentation restored if they have no
        // position update in this round (the map spawn is retained for those).
        foreach(var actor in FindObjectsByType<PlayerIdentity>(FindObjectsSortMode.None)) {
            var hp=actor.GetComponent<PlayerHP>();
            if(hp!=null && hp.IsDead) {
                actor.GetComponent<PlayerDeathController>()?.OnDead();
                if(actor.PlayerId==MyPlayerId) SpectatorManager.Instance?.StartSpectating();
            }
        }
    }
}
