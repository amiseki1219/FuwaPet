using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    // オーナー情報
    public string ownerName;
    public string ownerBirthday;
    public string playerId;

    // ペット情報
    public string characterId;
    public int petColorSlot;
    public string petName;

    // どのキャラを選んだか
    public string selectedCharacterId;
    public string startDate; // 出会った日
    public string lastDate;  // 最後に遊んだ日

    // オンボーディング完了判定
    public bool onboardingCompleted;

    public List<WordData> words = new();
    public List<DiaryEntry> diaries = new();
}