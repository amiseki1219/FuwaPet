using UnityEngine;
using UnityEngine.UI;

public class StatusBarEffects : MonoBehaviour
{
    private Slider slider;

    [Header("ピンチの時の設定")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Color warningColor = Color.red; // 赤く点滅する色
    [SerializeField] private float blinkSpeed = 4.0f;

    // もともとの色を覚えておくための変数
    private Color originalColor;

    void Awake()
    {
        slider = GetComponent<Slider>();
        // 最初にインスペクターで設定されている色を覚えさせておくよ
        if (fillImage != null)
        {
            originalColor = fillImage.color;
        }
    }

    void Update()
    {
        if (slider == null || fillImage == null) return;

        // 20%以下の時だけ点滅させる
        if (slider.value <= 0.2f)
        {
            float t = Mathf.PingPong(Time.time * blinkSpeed, 1.0f);
            // 「あみまるが設定した色」と「赤」の間で点滅！
            fillImage.color = Color.Lerp(originalColor, warningColor, t);
        }
        else
        {
            // 20%より多い時は、あみまるが最初に決めた色に戻す
            fillImage.color = originalColor;
        }
    }
}