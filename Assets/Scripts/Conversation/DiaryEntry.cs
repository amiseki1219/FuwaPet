using System;
using System.Collections.Generic;

[Serializable]
public class DiaryEntry
{
    public string date;
    public string text;
    public string mood;
    public List<string> usedWords = new();
}