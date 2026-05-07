using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoachMarkController : MonoBehaviour
{
    [Header("暗転オーバーレイ")]
    [SerializeField] private Image darkOverlay;

    [Header("スポットライト枠（クエストボタンに重ねる）")]
    [SerializeField] private RectTransform spotlightFrame;

    [Header("ガイドテキスト")]
    [SerializeField] private TextMeshProUGUI guideText;

    [Header("ターゲット（クエストボタンの RectTransform）")]
    [SerializeField] private RectTransform questButtonRect;

    [SerializeField] private float padding = 12f;

    private void OnEnable()
    {
        PositionSpotlight();
        if (guideText != null) guideText.text = "クエストを見てみよう！";
    }

    private void PositionSpotlight()
    {
        if (questButtonRect == null || spotlightFrame == null) return;
        spotlightFrame.position = questButtonRect.position;
        spotlightFrame.sizeDelta = questButtonRect.sizeDelta + Vector2.one * padding * 2f;
    }

    // 全画面透明ボタンの onClick に登録する
    public void Dismiss()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.coachMarkShown = true;
            SaveManager.Instance.Save();
        }
        gameObject.SetActive(false);
    }
}
