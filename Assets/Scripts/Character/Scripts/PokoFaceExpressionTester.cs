using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PokoFaceExpressionTester : MonoBehaviour
{
    [SerializeField] private PokoFaceController faceController;
    [SerializeField] private float intervalSeconds = 5f;
    [SerializeField] private List<string> testExpressionKeys = new List<string>();

    [Header("CloseEye Transition")]
    [SerializeField] private Texture2D closeEyeL;
    [SerializeField] private Texture2D closeEyeR;
    [SerializeField] private float transitionCloseDuration = 0.12f;
    [SerializeField] private bool useCloseEyeTransition = true;

    private int _currentIndex;

    private void Start()
    {
        if (testExpressionKeys.Count == 0) return;
        StartCoroutine(CycleExpressions());
    }

    private IEnumerator CycleExpressions()
    {
        while (true)
        {
            faceController?.SetExpression(testExpressionKeys[_currentIndex]);
            yield return new WaitForSeconds(intervalSeconds);

            _currentIndex = (_currentIndex + 1) % testExpressionKeys.Count;

            if (useCloseEyeTransition && faceController != null && closeEyeL != null && closeEyeR != null)
            {
                faceController.SetEyes(closeEyeL, closeEyeR);
                yield return new WaitForSeconds(transitionCloseDuration);
            }
        }
    }
}
