using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;

public class CharaNicknamePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private OnboardingManager onboardingManager;
    [SerializeField] private Image selectCharacterImage;
    [SerializeField] private TMP_Text[] recommendNameTexts;   // おすすめ名前4つのTMP
    [SerializeField] private Button[] recommendButtons;        // おすすめ名前4つのButton
    [SerializeField] private Toggle defaultNameToggle;
    [SerializeField] private TMP_Text nicknameWarningText;     // 警告（初期非表示）

    // キャラID → 日本語デフォルト名
    // ★2026/8/28：キャラID→日本語名の対応表は CharacterNames へ集約した。
    //   ここにあった DefaultNames は削除。見つからないときの "ぽこ" は呼び出し側で渡している。

    // キャラID → Select画像名（Resources/TutorialUI/ 配下）
    private static readonly Dictionary<string, string> SelectImageNames = new Dictionary<string, string>
    {
        { "poko", "SelectPoko" },
        { "eru", "SelectEru" },
        { "koko", "SelectKoko" },
        { "paru", "SelectParu" },
        { "piyoko", "SelectPiyoko" },
    };

    // おすすめ名前リスト（30個）
    private static readonly string[] RecommendNamePool =
    {
        "もちお", "ぷにお", "まるん", "こまち", "ちゃちゃ",
        "むぎ", "だんご", "きなこ", "もふ", "ぷくぷく",
        "ふわり", "こてつ", "ちび", "まめ", "ぽむ",
        "くうた", "ぬん", "みるく", "ぷりん", "わたげ",
        "ころん", "てんち", "ぽぽ", "うにゅ", "みかん",
        "もっち", "ぷちこ", "りんく", "なずな", "ゆず",
    };

    // 現在の選択キャラID（null/空なら poko フォールバック）
    private string ResolveSelectedCharacterId()
    {
        string id = SaveManager.Instance != null ? SaveManager.Instance.Data.selectedCharacterId : null;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[Nickname] selectedCharacterId が未設定のため poko をフォールバック");
            id = "poko";
        }
        return id;
    }

    private void OnEnable()
    {
        string id = ResolveSelectedCharacterId();

        // 1. 選択キャラ画像
        if (selectCharacterImage != null)
        {
            string imgName = SelectImageNames.TryGetValue(id, out var n) ? n : "SelectPoko";
            string path = $"TutorialUI/{imgName}";
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                selectCharacterImage.sprite = sprite;
                selectCharacterImage.enabled = true;
            }
            else
            {
                selectCharacterImage.enabled = false;
                Debug.LogWarning($"[Nickname] Select画像ロード失敗: {path}");
            }
        }
        else
        {
            Debug.LogWarning("[Nickname] selectCharacterImage が未結線のため skip");
        }

        // 2. おすすめ名前：シャッフルして先頭4つを表示
        string[] picks = PickRecommendNames(4);
        if (recommendNameTexts != null)
        {
            for (int i = 0; i < recommendNameTexts.Length; i++)
            {
                if (recommendNameTexts[i] == null) continue;
                if (i < picks.Length)
                {
                    string name = picks[i];
                    recommendNameTexts[i].text = name;
                    // 文字サイズ自動調整：7〜8文字なら15、それ以外は18
                    recommendNameTexts[i].fontSize = (name.Length >= 7 && name.Length <= 8) ? 15 : 18;
                }
            }
        }
        else
        {
            Debug.LogWarning("[Nickname] recommendNameTexts が未結線のため skip");
        }

        // 3. おすすめ名前タップで入力欄へ反映
        if (recommendButtons != null && recommendNameTexts != null)
        {
            for (int i = 0; i < recommendButtons.Length; i++)
            {
                if (recommendButtons[i] == null) continue;
                recommendButtons[i].onClick.RemoveAllListeners();
                int idx = i; // クロージャの i 束縛対策：ローカルにコピー
                recommendButtons[i].onClick.AddListener(() =>
                {
                    if (nicknameInput != null
                        && recommendNameTexts != null
                        && idx < recommendNameTexts.Length
                        && recommendNameTexts[idx] != null)
                    {
                        nicknameInput.text = recommendNameTexts[idx].text;
                    }
                });
            }
        }
        else
        {
            Debug.LogWarning("[Nickname] recommendButtons が未結線のため タップ反映を skip");
        }

        // 4. 警告テキスト初期非表示
        if (nicknameWarningText != null)
            nicknameWarningText.gameObject.SetActive(false);
        else
            Debug.LogWarning("[Nickname] nicknameWarningText が未結線のため skip");

        // 5. デフォルト名トグル初期化
        if (defaultNameToggle != null)
            defaultNameToggle.isOn = false;
        else
            Debug.LogWarning("[Nickname] defaultNameToggle が未結線のため skip");

        Debug.Log($"[Nickname] 初期化: selectedId={id} おすすめ={string.Join(",", picks)}");
    }

    // プールをシャッフルして先頭 count 件を返す（Fisher-Yates）
    private string[] PickRecommendNames(int count)
    {
        var list = new List<string>(RecommendNamePool);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        count = Mathf.Min(count, list.Count);
        return list.GetRange(0, count).ToArray();
    }

    public void OnDecideClicked()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[Nickname] SaveManager が null のため保存できません");
            return;
        }

        string savedValue;

        if (defaultNameToggle != null && defaultNameToggle.isOn)
        {
            // デフォルト名を採用
            string id = ResolveSelectedCharacterId();
            savedValue = CharacterNames.GetDefaultName(id, "ぽこ");
        }
        else
        {
            // 入力値バリデーション
            string input = nicknameInput != null ? nicknameInput.text : "";
            if (string.IsNullOrWhiteSpace(input))
            {
                if (nicknameWarningText != null)
                {
                    nicknameWarningText.text = "※なまえを入れてね";
                    nicknameWarningText.gameObject.SetActive(true);
                }
                return; // 保存も遷移もしない
            }
            savedValue = input;
        }

        SaveManager.Instance.Data.petNickname = savedValue;
        SaveManager.Instance.Save();

        if (nicknameWarningText != null)
            nicknameWarningText.gameObject.SetActive(false);

        bool toggleOn = defaultNameToggle != null && defaultNameToggle.isOn;
        Debug.Log($"<color=#00E5FF>[決定] ニックネーム確定: petNickname={savedValue} (toggle={toggleOn})</color>");

        if (onboardingManager != null) { onboardingManager.Next(); }
        else { Debug.LogWarning("OnboardingManager が未設定です。"); }
    }

    public void OnSkipClicked()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.petNickname = "";
            SaveManager.Instance.Save();
        }

        if (nicknameWarningText != null)
            nicknameWarningText.gameObject.SetActive(false);

        Debug.Log("<color=#00E5FF>[決定] ニックネームスキップ（空保存）</color>");

        if (onboardingManager != null) { onboardingManager.Next(); }
        else { Debug.LogWarning("OnboardingManager が未設定です。"); }
    }
}
