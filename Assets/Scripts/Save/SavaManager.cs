using System;
using System.IO;
using UnityEngine;

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

    // ★ ここから下、変数の宣言を忘れずに足してね！
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

        // パスの設定などは Awake で OK
        savePath = Path.Combine(Application.persistentDataPath, "save_v1.json");
        backupPath = savePath + ".bak";

        Load();
    }
    // ...あとの Load や Save はそのままで大丈夫！

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