using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BathSceneManager : MonoBehaviour
{
    [System.Serializable]
    private class ShampooData
    {
        public string id;
        public string displayName;
        public string imageName;
        public string description;
        public string effectText;
        public int costCoin;
        public int costLuna;
    }

    private static readonly List<ShampooData> AllShampoo = new List<ShampooData>
    {
        new ShampooData
        {
            id = "normal",
            displayName = "せっけん",
            imageName = "NomalImage",
            description = "さっぱりやさしい泡立ち。\n毎日使えるシンプルなせっけん",
            effectText = "清潔 +40",
            costCoin = 0, costLuna = 0
        },
        new ShampooData
        {
            id = "ichigo",
            displayName = "いちごシャンプー",
            imageName = "IchigoImage",
            description = "ふんわり甘くてかわいい香り。\n使うたびに甘えん坊になっちゃう？",
            effectText = "清潔 +40\n甘えん坊度 +2",
            costCoin = 0, costLuna = 50
        },
        new ShampooData
        {
            id = "hoshizora",
            displayName = "ほしぞらシャンプー",
            imageName = "HoshiImage",
            description = "星空みたいな神秘的な香り。\nコツコツがんばる気持ちが芽生えるかも",
            effectText = "清潔 +40\n勤勉さ +2",
            costCoin = 0, costLuna = 50
        },
        new ShampooData
        {
            id = "rainbow",
            displayName = "レインボーせっけん",
            imageName = "RainbowImage",
            description = "7色の泡があふれだす！\nどんな変化が起きるかはおたのしみ♪",
            effectText = "清潔 +40\n性格が少しランダムで変化する?!",
            costCoin = 100, costLuna = 0
        },
    };

    private static readonly string[] SpeechBubbleTexts =
    {
        "今日はどの香りにする〜？",
        "ふわふわ泡で洗ってほしいな",
        "やさしくごしごししてね",
        "いいにおいにしてくれる？",
        "今日もいっぱい遊んだ！きれいにして〜",
        "ぴかぴかになる準備できてるよ",
        "今日は甘い香りがいい気分…♡",
        "おふろでさっぱりしたいな！",
        "いい香りのおふろ、たのしみ♪",
        "どれ使うの？わくわくする〜！",
    };

    [Header("パネル切り替え")]
    [SerializeField] private GameObject selectShampooPanel;
    [SerializeField] private GameObject washPanel;
    [SerializeField] private BathWashManager bathWashManager;
    [SerializeField] private Button goNextButton;

    [Header("財布表示")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI lunaText;

    [Header("吹き出し")]
    [SerializeField] private TextMeshProUGUI speechBubbleText;

    [Header("シャンプーボタン")]
    [SerializeField] private Button nomalButton;
    [SerializeField] private Button ichigoButton;
    [SerializeField] private Button hoshizoraButton;
    [SerializeField] private Button rainbowButton;

    [Header("選択中Panel")]
    [SerializeField] private RawImage selectRawImage;
    [SerializeField] private TextMeshProUGUI selectSorpName;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI effortText;

    private string _selectedId = "normal";
    private Coroutine _coinCoroutine;
    private Coroutine _lunaStoneCoroutine;

    private void Start()
    {
        SetRandomSpeechBubble();
        SetupButtonListeners();
        OnSelectShampoo("normal");
        if (goNextButton != null) goNextButton.onClick.AddListener(OnGoNext);
        RefreshWallet();
    }

    private void OnGoNext()
    {
        var data = AllShampoo.Find(s => s.id == _selectedId);
        if (data == null) return;

        bool hasCost = data.costCoin > 0 || data.costLuna > 0;
        if (hasCost)
        {
            if (data.costCoin > 0)
            {
                if (GameData.Instance != null)
                {
                    if (!GameData.Instance.UseCoin(data.costCoin)) { Debug.LogWarning("コインが足りません"); return; }
                }
                else
                {
                    var save = SaveManager.Instance?.Data;
                    if (save == null || save.coinCount < data.costCoin) { Debug.LogWarning("コインが足りません"); return; }
                    save.coinCount -= data.costCoin;
                    SaveManager.Instance?.Save();
                }
            }
            if (data.costLuna > 0)
            {
                if (GameData.Instance != null)
                {
                    if (!GameData.Instance.UseLunaStone(data.costLuna)) { Debug.LogWarning("ルナストーンが足りません"); return; }
                }
                else
                {
                    var save = SaveManager.Instance?.Data;
                    if (save == null || save.lunaStoneCount < data.costLuna) { Debug.LogWarning("ルナストーンが足りません"); return; }
                    save.lunaStoneCount -= data.costLuna;
                    SaveManager.Instance?.Save();
                }
            }
            RefreshWallet();
        }

        if (selectShampooPanel != null) selectShampooPanel.SetActive(false);
        if (washPanel          != null) washPanel.SetActive(true);
        bathWashManager?.Initialize(_selectedId);
    }

    private void RefreshWallet()
    {
        int coin = GameData.Instance != null ? GameData.Instance.Coin
                 : SaveManager.Instance?.Data?.coinCount ?? 0;
        int luna = GameData.Instance != null ? GameData.Instance.LunaStone
                 : SaveManager.Instance?.Data?.lunaStoneCount ?? 0;

        if (coinText != null)
        {
            int from = int.TryParse(coinText.text, out int parsedCoin) ? parsedCoin : coin;
            if (_coinCoroutine != null) StopCoroutine(_coinCoroutine);
            _coinCoroutine = StartCoroutine(AnimateCoinText(coinText, from, coin, 0.5f));
        }
        if (lunaText != null)
        {
            int from = int.TryParse(lunaText.text, out int parsedLuna) ? parsedLuna : luna;
            if (_lunaStoneCoroutine != null) StopCoroutine(_lunaStoneCoroutine);
            _lunaStoneCoroutine = StartCoroutine(AnimateCoinText(lunaText, from, luna, 0.5f));
        }
    }

    private IEnumerator AnimateCoinText(TextMeshProUGUI text, int fromValue, int toValue, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            text.text = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, t)).ToString();
            yield return null;
        }
        text.text = toValue.ToString();
    }

    private void SetRandomSpeechBubble()
    {
        if (speechBubbleText == null) return;
        speechBubbleText.text = SpeechBubbleTexts[Random.Range(0, SpeechBubbleTexts.Length)];
    }

    private void SetupButtonListeners()
    {
        if (nomalButton     != null) nomalButton.onClick.AddListener(()     => OnSelectShampoo("normal"));
        if (ichigoButton    != null) ichigoButton.onClick.AddListener(()    => OnSelectShampoo("ichigo"));
        if (hoshizoraButton != null) hoshizoraButton.onClick.AddListener(() => OnSelectShampoo("hoshizora"));
        if (rainbowButton   != null) rainbowButton.onClick.AddListener(()   => OnSelectShampoo("rainbow"));
    }

    public void OnSelectShampoo(string shampooId)
    {
        _selectedId = shampooId;
        UpdateSelectFrames(shampooId);
        UpdateInfoPanel(shampooId);
    }

    private void UpdateSelectFrames(string shampooId)
    {
        UpdateFrame(nomalButton,     "normal",     shampooId);
        UpdateFrame(ichigoButton,    "ichigo",     shampooId);
        UpdateFrame(hoshizoraButton, "hoshizora",  shampooId);
        UpdateFrame(rainbowButton,   "rainbow",    shampooId);
    }

    private void UpdateFrame(Button btn, string btnId, string selectedId)
    {
        if (btn == null) return;
        var frame = btn.transform.Find("SelectFrame");
        if (frame != null)
            frame.gameObject.SetActive(btnId == selectedId);
    }

    private void UpdateInfoPanel(string shampooId)
    {
        var data = AllShampoo.Find(s => s.id == shampooId);
        if (data == null) return;

        if (selectSorpName  != null) selectSorpName.text  = data.displayName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (effortText      != null) effortText.text      = data.effectText;

        if (selectRawImage != null)
        {
            var tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName.Normalize(System.Text.NormalizationForm.FormC)}");
            if (tex == null)
                tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName.Normalize(System.Text.NormalizationForm.FormD)}");
            if (tex == null)
                tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName}");
            selectRawImage.texture = tex;
        }
    }
}
