using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    public NavigationPanelController Navigation { get; private set; }
    public LobbyPanelController Lobby { get; private set; }
    public CharacterPanelController Character { get; private set; }
    public GamePanelController Game { get; private set; }
    public ProfilePanelController Profile { get; private set; }
    public ChatPanelController Chat { get; private set; }

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Navigation = GetComponent<NavigationPanelController>();
        Lobby = GetComponent<LobbyPanelController>();
        Character = GetComponent<CharacterPanelController>();
        Game = GetComponent<GamePanelController>();
        Profile = GetComponent<ProfilePanelController>();
        Chat = GetComponent<ChatPanelController>();
    }

    void Start()
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("[LOBBY] NetworkManager.Instance is null");
            return;
        }
        NetworkManager.Instance.SetLobbyManager(this);
        NetworkManager.Instance.SendLobbyEnter();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
    }
}