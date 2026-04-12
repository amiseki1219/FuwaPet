using UnityEngine;

public class CollectionTabManager : MonoBehaviour
{
    [System.Serializable]
    public struct TabPair
    {
        public RectTransform panel;
        public CanvasGroup tabGroup; // ここをCanvasGroupに変えるお！
    }

    public TabPair[] tabs;

    void Start()
    {
        SwitchTab(0); // 最初はマイアイコンを選択
    }

    public void SwitchTab(int index)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            // 選んだタブはハッキリ(1.0)、選んでないタブは少し透かす(0.5)
            // これで「選んでない感」が出るお！
            tabs[i].tabGroup.alpha = (i == index) ? 1.0f : 0.5f;

            if (i == index)
            {
                tabs[i].panel.SetAsLastSibling();
            }
        }
    }
}