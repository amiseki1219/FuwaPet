using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class BathWashManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public static bool BathJustCompleted = false;
    private class ShampooData
    {
        public string id;
        public string displayName;
        public string imageName;
        public string description;
    }

    private static readonly List<ShampooData> AllShampoo = new List<ShampooData>
    {
        new ShampooData { id = "normal",    displayName = "せっけん",          imageName = "NomalImage",   description = "さっぱりやさしい泡立ち。\n毎日使えるシンプルなせっけん"          },
        new ShampooData { id = "ichigo",    displayName = "いちごシャンプー",   imageName = "IchigoImage",  description = "ふんわり甘くてかわいい香り。\n使うたびに甘えん坊になっちゃう？"   },
        new ShampooData { id = "hoshizora", displayName = "ほしぞらシャンプー", imageName = "HoshiImage",   description = "星空みたいな神秘的な香り。\nコツコツがんばる気持ちが芽生えるかも" },
        new ShampooData { id = "rainbow",   displayName = "レインボーせっけん", imageName = "RainbowImage", description = "7色の泡があふれだす！\nどんな変化が起きるかはおたのしみ♪"         },
    };

    private const int MaxRub = 24;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private TextMeshProUGUI rubCountText;
    [SerializeField] private Slider gaugeSlider;
    [SerializeField] private RawImage shampooIcon;
    [SerializeField] private TextMeshProUGUI shampooNameText;
    [SerializeField] private TextMeshProUGUI shampooDescriptionText;
    [SerializeField] private GameObject hintText;
    [SerializeField] private GameObject completeButton;

    [Header("タッチエフェクト")]
    [SerializeField] private BathTouchEffect touchEffect;

    [Header("手のカーソル")]
    [SerializeField] private RectTransform handCursor;
    [SerializeField] private BathSparkleUI sparkle;

    [Header("泡 (BubbleGroupの子を順番に登録)")]
    [SerializeField] private BubbleController[] bubbles;

    private int _rubCount;
    private bool _isComplete;
    private bool _inputBlocked;
    private string _shampooId;
    private System.Collections.IEnumerator _sliderCoroutine;

    // ── 初期化 ────────────────────────────────────────────────────────────────

    public void Initialize(string shampooId)
    {
        _shampooId    = shampooId;
        _rubCount     = 0;
        _isComplete   = false;
        _inputBlocked = true;  // GoNextButton のクリックイベントを1フレーム遮断
        StartCoroutine(UnblockInputNextFrame());

        if (completeButton != null) completeButton.SetActive(false);
        if (hintText != null) hintText.SetActive(true);
        if (handCursor != null) handCursor.gameObject.SetActive(false);

        UpdateUI();
        ResetBubbles();
        UpdateShampooInfo(shampooId);
    }

    private System.Collections.IEnumerator UnblockInputNextFrame()
    {
        yield return null;
        _inputBlocked = false;
    }

    // ── タップ検知 ────────────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[BathWash] OnPointerDown rubCount={_rubCount} isComplete={_isComplete} blocked={_inputBlocked}");
        if (_isComplete || _rubCount >= MaxRub || _inputBlocked) return;

        if (handCursor != null)
        {
            handCursor.gameObject.SetActive(true);
            // HandCursor は ScreenSpaceOverlay Canvas にあるため cam=null で変換
            var parentRect = handCursor.parent as RectTransform;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect, eventData.position, null, out local);
            handCursor.anchoredPosition = local;
        }

        sparkle?.Play(handCursor != null ? handCursor.anchoredPosition : Vector2.zero);
        touchEffect?.Play(eventData.position);
        _rubCount++;
        UpdateUI();
        UpdateBubbles(_rubCount);

        if (_rubCount >= MaxRub)
            OnWashComplete();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handCursor != null) handCursor.gameObject.SetActive(false);
    }

    // ── UI更新 ────────────────────────────────────────────────────────────────

    private void UpdateUI()
    {
        float pct = (float)_rubCount / MaxRub * 100f;
        if (percentText  != null) percentText.text  = $"{Mathf.RoundToInt(pct)}%";
        if (rubCountText != null) rubCountText.text = $"あと {_rubCount} 回";
        if (gaugeSlider  != null)
        {
            float target = (float)_rubCount / MaxRub;
            if (_sliderCoroutine != null) StopCoroutine(_sliderCoroutine);
            _sliderCoroutine = AnimateSliderCoroutine(gaugeSlider, gaugeSlider.value, target, 0.3f);
            StartCoroutine(_sliderCoroutine);
        }
    }

    private System.Collections.IEnumerator AnimateSliderCoroutine(Slider slider, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            slider.value = Mathf.Lerp(from, to, t);
            yield return null;
        }
        slider.value = to;
        _sliderCoroutine = null;
    }

    // ── 泡ステージ管理 ────────────────────────────────────────────────────────
    // bubbles配列の順番:
    // [0]Head_01 [1]Head_02 [2]Head_03 [3]Ear_L [4]Ear_R
    // [5]Body_01 [6]Body_02 [7]Body_03 [8]Body_04 [9]Tail

    private void UpdateBubbles(int rubCount)
    {
        int stage = rubCount / 6;
        switch (stage)
        {
            case 0:
                for (int i = 0; i < bubbles.Length; i++) SafeHide(i);
                break;
            case 1:
                for (int i = 0; i < 5  && i < bubbles.Length; i++) SafeShow(i, 0.15f);
                for (int i = 5; i < bubbles.Length; i++) SafeHide(i);
                break;
            case 2:
                for (int i = 0; i < bubbles.Length; i++) SafeShow(i, 0.15f);
                break;
            case 3:
                for (int i = 0; i < 5  && i < bubbles.Length; i++) SafeShow(i, 0.15f);
                for (int i = 5; i < 9  && i < bubbles.Length; i++) SafeHide(i);
                if (bubbles.Length > 9) SafeShow(9, 0.15f);
                break;
            default: // stage 4 (24回)
                for (int i = 0; i < bubbles.Length; i++) SafePop(i);
                break;
        }
    }

    private void SafeShow(int i, float size) { if (bubbles[i] != null) bubbles[i].Show(size); }
    private void SafeHide(int i)             { if (bubbles[i] != null) bubbles[i].Hide(); }
    private void SafePop(int i)              { if (bubbles[i] != null) bubbles[i].PopEffect(); }

    private void ResetBubbles()
    {
        foreach (var b in bubbles)
        {
            if (b == null) continue;
            b.transform.localScale = Vector3.zero;
        }
    }

    private void UpdateShampooInfo(string shampooId)
    {
        var data = AllShampoo.Find(s => s.id == shampooId);
        if (data == null) return;

        if (shampooNameText        != null) shampooNameText.text        = data.displayName;
        if (shampooDescriptionText != null) shampooDescriptionText.text = data.description;
        if (shampooIcon != null)
        {
            var tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName.Normalize(System.Text.NormalizationForm.FormC)}");
            if (tex == null) tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName.Normalize(System.Text.NormalizationForm.FormD)}");
            if (tex == null) tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName}");
            shampooIcon.texture = tex;
        }
    }

    // ── ボタンハンドラ ────────────────────────────────────────────────────────

    public void OnSkip()
    {
        _rubCount = MaxRub;
        UpdateUI();
        OnWashComplete();
    }

    private void OnWashComplete()
    {
        _isComplete = true;
        if (hintText != null) hintText.SetActive(false);
        if (completeButton != null) completeButton.SetActive(true);
    }

    public void OnComplete()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null) return;

        // 清潔度 +40（上限100）
        save.clean = Mathf.Clamp(save.clean + 40f, 0f, 100f);

        // お風呂カウント（日付をまたいだらリセット）
        ResetBathCountIfNewDay(save);
        save.bathCountToday++;
        save.lastBathDate = System.DateTime.Now.ToString("yyyy-MM-dd");

        // シャンプーに応じた性格パラ変化
        ApplyPersonality(save);

        Debug.Log($"[OnComplete] clean={save.clean} bathCountToday={save.bathCountToday} lastBathDate={save.lastBathDate} shampooId={_shampooId} activity={save.personalityActivity} dependency={save.personalityDependency} diligence={save.personalityDiligence} honesty={save.personalityHonesty} sensitivity={save.personalitySensitivity}");
        SaveManager.Instance.Save();
        BathJustCompleted = true;
        SceneManager.LoadScene("Care");
    }

    // ── プライベートヘルパー ──────────────────────────────────────────────────

    private void ApplyPersonality(SaveData save)
    {
        switch (_shampooId)
        {
            case "ichigo":
                save.personalityDependency = Mathf.Clamp(save.personalityDependency + 2, -100, 100);
                break;
            case "hoshizora":
                save.personalityDiligence = Mathf.Clamp(save.personalityDiligence + 2, -100, 100);
                break;
            case "rainbow":
                int idx   = Random.Range(0, 5);
                int delta = Random.value > 0.5f ? 2 : -2;
                switch (idx)
                {
                    case 0: save.personalityActivity    = Mathf.Clamp(save.personalityActivity    + delta, -100, 100); break;
                    case 1: save.personalityDependency  = Mathf.Clamp(save.personalityDependency  + delta, -100, 100); break;
                    case 2: save.personalityDiligence   = Mathf.Clamp(save.personalityDiligence   + delta, -100, 100); break;
                    case 3: save.personalityHonesty     = Mathf.Clamp(save.personalityHonesty     + delta, -100, 100); break;
                    case 4: save.personalitySensitivity = Mathf.Clamp(save.personalitySensitivity + delta, -100, 100); break;
                }
                break;
        }
    }

    private void ResetBathCountIfNewDay(SaveData save)
    {
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (save.lastBathDate != today)
            save.bathCountToday = 0;
    }
}
