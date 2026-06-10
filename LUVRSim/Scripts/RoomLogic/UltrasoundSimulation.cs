using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using XRMultiplayer;
using XRMultiplayer.MiniGames;

using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using XRMultiplayer;
using XRMultiplayer.MiniGames;

public class UltrasoundSimulation : BaseSimulation
{
    private const ulong PLACEHOLDER = 10000;
    // Here I monitor how the game is going and I will collect here the CSV information
    private TeleportationProvider m_LocalPlayerTeleportProvider;
    [SerializeField] private Transform joinActivityTarget;
    [SerializeField] private Transform startActivityTarget;
    
    [Header("Exercise settings")]
    [SerializeField] private Transform _probeSpawner;
    [SerializeField] private GameObject _screen;
    [SerializeField] private GameObject _USsection;
    [SerializeField] private ScreenSettings _screenSettings;
    [SerializeField] private TMP_InputField _exerciseNameSaved;
    
    [Header("Menus")]
    public MainMenu mainMenu;
    public SelectionMenu selectionMenu;

    private GameObject _chosenProbe;
    private NetworkVariable<ulong> _chosenProbeId;
    private NetworkVariable<int> _patientId;
    private NetworkVariable<bool> _toggleUltrasound;
    private NetworkVariable<bool> _toggleBorders;
    private NetworkVariable<bool> _toggleMoveScan;
    // private NetworkVariable<bool> _toggleScaleScan;
    private Exercise _selectedExercise;

    protected override void Awake()
    {
        base.Awake();
        m_LocalPlayerTeleportProvider = FindFirstObjectByType<TeleportationProvider>();
        
        selectionMenu.OnFinished += FinishGame;
        
        mainMenu.onExerciseChanged += SetExercise;
        mainMenu.onProbeChanged += SetProbe;
        mainMenu.onPatientChanged += SetPatient;
        mainMenu.onExerciseCreation += CreateExercise;
        mainMenu.onSimulationToggle += ToggleUltrasound;
        mainMenu.onBordersToggle += ToggleCTBorders;
        mainMenu.onMoveScanToggle += ToggleMoveScan;
        
        _chosenProbeId = new NetworkVariable<ulong>(PLACEHOLDER); // placeholder value
        _chosenProbeId.OnValueChanged += OnProbeIdChange;

        _patientId = new NetworkVariable<int>(1);
        _patientId.OnValueChanged += OnPatientIdChange;

        _toggleUltrasound = new NetworkVariable<bool>(false);
        _toggleUltrasound.OnValueChanged += OnToggleSimulationChange;

        _toggleBorders = new NetworkVariable<bool>(false);
        _toggleBorders.OnValueChanged += OnToggleBordersChange;

        _toggleMoveScan = new NetworkVariable<bool>();
        _toggleMoveScan.OnValueChanged += OnToggleMoveScanChange;
        
        selectionMenu.onExerciseCreationFinished += SaveExercise;
    }

    private void CreateExercise()
    {
        if(_chosenProbeId.Value == PLACEHOLDER)
            PlayerHudNotification.Instance.ShowText("You need to select a probe prior to creating an exercise!");
        else
        {
            mainMenu.SetUsabilityCreateExerciseButton(false);
            mainMenu.SetUsabilityStartButton(false);
            selectionMenu.CreateExercise();
        }
    }

    private void SaveExercise(string exerciseName, List<bool> answers)
    {
        var exe = new Exercise { probe = _chosenProbe.name, answers = answers, artifactCode = _chosenProbe.GetComponentInChildren<ProbeUltrasoundLogic>().GetArtifactCode()};
        ExerciseManager.SaveExercise(exerciseName, exe);
        mainMenu.SetUsabilityStartButton(true);
        mainMenu.SetUsabilityCreateExerciseButton(true);
    }

    public override void SetupGame()
    {
        if (_selectedExercise != null)
        {
            var path = "Probes/" + _selectedExercise.probe;
            SetProbe(Resources.Load<GameObject>(path));
            SetPatient(_selectedExercise.artifactCode);
        }
    }

    public override void StartGame(ulong currentPlayerId)
    {
        if (_selectedExercise == null){
            PlayerHudNotification.Instance.ShowText("You need to select an exercise before starting!");
            SimulationManager.FinishGame();
            return;
        }
        
        if (currentPlayerId == XRINetworkPlayer.LocalPlayer.OwnerClientId)
        {
            var teleportRequest = new TeleportRequest
            {
                destinationPosition = startActivityTarget.position,
                destinationRotation = startActivityTarget.rotation
            };
            m_LocalPlayerTeleportProvider.QueueTeleportRequest(teleportRequest);
        }
        
        selectionMenu.StartExercise(_selectedExercise);
        mainMenu.SetUsabilityStartButton(false);
        mainMenu.SetUsabilityCreateExerciseButton(false);
    }

    public override void UpdateGame(float deltaTime)
    {
    }

    public override void FinishGame()
    {
        mainMenu.SetUsabilityStartButton(true);
        mainMenu.SetUsabilityCreateExerciseButton(true);
        SimulationManager.FinishGame();
    }

