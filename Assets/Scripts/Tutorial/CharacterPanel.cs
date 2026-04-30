using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic; // Listを使うのに必要だお
using DG.Tweening;
using Game.Core;

public class CharacterPanelLite : MonoBehaviour
{
    [Serializable]
    public class CharacterSetting
    {
        public string characterId;
        public GameObject detailPanel;
        public RectTransform animateTarget;
        [HideInInspector] public Vector2 originPos; // 元の位置をメモするお
    }

    [Header("4人のキャラクター設定")]
    [SerializeField] private List<CharacterSetting> characterSettings = new List<CharacterSetting>();

    [Header("アニメーション演出の設定")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float jumpOffset = -500f;
    [SerializeField] private Ease moveEase = Ease.OutBack;

    private int currentIndex = 0;

    private void Awake()
    {
        // 起動時に、あみまるさんが置いた「正しい位置」をメモ！
        foreach (var setting in characterSettings)
        {
            if (setting.animateTarget != null)
            {
                setting.originPos = setting.animateTarget.anchoredPosition;
            }
        }
    }

    private void Start()
    {
        foreach (var setting in characterSettings)
        {
            if (setting.detailPanel != null) setting.detailPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        Debug.Log("<color=cyan>【CharacterPanelLite】OnEnable() 呼ばれた</color>");
        OnStartSelection();
    }

    public void OnStartSelection()
    {
        Debug.Log("<color=cyan>【CharacterPanelLite】OnStartSelection() 呼ばれた index=0</color>");
        currentIndex = 0;
        UpdateDetailDisplay(true);
    }

    public void OnClickNextArrow() { ChangeIndex(1); }
    public void OnClickPrevArrow() { ChangeIndex(-1); }

    private void ChangeIndex(int step)
    {
        // ★ここを .Count に修正したお！
        currentIndex = (currentIndex + step + characterSettings.Count) % characterSettings.Count;
        UpdateDetailDisplay(true);
    }

    private void UpdateDetailDisplay(bool useAnimation)
    {
        for (int i = 0; i < characterSettings.Count; i++)
        {
            var setting = characterSettings[i];
            if (setting.detailPanel == null)
            {
                Debug.Log($"<color=red>【CharacterPanelLite】index={i} detailPanel が null</color>");
                continue;
            }
            bool isActive = (i == currentIndex);
            Debug.Log($"<color=yellow>【CharacterPanelLite】index={i} id={setting.characterId} isActive={isActive}</color>");

            if (i == currentIndex)
            {
                setting.detailPanel.SetActive(true);
                Debug.Log($"<color=lime>【CharacterPanelLite】SetActive({true}) 呼んだ直後 → activeSelf={setting.detailPanel.activeSelf} activeInHierarchy={setting.detailPanel.activeInHierarchy}</color>");

                if (useAnimation && setting.animateTarget != null)
                {
                    CanvasGroup cg = setting.animateTarget.GetComponent<CanvasGroup>();
                    if (cg == null) cg = setting.animateTarget.gameObject.AddComponent<CanvasGroup>();

                    cg.DOKill();
                    cg.alpha = 0f;

                    setting.animateTarget.DOKill();
                    // 記憶した元の位置からオフセット分下げてスタート
                    setting.animateTarget.anchoredPosition = setting.originPos + new Vector2(0, jumpOffset);

                    // 記憶した「正しい位置」へ帰るお！
                    setting.animateTarget.DOAnchorPos(setting.originPos, duration).SetEase(moveEase);
                    cg.DOFade(1f, duration).SetEase(Ease.InCubic);
                }
            }
            else
            {
                setting.detailPanel.SetActive(false);
                Debug.Log($"<color=lime>【CharacterPanelLite】SetActive({false}) 呼んだ直後 → activeSelf={setting.detailPanel.activeSelf} activeInHierarchy={setting.detailPanel.activeInHierarchy}</color>");
            }
        }
    }

    public void OnFinalDecide()
    {
        if (SaveManager.Instance != null)
        {
            string finalId = characterSettings[currentIndex].characterId;
            SaveManager.Instance.Data.selectedCharacterId = finalId;
            SaveManager.Instance.Data.iconId = finalId;
            SaveManager.Instance.Save();
        }

        FindAnyObjectByType<OnboardingManager>()?.Next();
    }
}