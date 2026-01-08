using UnityEngine;
using UnityEngine.UI;
using System; // Serializableを使うために必要

public class CharacterPanelLite : MonoBehaviour
{
    // ID、ボタン、枠をセットで管理するための専用の「箱」を作るよ
    [Serializable]
    public struct CharacterSetting
    {
        public string characterId;      // "Dog" "Cat" など（PetDisplay.csと合わせてね！）
        public Button button;           // キャラクターのボタン
        public GameObject selectedFrame; // 選択した時に出る枠
    }

    [Header("キャラクターの設定（Sizeを6にしてね）")]
    [SerializeField] private CharacterSetting[] characterSettings;

    [Header("画面遷移の設定")]
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject namePanel;

    private void Awake()
    {
        // セットし忘れがないかチェック！
        if (characterSettings == null || characterSettings.Length == 0)
        {
            Debug.LogError("CharacterSettingsが空っぽだよ！インスペクターで設定してね。");
        }
        if (nextButton == null) Debug.LogError("NextButtonがセットされてないよ！");
    }

    private void Start()
    {
        // 1. 最初は「次へ」ボタンを押せないようにする
        if (nextButton != null)
        {
            nextButton.interactable = false;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNext);
        }

        // 2. すべてのボタンに「押した時の動き」を登録するよ
        foreach (var setting in characterSettings)
        {
            if (setting.button == null) continue;

            // 枠を一旦消しておく
            if (setting.selectedFrame != null) setting.selectedFrame.SetActive(false);

            // ボタンを押した時、そのキャラのIDを渡して OnSelect を呼ぶ
            string id = setting.characterId;
            setting.button.onClick.RemoveAllListeners();
            setting.button.onClick.AddListener(() => OnSelect(id));
        }
    }

    private void OnSelect(string id)
    {
        // 3. 選んだIDをセーブデータ（メモリ）に一時保存
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.Data.selectedCharacterId = id;
        }

        // 4. 見た目の更新（選んだIDの枠だけを表示する）
        foreach (var setting in characterSettings)
        {
            if (setting.selectedFrame != null)
            {
                // 設定されているIDが、今選んだIDと同じなら表示(true)、違うなら非表示(false)
                setting.selectedFrame.SetActive(setting.characterId == id);
            }
        }

        // 5. キャラを選んだから「次へ」ボタンを有効にする
        if (nextButton != null)
            nextButton.interactable = true;

        Debug.Log($"選んだキャラクターID: {id}");
    }

    public void OnNext()
    {
        // 6. 「次へ」が押された瞬間にファイルへ書き込み（セーブ）！
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
            Debug.Log("キャラクターの選択をセーブしたよ！");
        }

        // 7. 次のパネル（名前入力）を表示して、自分を隠す
        if (namePanel != null)
        {
            namePanel.SetActive(true);
        }
        this.gameObject.SetActive(false);
    }
    // CharacterPanel.cs (または相当するスクリプト) に追加
    [SerializeField] private GameObject ownerPanelCard;

    public void OnClickBack()
    {
        this.gameObject.SetActive(false); // 自分（キャラ選択）を消す
        ownerPanelCard.SetActive(true);   // 最初のアカウント設定画面を出す
    }
}