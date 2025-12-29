using System;
using System.Collections.Generic;


[Serializable]
public class SaveData
{
    //オーナー情報
    public string ownerName;
    public string ownerBirthday;

    //ペット情報
    public int characterId;
    public int petColorSlot;
    public string petName;

    //オンボーディング完了判定
    public bool onboardingCompleted;

    public string selectedCharacterId;
    public string lastDate; // yyyy-MM-dd

    public List<WordData> words = new();
    public List<DiaryEntry> diaries = new();


}