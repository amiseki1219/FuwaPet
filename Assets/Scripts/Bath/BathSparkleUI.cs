using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BathSparkleUI : MonoBehaviour
{
    [SerializeField] private Image sparkleImage;

    private RectTransform _rt;
    private Coroutine _coroutine;

    public void Play(Vector2 anchoredPosition)
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        _rt.anchoredPosition = anchoredPosition;
        gameObject.SetActive(true);
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(PlayCoroutine());
    }

    private IEnumerator PlayCoroutine()
    {
        _rt.localScale = Vector3.zero;

        if (sparkleImage != null)
        {
            var c = sparkleImage.color;
            c.a = 1f;
            sparkleImage.color = c;
        }

        // スケールアップ 0 → 1.2
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.12f;
            float scale = Mathf.Lerp(0f, 1.2f, Mathf.Clamp01(t));
            _rt.localScale = Vector3.one * scale;
            yield return null;
        }

        // フェードアウト + スケール 1.2 → 0.8
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.18f;
            float eased = Mathf.Clamp01(t);
            _rt.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.8f, eased);
            if (sparkleImage != null)
            {
                var c = sparkleImage.color;
                c.a = 1f - eased;
                sparkleImage.color = c;
            }
            yield return null;
        }

        gameObject.SetActive(false);
        _coroutine = null;
    }
}
