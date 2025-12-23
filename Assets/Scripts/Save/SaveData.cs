using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string selectedCharacterId;
    public string lastDate; // yyyy-MM-dd

    public List<WordData> words = new();
    public List<DiaryEntry> diaries = new();

}