using UnityEngine;

public enum AgentRole
{
    Default,
    Initiator,
    Controller,
    Entry,
    Sentinel
}

public class CharacterType : MonoBehaviour
{
    [SerializeField] private AgentRole role = AgentRole.Initiator;

    public AgentRole Role => role;
}