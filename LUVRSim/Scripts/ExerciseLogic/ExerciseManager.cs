using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public static class ExerciseManager
{
    // saved exercises are in JSON format, which is translated to string
    private const string SaveLocation = "/Exercises/";

    public static void SaveExercise(string exerciseName, Exercise exe)
    { 
        string path = Application.persistentDataPath + SaveLocation;
        
        if(!Directory.Exists(path))
            Directory.CreateDirectory(path);

        exe.probe = exe.probe.Replace("(Clone)", "");
        
        var json = JsonUtility.ToJson(exe);
        if (File.Exists(path + exerciseName + ".json"))
            return;
        File.WriteAllText(path + exerciseName + ".json", json);
    }

    public static Exercise LoadExercise(string exerciseName)
    {
        string path = Application.persistentDataPath + SaveLocation + exerciseName + ".json";
        Exercise exercise = new Exercise();

        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            exercise = JsonUtility.FromJson<Exercise>(json);
        }
        else
        {
            Debug.Log("No exercise found");
        }
        
        return exercise;
    }

    public static List<string> GetAllExerciseNames()
    {
        string path = Application.persistentDataPath + SaveLocation;
        var exercises = Directory.EnumerateFiles(path, "*.json");
        
        List<string> exerciseNames = new List<string>();
        foreach (var exercise in exercises)
        {
            exerciseNames.Add(Path.GetFileNameWithoutExtension(exercise));
        }
        
        return exerciseNames;
    }
}
