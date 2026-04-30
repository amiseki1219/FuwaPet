using UnityEngine;

public class StoryPanelManager : MonoBehaviour
{
    [SerializeField] private GameObject firstStory;
    [SerializeField] private GameObject nameInput;
    [SerializeField] private GameObject iconSelectPanel;
    [SerializeField] private GameObject finalStory;

    private int subStep = 0;
    private GameObject[] panels;

    private void OnEnable()
    {
        panels = new[] { firstStory, nameInput, iconSelectPanel, finalStory };
        subStep = 0;
        UpdateSubView();
    }

    public void NextSub()
    {
        Debug.Log($"<color=yellow>【StoryPanelManager】NextSub() 呼ばれた！ subStep={subStep}</color>");
        if (subStep < panels.Length - 1)
        {
            subStep++;
            UpdateSubView();
        }
        else
        {
            Debug.Log("<color=cyan>【StoryPanelManager】最終サブステップ → PlayDoorAnimation() 呼ぶ</color>");
            FindAnyObjectByType<OnboardingManager>()?.PlayDoorAnimation();
        }
    }

    public void GoBackSub()
    {
        if (subStep > 0)
        {
            subStep--;
            UpdateSubView();
        }
    }

    private void UpdateSubView()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null) panels[i].SetActive(i == subStep);
        }
    }
}
