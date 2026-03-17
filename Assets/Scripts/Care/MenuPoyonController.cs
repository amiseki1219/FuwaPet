using UnityEngine;
using DG.Tweening;

public class MenuPoyonController : MonoBehaviour
{
    [Header("飛び出すボタンたち")]
    [SerializeField] private RectTransform[] buttons;
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float delayInterval = 0.1f;

    void Start()
    {
        foreach (var btn in buttons) if (btn != null) btn.localScale = Vector3.zero;
    }

    public void OpenMenu()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            buttons[i].DOKill(); // 今のアニメを止めて上書き！
            buttons[i].DOScale(Vector3.one, duration).SetEase(Ease.OutBack).SetDelay(i * delayInterval);
        }
    }

    public void CloseMenu()
    {
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            btn.DOKill();
            btn.DOScale(Vector3.zero, duration * 0.5f).SetEase(Ease.InBack);
        }
    }
}