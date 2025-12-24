using System.Collections.Generic;
using UnityEngine;

public static class CharacterColorTable
{
    public static readonly Dictionary<int, string[]> ColorsByCharacterId = new()
    {
        // 1: cat
        { 1, new[] { "#000000", "#FFFFFF", "#808080" } },

        // 2: dog
        { 2, new[] { "#FFFFFF", "#B5651D", "#808080" } },
    };

    public static string GetColorHex(int characterId, int slot)
    {
        if (!ColorsByCharacterId.TryGetValue(characterId, out var arr)) return "#FFFFFF";
        int index = Mathf.Clamp(slot - 1, 0, 2);
        return arr[index];
    }
}
