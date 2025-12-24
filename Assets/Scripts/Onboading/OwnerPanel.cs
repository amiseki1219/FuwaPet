using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Core;

public class OwnerPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField ownerNameInput;
    [SerializeField] private TMP_InputField birthdayInput;
    [SerializeField] private Button nextButton;

    private void Start()
    {
        var data = SaveManager.Instance.Data;

        ownerNameInput.text = data.ownerName ?? "";
        birthdayInput.text = data.ownerBirthday ?? "";

        nextButton.interactable = false;

        ownerNameInput.onValueChanged.AddListener(_ => Validate());
        birthdayInput.onValueChanged.AddListener(_ => Validate());

        Validate(); // ← 最初から判定
    }



    void Validate()
    {
        bool isValid =
            !string.IsNullOrWhiteSpace(ownerNameInput.text) &&
            !string.IsNullOrWhiteSpace(birthdayInput.text);

        nextButton.interactable = isValid;
    }

    // NextボタンのOnClickに設定
    public void SaveOwnerInfo()
    {
        var data = SaveManager.Instance.Data;

        data.ownerName = ownerNameInput.text;
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
