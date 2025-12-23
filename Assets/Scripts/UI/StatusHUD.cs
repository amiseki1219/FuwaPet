using TMPro;
using UnityEngine;
using Game.Core;

public class StatusHUD : MonoBehaviour
{
    [SerializeField] TMP_Text trustText;
    [SerializeField] TMP_Text hungerText;
    [SerializeField] TMP_Text moodText;

    void Update()
    {
        var pet = GameContext.Instance?.PetStatus;
        if (pet == null) return;

        trustText.text = $"Trust: {pet.Trust}";
        hungerText.text = $"Hunger: {Mathf.RoundToInt(pet.Hunger)}";
        moodText.text = $"Mood: {Mathf.RoundToInt(pet.Mood)}";
    }
}
