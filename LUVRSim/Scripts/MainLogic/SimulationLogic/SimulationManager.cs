using System.Collections;
using System.Collections.Generic;
using MinigameSystem;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using XRMultiplayer;
using XRMultiplayer.MiniGames;

// This class should take care of the ui outside the room and should manage the ui inside via the MainMenu and the SelectionMenu
public class SimulationManager : NetworkBehaviour
{
    [Header("Chosen simulation")]
    public BaseSimulation chosenSimulation;
    
    [Header("Game")]
    public int maxAllowedUsersInRoom = 1; // The number of people that can do a simulation is 1, this can be a future upgrade in the "possible developments" section of the thesis
    [SerializeField] int m_ReadyUpTimeInSeconds = 15;
    [SerializeField] int m_StartCoutdownTimeInSeconds = 5;
    [SerializeField] int m_PostGameWaitTimeInSeconds = 3;
    [SerializeField] int m_PostGameCountdownTimeInSeconds = 7;
    
    [Header("Transform References")]
    [SerializeField] Transform m_JoinTeleportTransform;
    [SerializeField] Transform m_LeaveTeleportTransform;
    
    private TeleportationProvider m_LocalPlayerTeleportProvider;

    [Header("UI")]
    [SerializeField] TextButton m_EnterButton;
    // move here the start button, this makes sense because every simulation is going to have a start button at the very least
    // finish button here
    [SerializeField] TextButton m_LeaveButton;
    [SerializeField] TextButton m_StartButton;
    // some message telling that the current user is doing the simulation
    
    NetworkVariable<ulong> m_CurrentUserID; // id of the user that is currently doing the simulation
    private NetworkList<ulong> m_PlayerIDs; // contains player ids to be set back to private
    public List<XRINetworkPlayer> currentPlayersInRoom; // contains players

    private enum GameState { PreSim, InSim, PostSim }
    
    readonly NetworkVariable<GameState> networkedGameState = new();
    
    IEnumerator m_PostGameRoutine;
    
    // This determines whether the player is in the game
    private bool LocalPlayerInGame => m_LocalPlayerInGame;
    bool m_LocalPlayerInGame = false;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        if (chosenSimulation == null)
        {
            TryGetComponent(out chosenSimulation);
        }
        
        m_LocalPlayerTeleportProvider = FindFirstObjectByType<TeleportationProvider>();
        
        // m_GameNameText.text = chosenSimulation.gameName; this should be displayed on the UI outside
        
