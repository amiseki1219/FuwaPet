using UnityEngine;
using UnityEngine.UI;

public class CharacterPanelLite : MonoBehaviour
{
    [Header("Assign in Inspector (Size must be 6)")]
    [SerializeField] private Button[] buttons;
    [SerializeField] private GameObject[] selectedFrames;

    [Header("Next")]
    [SerializeField] private Button nextButton;

    // 遷移先（どっちでもOK）
    [SerializeField] private GameObject namePanel;      // Panelを切り替える方式
    [SerializeField] private GameObject characterPanel; // 自分自身でもOK
    [SerializeField] private string[] characterIds; // サイズ6、"Dog" "Cat" など


    private int selectedIndex = -1;

    private void Awake()
    {
        // ここで Null を早期発見できるようにする
        if (buttons == null || buttons.Length == 0) Debug.LogError("Buttons is not assigned / empty");
        if (selectedFrames == null || selectedFrames.Length == 0) Debug.LogError("SelectedFrames is not assigned / empty");
        if (nextButton == null) Debug.LogError("NextButton is not assigned");

        if (buttons != null && selectedFrames != null && buttons.Length != selectedFrames.Length)
            Debug.LogError($"Buttons.Length ({buttons.Length}) != SelectedFrames.Length ({selectedFrames.Length})");

        // どれがnullか特定したい時に超効く
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i] == null) Debug.LogError($"Buttons[{i}] is NULL");
        }
        if (selectedFrames != null)
        {
            for (int i = 0; i < selectedFrames.Length; i++)
                if (selectedFrames[i] == null) Debug.LogError($"SelectedFrames[{i}] is NULL");
        }
    }

    private void Start()
    {
        // Nextは選ぶまで無効
        if (nextButton != null)
        {
            nextButton.interactable = false;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNext);
        }

        // 全枠オフ
        HideAllFrames();

        // ボタンにクリック登録
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i; // クロージャ対策
                if (buttons[i] == null) continue;

                buttons[i].onClick.RemoveAllListeners();
                buttons[i].onClick.AddListener(() => OnSelect(index));
            }
        }
    }


    private void OnSelect(int index)
    {
        selectedIndex = index;

        // 選択IDを保存（← これが今回の本題）
        SaveManager.Instance.Data.selectedCharacterId = characterIds[index];

        // ===== 見た目処理は今まで通り =====
        HideAllFrames();

        if (selectedFrames != null &&
            index >= 0 &&
            index < selectedFrames.Length &&
            selectedFrames[index] != null)
        {
            selectedFrames[index].SetActive(true);
        }

        if (nextButton != null)
            nextButton.interactable = true;

        Debug.Log($"Selected character index = {selectedIndex}, id = {characterIds[index]}");
    }


    private void HideAllFrames()
    {
        if (selectedFrames == null) return;
        for (int i = 0; i < selectedFrames.Length; i++)
        {
            if (selectedFrames[i] != null)
                selectedFrames[i].SetActive(false);
        }
    }

    // ↓ ここに public をつけるよ！
    public void OnNext()
    {
        if (selectedIndex < 0) return;
        if (namePanel != null) namePanel.SetActive(true);
        if (characterPanel != null) characterPanel.SetActive(false);
        else gameObject.SetActive(false);
    }

}
