using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Core;

public class OwnerPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField birthdayInput;
    [SerializeField] private Button nextButton;

    private void Awake()
    {
        if (nameInput == null) Debug.LogError("nameInputがセットされてないよ！");
        if (SaveManager.Instance == null) Debug.LogError("SaveManagerがまだ準備できてないよ！");

        var data = SaveManager.Instance.Data;

        nameInput.text = data.ownerName ?? "";
        birthdayInput.text = data.ownerBirthday ?? "";

        nextButton.interactable = false;

        nameInput.onValueChanged.AddListener(_ => Validate());
        birthdayInput.onValueChanged.AddListener(_ => Validate());

        Validate(); // ← 最初から判定
    }



    void Validate()
    {
        bool isValid =
            !string.IsNullOrWhiteSpace(nameInput.text) &&
            !string.IsNullOrWhiteSpace(birthdayInput.text);

        nextButton.interactable = isValid;
    }

    // NextボタンのOnClickに設定
    public void SaveOwnerInfo()
    {
        var data = SaveManager.Instance.Data;

        data.ownerName = nameInput.text;
        data.ownerBirthday = birthdayInput.text;

        // ここでは Save() しない！
        // 最後に OnboardingManager がまとめて Save する
    }
    [SerializeField] private OnboardingManager onboarding;

    public void OnClickNext()
    {
        SaveOwnerInfo();
        onboarding.Next();
    }
}
