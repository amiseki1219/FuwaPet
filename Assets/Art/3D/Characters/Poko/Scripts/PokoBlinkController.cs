using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokoBlinkController : MonoBehaviour
{
    [SerializeField] private FaceController faceController;
    [SerializeField] private Texture2D closeEyeL;
    [SerializeField] private Texture2D closeEyeR;
    [SerializeField] private float minBlinkInterval = 3f;
    [SerializeField] private float maxBlinkInterval = 6f;
    [SerializeField] private float blinkDuration = 0.12f;
    [SerializeField] private bool enableBlink = true;
    [SerializeField] private List<string> skipBlinkExpressionKeys = new List<string>();

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
