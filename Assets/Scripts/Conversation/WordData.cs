using System;
using System.Collections.Generic;

[Serializable]
public class WordData
{
    public string text;
    public string partOfSpeech;
    public string category;
    public List<string> emotions = new();

    public int familiarity;
    public string taughtAt;
    public string lastUsedAt;
    public int usedCount;
}