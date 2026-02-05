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

    [Header("表示パネルの設定")]
    [SerializeField] private GameObject previewListPanel;
    [SerializeField] private GameObject namePanel;

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

    public void OnStartSelection()
    {
        if (previewListPanel != null) previewListPanel.SetActive(false);
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
        // ★ここも .Count に修正だっぴ！
        for (int i = 0; i < characterSettings.Count; i++)
        {
            var setting = characterSettings[i];
            if (setting.detailPanel == null) continue;

            if (i == currentIndex)
            {
                setting.detailPanel.SetActive(true);

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
            }
        }
    }

    public void OnFinalDecide()
    {
        if (SaveManager.Instance != null)
        {
            string finalId = characterSettings[currentIndex].characterId;
            SaveManager.Instance.Data.selectedCharacterId = finalId;
            SaveManager.Instance.Save();
        }

        if (namePanel != null) namePanel.SetActive(true);
        this.gameObject.SetActive(false);
    }
}