        m_PlayerIDs = new NetworkList<ulong>();
        m_CurrentUserID = new NetworkVariable<ulong>();
    }

    // Update for now just increases the timer, it should be used to gather statistics
    void Update()
    {
        if (networkedGameState.Value == GameState.InSim)
        {
            float dt = Time.deltaTime;
            chosenSimulation.UpdateGame(dt);
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkedGameState.OnValueChanged += GameStateValueChanged; 
        m_PlayerIDs.OnListChanged += UpdatePlayerList; // when a new user JOINS (client id)

        if (IsServer)
        {
            networkedGameState.Value = GameState.PreSim;
        }
        UpdateGameState();
    }
    
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        m_LocalPlayerInGame = false;
        currentPlayersInRoom.Clear();
    }
    
    private void UpdatePlayerList(NetworkListEvent<ulong> changeEvent)
    {
        if (networkedGameState.Value != GameState.InSim) return;

        currentPlayersInRoom.Clear();

        foreach (var playerId in m_PlayerIDs)
        {
            AddPlayerToRoom(playerId);
        }
        
    }
    
    void GameStateValueChanged(GameState oldState, GameState currentState)
    {
        UpdateGameState();
    }
    
    void UpdateGameState()
    {
        switch (networkedGameState.Value)
        {
            case GameState.PreSim:
                SetPreGameState();
                break;
            case GameState.InSim:
                SetInGameState();
                break;
            case GameState.PostSim:
                SetPostGameState();
                break;
        }
    }
    
    void SetPreGameState()
    {
        m_LocalPlayerInGame = false;
        if (m_PostGameRoutine != null)
        {
            StopCoroutine(m_PostGameRoutine);
        }
        chosenSimulation.SetupGame();
    }

    void SetInGameState()
    {

        // m_GameStateText.text = "In Progess"; this could be displayed outside when a player is doing an exercise
        chosenSimulation.StartGame(m_CurrentUserID.Value);
    }

    void SetPostGameState()
    {
        m_LocalPlayerInGame = false;
        
        chosenSimulation.FinishGame();
        //chosenSimulation.SetUsability(m_CurrentUserID);

        m_PostGameRoutine = PostGameRoutine();
        StartCoroutine(m_PostGameRoutine);
        if (currentPlayersInRoom.Count <= 0)
        {
            if (IsServer)
            {
                networkedGameState.Value = GameState.PreSim;
            }
        }
    }
    
    IEnumerator PostGameRoutine()
    {
        // display the notification that the simulation is finished
        yield return null;
        if (IsServer)
        {
            networkedGameState.Value = GameState.PreSim;
        }
    }
        
    public void AddLocalPlayer() // called when using the Join button outside the room, adds player to the room
    {
        AddPlayerServerRpc(XRINetworkPlayer.LocalPlayer.OwnerClientId);
    }
        
    [ServerRpc(RequireOwnership = false)]
    void AddPlayerServerRpc(ulong clientId)
    {
        AddPlayerClientRpc(clientId);
        if (m_PlayerIDs.Count < maxAllowedUsersInRoom)
        {
            m_PlayerIDs.Add(clientId);
        }
    }
        
    [ClientRpc]
    void AddPlayerClientRpc(ulong clientId)
    {
        if (currentPlayersInRoom.Count < maxAllowedUsersInRoom)
        {

            AddPlayerToRoom(clientId);

            if (clientId == XRINetworkPlayer.LocalPlayer.OwnerClientId)
            {
                m_LocalPlayerInGame = true;
                TeleportToArea(m_JoinTeleportTransform);
            }
            
            chosenSimulation.SyncUponClientJoin(); // method used to synchronize any parameters that may not be set at runtime for new clients joining
            
            // should use player ids for now
            if (currentPlayersInRoom.Count >= maxAllowedUsersInRoom & !LocalPlayerInGame)
            {
                m_EnterButton.button.interactable = false;
            }
        }
    }
        
    void AddPlayerToRoom(ulong clientId)
    {
        if (XRINetworkGameManager.Instance.GetPlayerByID(clientId, out XRINetworkPlayer player))
        {
            if (!currentPlayersInRoom.Contains(player))
            {
                currentPlayersInRoom.Add(player);
                player.onDisconnected += PlayerDisconnected;
            }
        }
    }
        
    public void RemoveLocalPlayer() 
    {
        RemovePlayerServerRpc(XRINetworkPlayer.LocalPlayer.OwnerClientId);
    }
        
    [ServerRpc(RequireOwnership = false)]
    void RemovePlayerServerRpc(ulong clientId)
    {
        RemovePlayerClientRpc(clientId);

        if (m_PlayerIDs.Contains(clientId))
        {
            m_PlayerIDs.Remove(clientId);
        }
    }
        
    [ClientRpc]
    void RemovePlayerClientRpc(ulong clientId)
    {
        if (XRINetworkGameManager.Instance.GetPlayerByID(clientId, out XRINetworkPlayer player))
        {
            CheckDroppedPlayer(player);
        }

        if (clientId == XRINetworkPlayer.LocalPlayer.OwnerClientId)
        {
            m_LocalPlayerInGame = false;
            TeleportToArea(m_LeaveTeleportTransform);
        } 
        
        if (m_EnterButton.button.interactable == false)
        {
            m_EnterButton.button.interactable = true;
        }
    }
        
    private void PlayerDisconnected(XRINetworkPlayer droppedPlayer) // if the player is disconnected, remove him from everywhere
    {
        CheckDroppedPlayer(droppedPlayer);
    }

    void CheckDroppedPlayer(XRINetworkPlayer droppedPlayer)
    {
        if (currentPlayersInRoom.Contains(droppedPlayer) && networkedGameState.Value != GameState.PostSim)
        {
            currentPlayersInRoom.Remove(droppedPlayer);
            droppedPlayer.onDisconnected -= PlayerDisconnected;
        }

        if (IsOwner && m_PlayerIDs.Contains(droppedPlayer.OwnerClientId))
        {
            m_PlayerIDs.Remove(droppedPlayer.OwnerClientId);
        }

        switch (networkedGameState.Value)
        {
            case GameState.InSim:
            {
                if (XRINetworkGameManager.Instance.GetPlayerByID(m_CurrentUserID.Value, out XRINetworkPlayer player))
                {
                    if (droppedPlayer == player)
                    {
                        // m_DynamicButton.button.interactable = false;
                        if (IsServer)
                        {
                            StopGameServerRpc();
                        }
                    }
                }

                break;
            }
        }
    }

    public void FinishGame()
    {
        StopGameServerRpc();
    }
        
    [ServerRpc(RequireOwnership = false)]
    public void StopGameServerRpc()
    {
        networkedGameState.Value = GameState.PostSim; 
        m_CurrentUserID.Value = 3000; // placeholder value, possibly unrealistic
    }
    
    public void StartGame() // this is called via the start button
    {
        // Validate the client request
        if (networkedGameState.Value == GameState.PreSim)// && m_PlayerIDs.Contains(XRINetworkPlayer.LocalPlayer.OwnerClientId))
        {
            StartGameServerRpc(XRINetworkPlayer.LocalPlayer.OwnerClientId); // the game starts only for the player that pressed the button
        }
    }
        
    [ServerRpc(RequireOwnership = false)]
    void StartGameServerRpc(ulong clientId)
    {
        m_CurrentUserID.Value = clientId;
        networkedGameState.Value = GameState.InSim; // this triggers the event that starts the game
    }

    private void TeleportToArea(Transform teleportTransform)
    {
        TeleportRequest teleportRequest = new TeleportRequest
        {
            destinationPosition = teleportTransform.position,
            destinationRotation = teleportTransform.rotation,
            matchOrientation = MatchOrientation.TargetUpAndForward
        };
        m_LocalPlayerTeleportProvider.QueueTeleportRequest(teleportRequest);
    }
        
}
