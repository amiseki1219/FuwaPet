using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Core;

public class NamePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField petNameInput;
    [SerializeField] private Button nextButton;

    private void Start()
    {
        nextButton.interactable = false;

        petNameInput.onValueChanged.AddListener(_ => Validate());

        // 既に保存されてたら復元（任意だけど便利）
        var saved = SaveManager.Instance.Data.petName;
        if (!string.IsNullOrEmpty(saved))
        {
            petNameInput.text = saved;
            Validate();
        }
    }

    void Validate()
    {
        nextButton.interactable = !string.IsNullOrWhiteSpace(petNameInput.text);
    }

    // NextボタンのOnClickに設定
    public void SavePetName()
    {
        SaveManager.Instance.Data.petName = petNameInput.text.Trim();
    }
}
