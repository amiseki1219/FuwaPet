using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class StatusPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI popupPrefab; // ポップアップ1行のPrefab
    [SerializeField] private Transform spawnPoint;        // キャラクターの周り

    public static StatusPopup Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 複数行のポップアップを順番に表示
    /// </summary>
    public void Show(List<string> messages)
    {
        StartCoroutine(ShowSequence(messages));
    }

    private IEnumerator ShowSequence(List<string> messages)
    {
        for (int i = 0; i < messages.Count; i++)
        {
            SpawnOne(messages[i], i * 0.15f); // 少しずつずらして表示
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void SpawnOne(string message, float offsetY)
    {
        if (popupPrefab == null || spawnPoint == null) return;

        TextMeshProUGUI tmp = Instantiate(popupPrefab, spawnPoint);
        tmp.text = message;

        // 縦にずらして重ならないようにする
        RectTransform rt = tmp.GetComponent<RectTransform>();
        rt.anchoredPosition += new Vector2(0, offsetY * -40f);

        StartCoroutine(AnimatePopup(tmp));
    }

    private IEnumerator AnimatePopup(TextMeshProUGUI tmp)
    {
        float duration = 1.2f;
        float elapsed  = 0f;

        RectTransform rt = tmp.GetComponent<RectTransform>();
        Vector2 startPos = rt.anchoredPosition;
        Color startColor = tmp.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 上に浮かび上がる
            rt.anchoredPosition = startPos + new Vector2(0, t * 60f);

            // フェードアウト（後半から）
            float alpha = t < 0.5f ? 1f : 1f - ((t - 0.5f) / 0.5f);
            tmp.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        Destroy(tmp.gameObject);
    }
}