using UnityEngine;
using UnityEngine.UI;

public class GaugeHeartFollower : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private RectTransform heartBase;

    private void LateUpdate()
    {
        if (slider == null || heartBase == null || slider.fillRect == null) return;
        RectTransform fillRect = slider.fillRect;
        float x = fillRect.anchoredPosition.x + fillRect.rect.width;
        heartBase.anchoredPosition = new Vector2(x, heartBase.anchoredPosition.y);
    }
}
