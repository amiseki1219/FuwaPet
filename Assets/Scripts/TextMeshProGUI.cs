using UnityEngine;
using UnityEngine.UI; // Textを使う場合
using TMPro; // TextMeshProを使う場合

public class BlinkingText : MonoBehaviour
{
    public float speed = 1.0f; // 点滅スピード
    private Text text;
    private TextMeshProUGUI tmpText;

    void Start()
    {
        // どっちのコンポーネントかチェックして取得
        text = GetComponent<Text>();
        tmpText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        // サイン波を使って透明度を0〜1の間で変化させる
        float alpha = Mathf.Abs(Mathf.Sin(Time.time * speed));

        if (text != null)
        {
            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }
        else if (tmpText != null)
        {
            Color color = tmpText.color;
            color.a = alpha;
            tmpText.color = color;
        }
    }
}