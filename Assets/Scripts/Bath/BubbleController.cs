using System.Collections;
using UnityEngine;

public class BubbleController : MonoBehaviour
{
    private const float LerpSpeed = 8f;

    private Vector3 _targetScale;
    private bool _isShowing;
    private bool _isHiding;

    private void Update()
    {
        if (_isShowing)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * LerpSpeed);
            if (Vector3.Distance(transform.localScale, _targetScale) < 0.001f)
            {
                transform.localScale = _targetScale;
                _isShowing = false;
            }
        }
        else if (_isHiding)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * LerpSpeed);
            if (transform.localScale.magnitude < 0.001f)
            {
                transform.localScale = Vector3.zero;
                _isHiding = false;
            }
        }
    }

    public void Show(float size)
    {
        StopAllCoroutines();
        _targetScale = new Vector3(size, size, size);
        _isShowing = true;
        _isHiding = false;
    }

    public void Hide()
    {
        StopAllCoroutines();
        _isShowing = false;
        _isHiding = true;
    }

    public void PopEffect()
    {
        StopAllCoroutines();
        _isShowing = false;
        _isHiding = false;
        StartCoroutine(PopCoroutine());
    }

    private IEnumerator PopCoroutine()
    {
        Vector3 start = transform.localScale;
        Vector3 peak  = start * 1.4f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            transform.localScale = Vector3.Lerp(start, peak, t);
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 14f;
            transform.localScale = Vector3.Lerp(peak, Vector3.zero, t);
            yield return null;
        }
        transform.localScale = Vector3.zero;
    }
}
