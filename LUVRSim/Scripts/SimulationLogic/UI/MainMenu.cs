using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using XRMultiplayer;

public class MainMenu : NetworkBehaviour
{
    [Header("Dropdowns")] 
    [SerializeField] private TMP_Dropdown _patientsDropdown;
    [SerializeField] private TMP_Dropdown _probesDropdown;
    [SerializeField] private TMP_Dropdown _exercisesDropdown;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _choiceButton;
    
    private Dictionary<string, int> _patients = new (){ {"Patient A", 1}, {"Patient B", 2} };
    private GameObject[] _probes;
    private List<string> _exercises;
    
    public Action<GameObject> onProbeChanged;
    public Action<int> onPatientChanged;
    public Action<string> onExerciseChanged;
    public Action onExerciseCreation;
    public Action onSimulationToggle;
    public Action onBordersToggle;
    public Action onMoveScanToggle;
    
    void Start()
    {
        _probes = Resources.LoadAll<GameObject>("Probes");
        _exercises = ExerciseManager.GetAllExerciseNames();
        
        _exercisesDropdown.ClearOptions();
        var options = new List<string> { ".." };
        options.AddRange(_exercises.Select(item => item.ToString()).ToList());
        _exercisesDropdown.options = options.Select(text => new TMP_Dropdown.OptionData(text)).ToList();
        _exercisesDropdown.onValueChanged.AddListener(ChooseExercise);
    }

    public void ChoosePatient(TMP_Dropdown dropdown)
    {
        if (IsClient)
        {
            RequestChoosePatientServerRpc(dropdown.value);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestChoosePatientServerRpc(int index)
    {
        if (index < 1 || index > _patients.Count) return;
        onPatientChanged?.Invoke(_patients.ElementAt(index-1).Value);
    }
    
    public void ChooseProbe(TMP_Dropdown dropdown)
    {
        if (IsClient)
        {
            RequestChooseProbeServerRpc(dropdown.value);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestChooseProbeServerRpc(int index)
    {
        if (index < 1 || index > _probes.Length) return;
        onProbeChanged?.Invoke(_probes[index-1]);
    }

    public void ChooseExercise(int index)
    {
        if (IsClient)
        {
            RequestChooseExerciseServerRpc(index);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestChooseExerciseServerRpc(int index)
    {
        if (index < 1 || index >= _exercisesDropdown.options.Count) return;
        string exerciseName = _exercisesDropdown.options[index].text;
        onExerciseChanged?.Invoke(exerciseName);
    }

    public void CreateExercise()
    {
        RequestCreateExerciseServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCreateExerciseServerRpc()
    {
        onExerciseCreation?.Invoke();
    }

    public void DeactivateUltrasound()
    {
        DeactivateUltrasoundServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeactivateUltrasoundServerRpc()
    {
        onSimulationToggle?.Invoke();
    }

    public void ToggleBorders()
    {
        ToggleBordersServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleBordersServerRpc()
    {
        onBordersToggle?.Invoke();
    }
    
    public void ToggleMoveScan()
    {
        ToggleMoveScanServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleMoveScanServerRpc()
    {
        onMoveScanToggle?.Invoke();
    }

    public void SetUsabilityStartButton(bool choice)
    {
        _startButton.interactable = choice;
        SetUsabilityStartButtonClientRpc(choice);
    }

    public void SetUsabilityCreateExerciseButton(bool choice)
    {
        _choiceButton.interactable = choice;
    }

    [ClientRpc]
    private void SetUsabilityStartButtonClientRpc(bool choice)
    {
        _startButton.interactable = choice;
    }

    [ClientRpc]
    private void SetUsabilityCreateExerciseButtonClientRpc(bool choice)
    {
        _choiceButton.interactable = choice;
    }
}

public enum DropdownType
{
    Patients,
    Probes,
    Exercises
}
