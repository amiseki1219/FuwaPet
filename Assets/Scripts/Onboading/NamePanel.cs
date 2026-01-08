using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class NamePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button decisionButton;
    [SerializeField] private ConfirmPanel confirmPanel;

    private void Start()
    {
        if (decisionButton != null)
        {
            // ここを OnDecisionClick に変えるよ
            decisionButton.onClick.AddListener(OnDecision);
            decisionButton.interactable = false;
        }

        if (nameInputField != null)
        {
            nameInputField.onValueChanged.AddListener(OnValueChanged);
        }
    }

    private void OnValueChanged(string text)
    {
        decisionButton.interactable = !string.IsNullOrEmpty(text);
    }

    // 元々の機能は残したまま、名前だけ保存できるようにしておくね
    public void SavePetInfo()
    {
        var data = SaveManager.Instance.Data;
        data.petName = nameInputField.text;
    }
    public void OnDecision()
    {
        // 1. 名前を保存する
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.Data.petName = nameInputField.text;
            SaveManager.Instance.Save();
            Debug.Log("名前をセーブしたよ！");
        }

        // 2. 確認パネルを「ゲームオブジェクト」として表示する
        if (confirmPanel != null)
        {
            // ★ここを必ず .gameObject.SetActive(true) にしてね！
            confirmPanel.gameObject.SetActive(true);

            // 3. パネルの中身（テキストなど）を更新する
            confirmPanel.Open(nameInputField.text);

            Debug.Log("ConfirmPanelを表示させたよ！");
        }

        // 4. 自分（名前入力パネル）を消す
        this.gameObject.SetActive(false);
    }
    // NamePanel.cs の中に追加
    [SerializeField] private GameObject characterPanelCard;

    public void OnClickBack()
    {
        this.gameObject.SetActive(false);    // 自分（名前入力）を消す
        characterPanelCard.SetActive(true); // キャラ選択画面を出す
    }

}

