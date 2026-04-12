using UnityEngine;
using UnityEngine.UI;

public class BlinkingImage : MonoBehaviour
{
    public float speed = 1.0f;
    private Image image;

    void Start()
    {
        image = GetComponent<Image>();
    }

    void Update()
    {
        if (image == null) return;
        float alpha = Mathf.Abs(Mathf.Sin(Time.time * speed));
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}