using UnityEngine;
using UnityEngine.UI;
using System;
using Game.Core;

public class BackgroundManager : MonoBehaviour
{
    [Header("背景を表示するImage")]
    [SerializeField] private Image backgroundImage;

    [Header("時間ごとの画像")]
    [SerializeField] private Sprite daySprite;     // 朝と昼用（例: 6:00 ~ 15:59）
    [SerializeField] private Sprite eveningSprite; // 夕方用（例: 16:00 ~ 18:59）
    [SerializeField] private Sprite nightSprite;   // 夜用（例: 19:00 ~ 5:59）

    private void OnEnable()
    {
        GameContext.OnAppResumed += OnAppResumed;
    }

    private void OnDisable()
    {
        GameContext.OnAppResumed -= OnAppResumed;
    }

    // 復帰時。日をまたいだり時間帯が変わっていることがあるので背景を貼り直す。
    private void OnAppResumed()
    {
        UpdateBackground();
    }

    void Start()
    {
        UpdateBackground();
    }

    public void UpdateBackground()
    {
        // 今の「時（0〜23）」を取得
        int hour = DateTime.Now.Hour;

        if (hour >= 6 && hour < 16)
        {
            // 6時〜15時台：朝・昼
            backgroundImage.sprite = daySprite;
        }
        else if (hour >= 16 && hour < 19)
        {
            // 16時〜18時台：夕方
            backgroundImage.sprite = eveningSprite;
        }
        else
        {
            // それ以外（19時〜5時台）：夜
            backgroundImage.sprite = nightSprite;
        }
    }
}