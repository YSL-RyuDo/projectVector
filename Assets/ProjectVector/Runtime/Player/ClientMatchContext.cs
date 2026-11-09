using UnityEngine;
using UnityEngine.SceneManagement;

// 입력 처리와 미리 작성된 상점 UI가 같은 클라이언트 경기 상태를 공유한다.
public static class ClientMatchContext
{
    public static ClientMatchState State { get; } = new ClientMatchState();

    public static bool CanOpenStore(bool legacyBuyPhase)
    {
        var net = NetworkManager.Instance;
        if (State.IsDeathmatch && net != null && net.ReconnectBusy) return false;
        return State.IsDeathmatch ? State.CanSelectDeathmatchWeapon : legacyBuyPhase;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        // 도메인 리로드를 끈 상태로 Play에 진입해도 이전 경기 상태를 초기화한다.
        State.Reset();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 데스매치 테스트 씬과 기존 경기 씬에서는 상태를 유지한다.
        // 맵 추가 로드는 유지하고, 그 외 단일 씬으로 이동할 때는 초기화한다.
        if (mode == LoadSceneMode.Single &&
            scene.name != "SystemSample" && scene.name != "Game_TestVersion") State.Reset();
    }
}
