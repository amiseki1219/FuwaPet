using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OyatsuPuzzle
{
    // パズルの遊び方を表示する3ページ切り替え式 HelpPanel。
    // HelpButton.onClick → Show()（PersistentListener）。NextButton/CloseButton は Awake/Show で実行時ひもづけ。
    // 純粋な表示パネル。ゲーム進行・報酬・プレイ回数ロジックには一切関与しない。
    public class PuzzleHelpPanelUI : MonoBehaviour
    {
        [Header("Root")]
        [Tooltip("表示/非表示を切り替える対象。未設定ならこの GameObject 自身。")]
        [SerializeField] private GameObject panelRoot;

        [Header("Pages")]
        [Tooltip("ページ GameObject（Page1..3 の順）。SetActive で切り替える。")]
        [SerializeField] private GameObject[] pages;

        [Header("Page Indicator")]
        [SerializeField] private Image[]   pageDots;
        [SerializeField] private TMP_Text  pageNumberText;

        [Header("Buttons")]
        [SerializeField] private Button    nextButton;
        [SerializeField] private TMP_Text  nextButtonText;
        [SerializeField] private Button    closeButton;

        [Header("Dot Colors")]
        [SerializeField] private Color activeDotColor   = new Color(0.949f, 0.388f, 0.451f, 1f); // ピンク
        [SerializeField] private Color inactiveDotColor = new Color(0.901f, 0.843f, 0.784f, 1f); // 薄ベージュ

        private int  _currentPageIndex;
        private bool _wired;

        private GameObject Target => panelRoot != null ? panelRoot : gameObject;

        private void Awake()
        {
            WireButtons();
        }

        // NextButton/CloseButton を実行時にひもづける（初期非表示でも Show 時に確実に配線するため二重ガード）。
        private void WireButtons()
        {
            if (_wired) return;
            if (nextButton  != null) nextButton.onClick.AddListener(OnClickNext);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            _wired = true;
        }

        // HelpButton から呼ぶ。1ページ目から表示する。
        public void Show()
        {
            WireButtons();
            _currentPageIndex = 0;
            Target.SetActive(true);
            RefreshPage();
        }

        // CloseButton / 「わかった！」から呼ぶ。閉じる。
        public void Hide()
        {
            Target.SetActive(false);
        }

        // 「つぎへ」/「わかった！」。最終ページ未満なら次へ、最終ページなら閉じる。
        public void OnClickNext()
        {
            int count = pages != null ? pages.Length : 0;
            if (_currentPageIndex < count - 1)
            {
                _currentPageIndex++;
                RefreshPage();
            }
            else
            {
                Hide();
            }
        }

        // 現在ページに合わせて、ページ表示・ドット色・ページ番号・ボタン文言を更新する。
        private void RefreshPage()
        {
            int count = pages != null ? pages.Length : 0;

            for (int i = 0; i < count; i++)
                if (pages[i] != null) pages[i].SetActive(i == _currentPageIndex);

            if (pageDots != null)
                for (int i = 0; i < pageDots.Length; i++)
                    if (pageDots[i] != null)
                        pageDots[i].color = (i == _currentPageIndex) ? activeDotColor : inactiveDotColor;

            if (pageNumberText != null)
                pageNumberText.text = $"{_currentPageIndex + 1} / {count}";

            bool isLast = _currentPageIndex >= count - 1;
            if (nextButtonText != null)
                nextButtonText.text = isLast ? "わかった！" : "つぎへ";
        }
    }
}
