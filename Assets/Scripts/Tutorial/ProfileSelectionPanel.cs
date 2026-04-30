using UnityEngine;
using UnityEngine.UI;
using System;

public class ProfileSelectionPanel : MonoBehaviour
{
    [Serializable]
    public struct IconSetting
    {
        public string iconId;            // 保存用のID（例：Icon1, Icon2）
        public Button button;            // IconButton, SecondIconButton を入れるお
        public GameObject selectedFrame; // 選んだ時に出る枠（Frame）
    }

    [Header("アイコンの設定（Sizeを2にしてね）")]
    [SerializeField] private IconSetting[] iconSettings;

    [Header("ボタンの設定")]
    [SerializeField] private Button nextButton; // ヒエラルキーの NextButton だお

    private string temporarySelectedId;

    void Start()
    {
        // 最初はNextボタンを押せなくする
        if (nextButton != null) nextButton.interactable = false;

        // 各アイコンボタンに「押した時の動き」を登録
        foreach (var setting in iconSettings)
        {
            if (setting.button == null) continue;

            // 最初は枠を全部消しておく
            if (setting.selectedFrame != null) setting.selectedFrame.SetActive(false);

            string id = setting.iconId;
            setting.button.onClick.RemoveAllListeners();
            setting.button.onClick.AddListener(() => OnSelect(id));
        }

        // OK（Next）ボタンに機能を登録
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnClickNext);
        }
    }

    private void OnSelect(string id)
    {
        temporarySelectedId = id;

        // 枠の表示切り替え
        foreach (var setting in iconSettings)
        {
            if (setting.selectedFrame != null)
            {
                setting.selectedFrame.SetActive(setting.iconId == id);
            }
        }

        // 何か選ばれたらNextボタンを押せるようにする
        if (nextButton != null) nextButton.interactable = true;
        Debug.Log($"<color=cyan>選択中: {id}</color>");
    }

    public void OnClickNext()
    {
        // 1. セーブデータに確定保存
        if (SaveManager.Instance != null && !string.IsNullOrEmpty(temporarySelectedId))
        {
            SaveManager.Instance.Data.profileImagePath = temporarySelectedId;
            SaveManager.Instance.Data.iconId = temporarySelectedId;
            SaveManager.Instance.Save();
            Debug.Log($"<color=green>セーブ完了！ ID: {temporarySelectedId}</color>");
        }

        // 2. 次のサブステップへ
        FindAnyObjectByType<StoryPanelManager>()?.NextSub();
    }
}