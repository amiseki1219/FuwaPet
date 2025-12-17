using System.Collections;
using TMPro;
using UnityEngine;

public class MessageUI : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private float showSeconds = 2.0f;

    Coroutine routine;

    public void Show(string msg)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(msg));
    }

    IEnumerator ShowRoutine(string msg)
    {
        messageText.text = msg;
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(showSeconds);
        messageText.text = "";
        messageText.gameObject.SetActive(false);
    }
}
