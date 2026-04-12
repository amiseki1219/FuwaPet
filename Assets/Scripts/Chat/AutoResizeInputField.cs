using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AutoResizeInputField : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private RectTransform inputFieldRect;
    [SerializeField] private RectTransform imageRect; // 追加

    private float minHeight = 130f;
    private float maxHeight = 180f;

    void Start()
    {
        inputField.onValueChanged.AddListener(OnTextChanged);
    }

    void OnTextChanged(string text)
    {
        float textHeight = inputField.textComponent.preferredHeight;
        float newHeight = Mathf.Clamp(textHeight + 65f, minHeight, maxHeight);

        // InputFieldとImageを同時に伸ばす
        inputFieldRect.sizeDelta = new Vector2(inputFieldRect.sizeDelta.x, newHeight);
        imageRect.sizeDelta = new Vector2(imageRect.sizeDelta.x, newHeight);
    }
}

