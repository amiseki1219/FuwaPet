using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;

public class CharacterPanelCardManager : MonoBehaviour
{
    [SerializeField] private Image mainCharacterImage;
    [SerializeField] private Image miniCharacterImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text characterDescriptionText;
    [SerializeField] private GameObject[] dots; // Dot_0〜Dot_4 を順に結線する想定（5要素）
    [SerializeField] private OnboardingManager onboardingManager;

    // キャラID（コード内固定・シーン結線は使わない）。並びは names/colors/descriptions と一致させる。
    private readonly string[] characterIds = { "poko", "eru", "koko", "paru", "piyoko" };

    private readonly string[] names = { "ぽこ", "える", "ここ", "ぱる", "ぴよこ" };

    private readonly string[] colors = { "F4A959", "5A98F4", "FD8A8F", "FFB5C7", "EFC482" };

    private readonly string[] descriptions =
    {
        // poko
        "太陽みたいに明るい天真爛漫なトイプードル！どんなに落ち込んでいても、ぽこのポジティブパワーにかかればすぐに笑顔になっちゃうはず。元気いっぱいのぽこと一緒にわくわくが止まらない毎日をスタートしよう！",
        // eru
        "白猫のぱるを妹に持つ、クールな黒猫。そばにいるだけで安心できるような、不思議なやさしさを持っている。信頼した相手には特別な穏やかさを見せてくれる。えると、ゆっくり穏やかな時間を過ごしてみませんか？",
        // koko
        "愛に溢れたおっとり癒し系のうさぎさん。耳のハートは優しさの印。穏やかな口調で包み込むような愛情で、あなたの日常をふんわり包み込んでくれます。疲れた心を癒してほしい究極な安心感を求めるあなたにおすすめ",
        // paru
        "黒猫のえるを兄に持つ、ちょっぴり生意気な白猫。ついつい強がった言葉が出ちゃうけど、本当は甘えたい気持ちを隠しているだけ。素直になるのは少し苦手だけど、心を許した相手には、こっそり特別な顔を見せてくれる。背伸びしたい年ごろのぱると、ゆっくり仲良くなってみませんか？",
        // piyoko
        "おやつを見つけると一直線！元気いっぱいでちょっぴり破天荒なひよこ。思いついたらすぐ動いちゃうぴよこと一緒なら、毎日がにぎやかでワクワクいっぱいに。食いしん坊でまっすぐなぴよこと、笑顔あふれる毎日をスタートしましょう！"
    };

    private int currentIndex = 0;

    private void OnEnable()
    {
        currentIndex = 0;
        ShowCurrentCharacter();
    }

    private void ShowCurrentCharacter()
    {
        Debug.Log($"[CharCard] index={currentIndex} id={characterIds[currentIndex]}");

        string capitalized = Capitalize(characterIds[currentIndex]);

        // (a) メイン画像
        if (mainCharacterImage != null)
        {
            string path = $"TutorialUI/{capitalized}";
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                mainCharacterImage.sprite = sprite;
                mainCharacterImage.enabled = true;
            }
            else
            {
                mainCharacterImage.enabled = false;
                Debug.LogWarning($"[CharCard] フル画像ロード失敗: {path}");
            }
        }
        else
        {
            Debug.LogWarning("[CharCard] mainCharacterImage が未結線のため skip");
        }

