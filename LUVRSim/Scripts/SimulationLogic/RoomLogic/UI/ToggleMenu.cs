using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ToggleMenu : MonoBehaviour
{
    private List<Toggle> _toggles;

    void Start()
    {
        _toggles = new List<Toggle>();
        foreach (Transform child in transform)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                _toggles.Add(toggle);
            }
        }
    }

    public List<bool> GetAnswersBool()
    {
        List<bool> answers = new ();
        foreach (Toggle toggle in _toggles)
        {
            answers.Add(toggle.isOn);
        }
        return answers;
    }

    public List<string> GetAnswersNames()
    {
        List<string> answersNames = new ();
        foreach (Toggle toggle in _toggles)
        {
            answersNames.Add(toggle.GetComponentInChildren<Text>().text);
        }
        return answersNames;
    }

    
    public void SetupToggle()
    {
        _toggles = new List<Toggle>();
        foreach (Transform child in transform)
        {
            Toggle toggle = child.GetComponent<Toggle>();
            if (toggle != null)
            {
                _toggles.Add(toggle);
            }
        }
    }
}