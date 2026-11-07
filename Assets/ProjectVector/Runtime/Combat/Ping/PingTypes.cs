using System;
using UnityEngine;

namespace Game.Pings
{
    public enum PingType : byte { Location, Danger, Attack, Defend }

    [Serializable]
    public sealed class PingOption
    {
        public PingType type;
        public string label;
        public Color color = Color.white;

        public PingOption(PingType type, string label, Color color)
        { this.type = type; this.label = label; this.color = color; }
    }

    // 클라이언트가 보내는 요청. 팀/발신자는 서버가 연결 정보로 결정합니다.
    public readonly struct PingRequest
    {
        public readonly string RequestId;
        public readonly PingType Type;
        public readonly Vector3 Position;
        public PingRequest(string requestId, PingType type, Vector3 position)
        { RequestId = requestId; Type = type; Position = position; }
    }

    // 서버가 검증한 뒤 같은 팀에만 전달할 메시지. RPC 직렬화는 네트워크 SDK에 맞추세요.
    public readonly struct ConfirmedPing
    {
        public readonly string PingId;
        public readonly uint SenderId;
        public readonly int TeamId;
        public readonly PingType Type;
        public readonly Vector3 Position;
        public readonly float Lifetime;
        public ConfirmedPing(string pingId, uint senderId, int teamId,
            PingType type, Vector3 position, float lifetime = 3f)
        { PingId = pingId; SenderId = senderId; TeamId = teamId; Type = type; Position = position; Lifetime = lifetime; }
    }

    internal static class PingValidation
    {
        public static bool IsFinite(Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
            !float.IsNaN(v.y) && !float.IsInfinity(v.y) &&
            !float.IsNaN(v.z) && !float.IsInfinity(v.z);
    }
}
