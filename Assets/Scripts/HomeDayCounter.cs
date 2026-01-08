using UnityEngine;
using TMPro;
using System;

public class HomeDayCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dayText;

    void Start()
    {
        DisplayDays();
    }

    private void DisplayDays()
    {
        if (SaveManager.Instance == null || string.IsNullOrEmpty(SaveManager.Instance.Data.startDate))
        {
            dayText.text = "出会って 1 日";
            return;
        }

        // 保存された日付から日数を計算
        DateTime startDate = DateTime.Parse(SaveManager.Instance.Data.startDate);
        DateTime today = DateTime.Today;

        int days = (today - startDate).Days + 1;
        dayText.text = $"出会って {days} 日";
    }
}