    private void SetProbe(GameObject probe)
    {
        if (!IsServer) return;
        
        if (_chosenProbe != null)
        {
            _chosenProbe.GetComponent<NetworkObject>().Despawn();
        }
        var spawnedProbe = Instantiate(probe, _probeSpawner);
        spawnedProbe.GetComponent<NetworkObject>().Spawn();
        _chosenProbeId.Value = spawnedProbe.GetComponent<NetworkObject>().NetworkObjectId;
    }

    private void SetPatient(int artifactCode)
    {
        if (!IsServer) return;
        _patientId.Value = artifactCode;
    }

    private void SetExercise(string exerciseName)
    {
        if (!IsServer) return;
        
        _selectedExercise = ExerciseManager.LoadExercise(exerciseName);
        SetupGame();
    }

    private void OnPatientIdChange(int previousvalue, int newvalue)
    {
        if (_chosenProbe != null)
        {
            _chosenProbe.GetComponentInChildren<ProbeUltrasoundLogic>().SetArtifactCode(_patientId.Value);
        }
        SetArtifactCodeClientRpc();
    }
    
    [ClientRpc]
    private void SetArtifactCodeClientRpc()
    {
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(_chosenProbeId.Value, out var networkObject))
        {
            var probe = networkObject.gameObject;
            probe.GetComponentInChildren<ProbeUltrasoundLogic>().SetArtifactCode(_patientId.Value);
        }
    }
    
    private void OnProbeIdChange(ulong previousvalue, ulong newvalue)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(_chosenProbeId.Value, out var networkObject))
        {
            _chosenProbe = networkObject.gameObject;
            SyncProbeSetupClientRpc(_chosenProbeId.Value);
        }
    }
    
    [ClientRpc]
    private void SyncProbeSetupClientRpc(ulong networkObjectId)
    {
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
        {
            var probe = networkObject.gameObject;
            _screenSettings.SetProbe(probe);
            probe.GetComponentInChildren<SlicingPlane>().USSection = _USsection;
            probe.GetComponentInChildren<ProbeUltrasoundLogic>().bigScreenRenderer = _screen.GetComponent<Renderer>();
            _screen.GetComponent<Renderer>().material.SetTexture("_MaskTex", probe.GetComponentInChildren<ProbeUltrasoundLogic>().screenMask);
            probe.GetComponentInChildren<ProbeUltrasoundLogic>().SetArtifactCode(_patientId.Value);
        }
    }
    
    public override void SyncUponClientJoin()
    {
        OnProbeIdChange(PLACEHOLDER, PLACEHOLDER+1);
        SyncScanSettingsClientRpc();
    }

    [ClientRpc]
    private void SyncScanSettingsClientRpc()
    {
        SyncSimToggleClientRpc();
        ToggleBordersClientRpc();
        ToggleMoveScanClientRpc();
    }
    
    private void ToggleUltrasound()
    {
        if(!IsServer) return;
        _toggleUltrasound.Value = !_toggleUltrasound.Value;
    }

    private void ToggleCTBorders()
    {
        if(!IsServer) return;
        _toggleBorders.Value = !_toggleBorders.Value;
    }
    
    private void ToggleMoveScan()
    {
        if(!IsServer) return;
        _toggleMoveScan.Value = !_toggleMoveScan.Value;
    }

    private void OnToggleSimulationChange(bool prev, bool newval)
    {
        if (_chosenProbe != null)
        {
            _chosenProbe.GetComponentInChildren<ProbeUltrasoundLogic>().DeactivateUltrasound(_toggleUltrasound.Value);
        }
        
        SyncSimToggleClientRpc();
    }

    [ClientRpc]
    private void SyncSimToggleClientRpc()
    {
        if (NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(_chosenProbeId.Value, out var networkObject))
        {
            var probe = networkObject.gameObject;
            probe.GetComponentInChildren<ProbeUltrasoundLogic>().DeactivateUltrasound(_toggleUltrasound.Value);
        }
    }

    private void OnToggleBordersChange(bool prev, bool newval)
    {
        if(_USsection != null)
            _USsection.transform.GetChild(0).gameObject.SetActive(_toggleBorders.Value);
        ToggleBordersClientRpc();
    }

    [ClientRpc]
    private void ToggleBordersClientRpc()
    {
        Debug.Log("Object active is currently " + _USsection.transform.GetChild(0).gameObject.activeSelf + " and toggleBorders is " + _toggleBorders.Value);
        _USsection.transform.GetChild(0).gameObject.SetActive(_toggleBorders.Value);
    }

    private void OnToggleMoveScanChange(bool prev, bool newval)
    {
        ToggleMoveScanClientRpc();
    }

    [ClientRpc]
    private void ToggleMoveScanClientRpc()
    {
        _USsection.transform.GetChild(1).gameObject.SetActive(_toggleMoveScan.Value);
        _USsection.GetComponent<XRGrabInteractable>().enabled = _toggleMoveScan.Value;
        _USsection.GetComponent<Collider>().enabled = _toggleMoveScan.Value;
    }
    
}
