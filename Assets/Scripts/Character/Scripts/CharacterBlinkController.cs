using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 5キャラ共通のまばたき制御。PokoBlinkController と同じ動作で、参照先を CharacterFaceController にしたもの。
/// </summary>
public class CharacterBlinkController : MonoBehaviour
{
    [SerializeField] private CharacterFaceController faceController;
    [SerializeField] private Texture2D closeEyeL;
    [SerializeField] private Texture2D closeEyeR;
    [SerializeField] private float minBlinkInterval = 3f;
    [SerializeField] private float maxBlinkInterval = 6f;
    [SerializeField] private float blinkDuration = 0.12f;
    [SerializeField] private bool enableBlink = true;

    /// <summary>目を閉じた絵を使う表情はまばたきしない（見た目が変わらないため）。</summary>
    [SerializeField]
    private List<string> skipBlinkExpressionKeys = new List<string> { "Shy", "Close", "Relaxed" };

    private void Start()
    {
        if (enableBlink)
            StartCoroutine(BlinkLoop());
    }

    private IEnumerator BlinkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minBlinkInterval, maxBlinkInterval));

            if (faceController == null || closeEyeL == null || closeEyeR == null) continue;

            if (skipBlinkExpressionKeys.Contains(faceController.CurrentExpressionKey)) continue;

            faceController.SetEyes(closeEyeL, closeEyeR);
            yield return new WaitForSeconds(blinkDuration);
            faceController.RestoreCurrentExpressionEyes();
        }
    }
}
