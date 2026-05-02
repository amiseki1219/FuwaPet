using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スライド式トグルスイッチのビジュアル制御。
/// SettingManager から SetState(bool, bool) を呼んで状態を切り替える。
/// </summary>
public class ToggleSwitchUI : MonoBehaviour
{
    [Header("--- References ---")]
    [SerializeField] private Image background;
    [SerializeField] private RectTransform knob;

    [Header("--- Colors ---")]
    [SerializeField] private Color colorOn  = new Color(1f, 0.42f, 0.62f, 1f);
    [SerializeField] private Color colorOff = new Color(0.78f, 0.78f, 0.78f, 1f);

    [Header("--- Knob Positions (X) ---")]
    [SerializeField] private float knobOnX  =  20f;
    [SerializeField] private float knobOffX = -20f;

    [Header("--- Animation ---")]
    [SerializeField] private float animDuration = 0.15f;

    private Coroutine animCoroutine;

    /// <summary>
    /// トグルの状態を設定する。
    /// </summary>
    /// <param name="isOn">ON にする場合は true</param>
    /// <param name="animate">アニメーションを行うか（初期設定時は false 推奨）</param>
    public void SetState(bool isOn, bool animate = true)
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        if (animate && gameObject.activeInHierarchy && Application.isPlaying)
            animCoroutine = StartCoroutine(AnimateToState(isOn));
        else
            ForceVisuals(isOn);
    }

    private void ForceVisuals(bool isOn)
    {
        if (background != null) background.color = isOn ? colorOn : colorOff;
        if (knob != null) knob.anchoredPosition = new Vector2(isOn ? knobOnX : knobOffX, 0f);
    }

    private IEnumerator AnimateToState(bool isOn)
    {
        Color startColor = background != null ? background.color : (isOn ? colorOff : colorOn);
        Color endColor   = isOn ? colorOn : colorOff;
        float startX     = knob != null ? knob.anchoredPosition.x : (isOn ? knobOffX : knobOnX);
        float endX       = isOn ? knobOnX : knobOffX;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            // ease in-out cubic
            t = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 8f;

            if (background != null)
                background.color = Color.Lerp(startColor, endColor, t);
            if (knob != null)
                knob.anchoredPosition = new Vector2(Mathf.Lerp(startX, endX, t), 0f);

            yield return null;
        }

        ForceVisuals(isOn);
        animCoroutine = null;
    }
}