        // (b) ミニ画像
        if (miniCharacterImage != null)
        {
            string path = $"TutorialUI/{capitalized}mini";
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                miniCharacterImage.sprite = sprite;
                miniCharacterImage.enabled = true;
            }
            else
            {
                miniCharacterImage.enabled = false;
                Debug.LogWarning($"[CharCard] ミニ画像ロード失敗: {path}");
            }
        }
        else
        {
            Debug.LogWarning("[CharCard] miniCharacterImage が未結線のため skip");
        }

        // (c) 名前・色・フォントサイズ
        if (characterNameText != null)
        {
            characterNameText.text = names[currentIndex];
            if (ColorUtility.TryParseHtmlString($"#{colors[currentIndex]}", out var c))
                characterNameText.color = c;
            else
                Debug.LogWarning($"[CharCard] 色パース失敗: #{colors[currentIndex]}");
            characterNameText.fontSize = 65;
        }
        else
        {
            Debug.LogWarning("[CharCard] characterNameText が未結線のため skip");
        }

        // (d) 説明文
        if (characterDescriptionText != null)
        {
            characterDescriptionText.text = descriptions[currentIndex];
        }
        else
        {
            Debug.LogWarning("[CharCard] characterDescriptionText が未結線のため skip");
        }

        // (e) Dot
        if (dots != null && dots.Length > currentIndex)
        {
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] != null)
                    dots[i].SetActive(i == currentIndex);
            }
        }
        else
        {
            Debug.LogWarning("[CharCard] dots が未結線または要素数不足のため skip");
        }
    }

    // 小文字IDの先頭1文字だけ大文字化（poko→Poko / piyoko→Piyoko）
    private string Capitalize(string id)
    {
        if (string.IsNullOrEmpty(id))
            return id;
        return char.ToUpper(id[0]) + id.Substring(1);
    }

    public void OnNextClicked()
    {
        currentIndex = (currentIndex + 1) % characterIds.Length;
        ShowCurrentCharacter();
    }

    public void OnPrevClicked()
    {
        currentIndex = (currentIndex - 1 + characterIds.Length) % characterIds.Length;
        ShowCurrentCharacter();
    }

    public void OnDecideClicked()
    {
        if (SaveManager.Instance != null && characterIds.Length > currentIndex)
        {
            var data = SaveManager.Instance.Data;
            data.selectedCharacterId = characterIds[currentIndex];
            SetInitialPersonality(data, characterIds[currentIndex]);
            SaveManager.Instance.Save();

            // 決定した事項（Saveが走った瞬間）を水色で強調
            Debug.Log($"<color=#00E5FF>[CharCard][DECIDE] selectedCharacterId={data.selectedCharacterId} "
                + $"活動性={data.personalityActivity} 甘えん坊度={data.personalityDependency} "
                + $"勤勉さ={data.personalityDiligence} 素直さ={data.personalityHonesty} "
                + $"感受性={data.personalitySensitivity}</color>");
        }

        Debug.Log("[CharCard] onboardingManager.Next() へ遷移");
        if (onboardingManager != null)
            onboardingManager.Next();
    }

    private void SetInitialPersonality(SaveData data, string characterId)
    {
        switch (characterId)
        {
            case "poko":
                data.personalityActivity    =  60;
                data.personalityDependency  =  70;
                data.personalityDiligence   =   0;
                data.personalityHonesty     =  80;
                data.personalitySensitivity =  40;
                break;
            case "eru":
                data.personalityActivity    = -40;
                data.personalityDependency  = -50;
                data.personalityDiligence   =  20;
                data.personalityHonesty     = -30;
                data.personalitySensitivity = -60;
                break;
            case "koko":
                data.personalityActivity    = -20;
                data.personalityDependency  =  40;
                data.personalityDiligence   = -20;
                data.personalityHonesty     =  60;
                data.personalitySensitivity =  70;
                break;
            case "paru":
                data.personalityActivity    =  30;
                data.personalityDependency  = -60;
                data.personalityDiligence   =  10;
                data.personalityHonesty     = -80;
                data.personalitySensitivity =  20;
                break;
            case "piyoko":
                data.personalityActivity    =  75;
                data.personalityDependency  =  35;
                data.personalityDiligence   = -45;
                data.personalityHonesty     =  65;
                data.personalitySensitivity =  20;
                break;
            default:
                Debug.LogWarning($"[CharacterPanelCardManager] 未定義のキャラID: {characterId}");
                break;
        }
    }
}
