using System;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private string savePath;
    private string backupPath;

    public SaveData Data;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "save_v1.json");
        backupPath = savePath + ".bak";

        Load();
    }

    public void Load()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            Data = JsonUtility.FromJson<SaveData>(json);
        }
        else
        {
            Data = new SaveData();
            Data.lastDate = DateTime.Now.ToString("yyyy-MM-dd");
            Save();
        }
    }
    public void Save()
    {
        if (File.Exists(savePath))
        {
            File.Copy(savePath, backupPath, true);
        }

        string json = JsonUtility.ToJson(Data, true);
        File.WriteAllText(savePath, json);
    }
    public bool IsNewDay()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        return Data.lastDate != today;
    }

    public void UpdateDate()
    {
        Data.lastDate = DateTime.Now.ToString("yyyy-MM-dd");
        Save();
    }
}