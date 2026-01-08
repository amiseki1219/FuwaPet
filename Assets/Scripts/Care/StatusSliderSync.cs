using UnityEngine;
using UnityEngine.UI;
using Game.Core;

public class StatusSliderSync : MonoBehaviour
{
    // どっちのステータスを表示するか選べるようにするよ
    public enum StatusType { Hunger, Mood }
    [SerializeField] private StatusType type;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        var pet = GameContext.Instance?.PetStatus;
        if (pet == null) return;

        // 種類に合わせて、スライダーの値を更新！
        if (type == StatusType.Hunger)
        {
            slider.value = pet.Hunger; // お腹
        }
        else
        {
            slider.value = pet.Mood; // ご機嫌
        }
    }
}