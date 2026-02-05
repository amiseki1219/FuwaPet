using UnityEngine;
using UnityEngine.UI;
using System;

public class ProfileSelectionPanel : MonoBehaviour
{
    // キャラクターパネルと同じ「セット管理」の構造体
    [Serializable]
    public struct IconSetting
    {
        public string iconId;            // 保存用のID（例：Icon1, Icon2）
        public Button button;            // アイコンのボタン
        public GameObject selectedFrame; // 選んだ時に出る枠
    }

    [Header("キャラクターと同じ設定（Sizeを2にしてね）")]
    [SerializeField] private IconSetting[] iconSettings;

    [Header("画面遷移の設定")]
    [SerializeField] private Button confirmButton;

    void Start()
    {
        // 最初は「次へ」ボタンを無効にする
        if (confirmButton != null)
        {
            confirmButton.interactable = false;
        }

        // すべてのボタンに「押した時の動き」を登録
        foreach (var setting in iconSettings)
        {
            if (setting.button == null) continue;

            // 最初は枠を全部消しておく
            if (setting.selectedFrame != null) setting.selectedFrame.SetActive(false);

            // ボタンを押した時、そのIDを渡して OnSelect を呼ぶ
            string id = setting.iconId;
            setting.button.onClick.RemoveAllListeners();
            setting.button.onClick.AddListener(() => OnSelect(id));
        }
    }

    private void OnSelect(string id)
    {
        // 1. 選んだIDをセーブデータに一時保存
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            // 今までのやつ
            SaveManager.Instance.Data.profileImagePath = id;

            // ★これを追加！ランキングで使っている iconId にも同じIDを入れるお！
            SaveManager.Instance.Data.iconId = id;
        }

        // --- 以下そのまま ---
        foreach (var setting in iconSettings)
        {
            if (setting.selectedFrame != null)
            {
                setting.selectedFrame.SetActive(setting.iconId == id);
            }
        }

        if (confirmButton != null)
            confirmButton.interactable = true;

        Debug.Log($"選んだアイコンID: {id} (profileImagePathとiconIdの両方に保存したお！)");
    }

    public void OnClickNext()
    {
        // 4. セーブして次の画面へ！
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
        }

        // エラー修正ポイント：UnityEngine. を明示的に付ける
        var manager = UnityEngine.Object.FindAnyObjectByType<OnboardingManager>();
        if (manager != null)
        {
            manager.Next();
        }
    }
}