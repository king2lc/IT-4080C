using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;
using UnityEngine.UI;
using MLAPI.NetworkVariable.Collections;
using System;
using MLAPI.Connection;
using MLAPI.SceneManagement;
using MLAPI.Messaging;

public class MP_Lobby : NetworkBehaviour
{
    [SerializeField] private LobbyPlayerPanel[] lobbyPlayers;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Button startGameButton;

    private NetworkList<MP_PlayerInfo> nwPlayers = new NetworkList<MP_PlayerInfo>();

    [SerializeField] private GameObject chatPrefab;

    void Start() 
    {
        UpdateConnListServerRpc(NetworkManager.LocalClientId);
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    public override void NetworkStart()
    {
        Debug.Log("Starting Server");
        if(IsClient)
        {   
            nwPlayers.OnListChanged += PlayersInfoChanged;
        }
        if(IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += ClientConnectedHandle;
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnectedHandle;

            foreach(NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                ClientConnectedHandle(client.ClientId);
            }
        }

        //NetworkSceneManager.SwitchScene("S_Lobby");
    }

    private void OnDestroy() 
    {
        {
            nwPlayers.OnListChanged -= PlayersInfoChanged;
            if(NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnectedHandle;
                NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnectedHandle;
            }
        }
    }

    private void PlayersInfoChanged(NetworkListEvent<MP_PlayerInfo> changeEvent)
    {
        int index = 0;
        foreach (MP_PlayerInfo connectedplayer in nwPlayers)
        {
            lobbyPlayers[index].playerName.text = connectedplayer.networkPlayerName;
            lobbyPlayers[index].readyIcon.SetIsOnWithoutNotify(connectedplayer.networkPlayerReady);
            index++;
        }
        for(;index < 4; index++)
        {
            lobbyPlayers[index].playerName.text = "PlayerName";
            lobbyPlayers[index].readyIcon.SetIsOnWithoutNotify(false);
            index++;
        }

        if(IsHost)
        {
            startGameButton.gameObject.SetActive(true);
            startGameButton.interactable = CheckEveryoneReady();
        }

    }

    public void StartGame()
    {
        if (IsServer)
        {
            NetworkSceneManager.OnSceneSwitched += SceneSwitched;
            NetworkSceneManager.SwitchScene("asdf");
        }
        else
        {   
            Debug.Log("You are not host.");
        }
    }
    private void HandleClientConnected(ulong clientID)
    {
        UpdateConnListServerRpc(clientID);
        Debug.Log("A Player has connected. ID is :" + clientID);
    }

    [ServerRpc]
    private void UpdateConnListServerRpc(ulong clientID)
    {
        nwPlayers.Add(new MP_PlayerInfo(clientID, PlayerPrefs.GetString("PName"), false));
    }

    private void ClientDisconnectedHandle(ulong clientID)
    {
        Debug.Log("TODO: Player DC");
    }

    private void ClientConnectedHandle(ulong clientID)
    {
        Debug.Log("ToDO: Player Connected.");
    }

    public void ReadyButtonPressed()
    {
        ReadyUpServerRpc();
    }
    private void SceneSwitched()
    {
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");

        foreach (MP_PlayerInfo tmpClient in nwPlayers)
        {
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
            int index = UnityEngine.Random.Range(0, spawnPoints.Length);
            GameObject currentPoint = spawnPoints[index];

            //spawnplayer
            GameObject playerSpawn = Instantiate(playerPrefab, currentPoint.transform.position, Quaternion.identity);
            playerSpawn.GetComponent<NetworkObject>().SpawnWithOwnership(tmpClient.networkClientID);
            //chat ui
            GameObject chatUISpawn = Instantiate(chatPrefab);
            chatUISpawn.GetComponent<NetworkObject>().SpawnWithOwnership(tmpClient.networkClientID);
            chatUISpawn.GetComponent<MP_ChatUIScript>().chatPlayers = nwPlayers;
        }
    }
    [ServerRpc(RequireOwnership = false)]
    private void ReadyUpServerRpc(ServerRpcParams serverRpcParams = default)
    {

        for (int i = 0; i < nwPlayers.Count; i++)
        {
            if(nwPlayers[i].networkClientID == serverRpcParams.Receive.SenderClientId)
            {
                Debug.Log("Updated with new.");
                nwPlayers[i] = new MP_PlayerInfo(nwPlayers[i].networkClientID, nwPlayers[i].networkPlayerName, !nwPlayers[i].networkPlayerReady);
            }
        }
    }
    private bool CheckEveryoneReady()
    {
        foreach (MP_PlayerInfo player in nwPlayers)
        {
            if(!player.networkPlayerReady)
            {
                return false;
            }
        }
        return true;
    }
}
