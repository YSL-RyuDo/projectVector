using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Server-acknowledged search state. No progress estimate, queue mutation or new UI.
public sealed class MatchSearchPresentation : MonoBehaviour
{
    public TMP_Text stateText;
    public TMP_Text detailText;
    public Image[] signals;
    public void Present(bool searching, float time)
    {
        var network = NetworkManager.Instance;
        bool waiting = !searching && network != null && !network.PartyReadyToQueue;
        if (stateText != null) stateText.text = searching ? "상대 팀 탐색 중" : "작전 준비";
        if (detailText != null) detailText.text = searching
            ? "매칭이 완료되면 준비 확인창이 열립니다.\n기다리는 동안 프로필을 확인할 수 있습니다."
            : "친구를 초대하고 함께 출전하세요.\n모두 준비되면 리더가 매칭을 시작합니다.";
        if (waiting)
        {
            stateText.text = "파티원 복귀 대기";
            detailText.text = $"로비 복귀 {network.PartyLobbyCount} / {network.PartyPlayers.Count}\n전원이 돌아오면 다시 출전할 수 있습니다.";
        }
        for (int i = 0; signals != null && i < signals.Length; ++i)
        {
            if (signals[i] == null) continue;
            var color = signals[i].color;
            color.a = searching ? .25f + .75f * (.5f + .5f * Mathf.Sin(time * 3f - i * .8f)) : .35f;
            signals[i].color = color;
        }
    }
    private void Update() => Present(GamePanelController.IsMatchmakingActive, Time.unscaledTime);
}
