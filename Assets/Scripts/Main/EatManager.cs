using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EatManager : MonoBehaviour
{
    [Header("UIパネル設定")]
    [SerializeField] private GameObject eatPanel; // パネル本体
    [SerializeField] private RectTransform panelRect; // 動きを制御する座標

    [Header("アニメーション設定")]
    [SerializeField] private float slideSpeed = 10f; // 動く速さ

    private bool isPanelOpen = false;
    private Vector2 showPosition; // 表示時の位置
    private Vector2 hidePosition; // 隠れている時の位置

    void Start()
    {
        if (panelRect != null)
        {
            // インスペクターで設定した今の位置を「表示位置」として覚える
            showPosition = panelRect.anchoredPosition;
            // 画面の高さ分だけ下に下げた位置を「隠し位置」にする
            hidePosition = new Vector2(showPosition.x, -Screen.height);

            // 最初は隠しておく
            panelRect.anchoredPosition = hidePosition;
            eatPanel.SetActive(false);
        }
    }

    // EATボタンを押した時に呼ぶ関数
    public void ToggleEatPanel()
    {
        isPanelOpen = !isPanelOpen;
        StopAllCoroutines(); // 二重動作防止

        if (isPanelOpen)
        {
            eatPanel.SetActive(true);
            StartCoroutine(SlidePanel(showPosition));
        }
        else
        {
            StartCoroutine(SlidePanel(hidePosition, () => eatPanel.SetActive(false)));
        }
    }

    // パネルをスライドさせる演出
    private IEnumerator SlidePanel(Vector2 target, System.Action onComplete = null)
    {
        while (Vector2.Distance(panelRect.anchoredPosition, target) > 0.5f)
        {
            // Lerpを使って滑らかに移動
            panelRect.anchoredPosition = Vector2.Lerp(panelRect.anchoredPosition, target, Time.deltaTime * slideSpeed);
            yield return null;
        }
        panelRect.anchoredPosition = target;
        onComplete?.Invoke();
    }
}