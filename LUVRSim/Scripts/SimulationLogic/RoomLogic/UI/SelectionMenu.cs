using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

public class SelectionMenu : NetworkBehaviour
{
    private enum MenuState {LungSliding, Profile, Artifacts, Diagnosis, Creation, Finish}

    [SerializeField] private GameObject lungSlidingMenu;
    [SerializeField] private GameObject profileMenu;
    [SerializeField] private GameObject artifactsMenu;
    [SerializeField] private GameObject diagnosisMenu;
    [SerializeField] private GameObject finishMenu;
    [SerializeField] private GameObject creationMenu;
    
    private GameObject _activeMenu;
    private List<bool> _answers;
    private Exercise _selectedExercise;
    private readonly Dictionary<MenuState, GameObject> _menus = new();
    private bool _isCreatingExercise;
    
    public Action OnFinished;
    public Action<string, List<bool>> onExerciseCreationFinished;
    
    private readonly NetworkVariable<MenuState> _currentMenuState = new();
    
    void Start()
    {
        _answers = new ();

        lungSlidingMenu.SetActive(false);
        profileMenu.SetActive(false);
        artifactsMenu.SetActive(false);
        diagnosisMenu.SetActive(false);
        finishMenu.SetActive(false);
        creationMenu.SetActive(false);

        _menus.Add(MenuState.LungSliding, lungSlidingMenu);
        _menus.Add(MenuState.Artifacts, artifactsMenu);
        _menus.Add(MenuState.Profile, profileMenu);
        _menus.Add(MenuState.Diagnosis, diagnosisMenu);
        _menus.Add(MenuState.Finish, finishMenu);
        _menus.Add(MenuState.Creation, creationMenu);
        
        _activeMenu = lungSlidingMenu; // initialize here activeMenu to avoid null references 
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _currentMenuState.OnValueChanged += ChangeMenu;
        if (IsServer)
        {
            _currentMenuState.Value = MenuState.LungSliding;
            _activeMenu.SetActive(false);
        }
    }

    public void Next()
    {
        ToggleMenu toggleMenu = _activeMenu.GetComponentInChildren<ToggleMenu>();
        List<bool> currAnswers = new ();
        
        if (toggleMenu != null)
        {
            currAnswers.AddRange(toggleMenu.GetAnswersBool());
        }
        
        NextServerRpc(currAnswers.ToArray());
    }

    public void SubmitExerciseName()
    {
        string exerciseName = creationMenu.GetComponentInChildren<TMP_InputField>().text;
        SaveExerciseServerRpc(exerciseName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void NextServerRpc(bool[] currAnswersBools)
    {
        if(!IsServer) return;

        if (currAnswersBools.Length != 0)
        {
            _answers.AddRange(currAnswersBools);
        }

        switch (_currentMenuState.Value)
        {
            case MenuState.LungSliding:
                _currentMenuState.Value = MenuState.Profile;
                break;
            case ( MenuState.Profile ):
                _currentMenuState.Value = MenuState.Artifacts;
                break;
            case (MenuState.Artifacts):
                _currentMenuState.Value = MenuState.Diagnosis;
                break;
            case (MenuState.Diagnosis):
                if (!_isCreatingExercise)
                {
                    _currentMenuState.Value = MenuState.Finish;
                    StartEvaluationServerRpc();   
                }
                else
                {
                    _currentMenuState.Value = MenuState.Creation;
                }
                break;
            case MenuState.Creation:
                // handled in SubmitExerciseName
                break;
            case MenuState.Finish:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void StartExercise(Exercise exercise)
    {
        if (IsServer)
        {
            _selectedExercise = exercise;
            if(!_currentMenuState.Value.Equals(MenuState.LungSliding))
                _currentMenuState.Value = MenuState.LungSliding;
        }
        StartClientRpc();
    }

    public void CreateExercise()
    {
        if (IsServer)
        {
            _isCreatingExercise = true;
            if(!_currentMenuState.Value.Equals(MenuState.LungSliding))
                _currentMenuState.Value = MenuState.LungSliding;
        }
        StartClientRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SaveExerciseServerRpc(string exerciseName)
    {
        _isCreatingExercise = false;
        _currentMenuState.Value = MenuState.Finish;
        onExerciseCreationFinished?.Invoke(exerciseName, _answers);
        SendResultClientRpc($"Exercise {exerciseName} saved!");
        _answers.Clear();
    }

    [ClientRpc]
    private void StartClientRpc()
    {
        foreach (var menu in _menus)
        {
            var toggleMenu = menu.Value.GetComponentInChildren<ToggleMenu>(true);
            if(toggleMenu != null)
                toggleMenu.SetupToggle();
        }
        _activeMenu.SetActive(true);
    }

    [ServerRpc]
    private void StartEvaluationServerRpc()
    {
        if (_selectedExercise.answers.Count != _answers.Count)
        {
            Debug.Log("Something went horribly wrong!");
            return;
        }

        List<string> answerNames = new List<string>();
        answerNames.AddRange(lungSlidingMenu.GetComponentInChildren<ToggleMenu>().GetAnswersNames());
        answerNames.AddRange(profileMenu.GetComponentInChildren<ToggleMenu>().GetAnswersNames());
        answerNames.AddRange(artifactsMenu.GetComponentInChildren<ToggleMenu>().GetAnswersNames());
        answerNames.AddRange(diagnosisMenu.GetComponentInChildren<ToggleMenu>().GetAnswersNames());
        
        string correctAnswers = "";
        
        for(int i = 0; i < _selectedExercise.answers.Count - 1; i++)
        {
            if (_selectedExercise.answers[i])
            {
                correctAnswers += answerNames[i] + ", ";
            }
        }
        
        if (_selectedExercise.answers[^1])
        {
            correctAnswers += answerNames[_selectedExercise.answers.Count - 1];
        }

        string insertedAnswers = "";
        
        for(int i = 0; i < _answers.Count ; i++)
        {
            if (_answers[i])
            {
                insertedAnswers += answerNames[i] + ", ";
            }
        }
        
        if (_answers[^1])
        {
            insertedAnswers += answerNames[_answers.Count - 1];
        }
        
        Debug.Log(insertedAnswers);
        
        string outcome = $"The correct answers were: {correctAnswers}, Your answers were: {insertedAnswers}";
        _answers.Clear();
        
        SendResultClientRpc(outcome);
    }

    [ClientRpc]
    private void SendResultClientRpc(string text)
    {
        StartCoroutine(DisplayResult(text));
    }

    IEnumerator DisplayResult(string message)
    {
        Text text = _activeMenu.GetComponentInChildren<Text>();
        text.text = message;
        
        yield return new WaitForSeconds(10f);
        
        OnFinished?.Invoke();
        _activeMenu.SetActive(false);
    }

    private void ChangeMenu(MenuState previousState, MenuState newState)
    {
        RequestUIChangeClientRpc(newState);
    }

    [ClientRpc]
    private void RequestUIChangeClientRpc(MenuState newState)
    {
        _activeMenu.SetActive(false);
        _activeMenu = _menus[newState];
        _activeMenu.SetActive(true);
    }
}
