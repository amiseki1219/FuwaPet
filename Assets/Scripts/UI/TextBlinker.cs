using UnityEngine;
using TMPro;

public class TextBlinker : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float minAlpha = 0.2f;

    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (text == null) return;

        float alpha = Mathf.Lerp(minAlpha, 1f, (Mathf.Sin(Time.time * speed) + 1f) / 2f);
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}
