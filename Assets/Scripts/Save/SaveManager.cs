using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SaveManager>();
            }
            return _instance;
        }
    }

    private string savePath;
    private string backupPath;
    public SaveData Data;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
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

#if UNITY_EDITOR
    [MenuItem("SaveData/Delete Save File")]
    public static void DeleteSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, "save_v1.json");
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("セーブデータを削除したよ！次は真っさらから始まるよ！");
        }
        else
        {
            Debug.Log("消すデータがそもそもなかったよ！");
        }
    }
#endif
    public void DeleteData()
    {
        // 1. ファイルを物理的に消す
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        if (File.Exists(backupPath))
        {
            File.Delete(backupPath);
        }

        // 2. 今メモリにあるデータも真っさらにする
        Data = new SaveData();

        Debug.Log("<color=red>SaveManager: 全てのファイルを削除してデータを初期化したよ！</color>");
    }
}