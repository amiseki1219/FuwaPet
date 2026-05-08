using System.Collections;
using UnityEngine;
using TMPro;

public class StatusPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private float floatDistance = 50f;
    [SerializeField] private float duration = 1f;

    private RectTransform _rt;
    private Vector2 _startPos;
    private Coroutine _coroutine;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _startPos = _rt.anchoredPosition;
        if (popupText == null)
            popupText = GetComponent<TextMeshProUGUI>();
        gameObject.SetActive(false);
    }

    public void Show(string text)
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        popupText.text = text;
        _rt.anchoredPosition = _startPos;
        gameObject.SetActive(true);
        _coroutine = StartCoroutine(AnimateCoroutine());
    }

    private IEnumerator AnimateCoroutine()
    {
        var color = popupText.color;
        color.a = 1f;
        popupText.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            _rt.anchoredPosition = _startPos + new Vector2(0f, floatDistance * t);
            color.a = 1f - t;
            popupText.color = color;
            yield return null;
        }

        gameObject.SetActive(false);
        _rt.anchoredPosition = _startPos;
    }
}