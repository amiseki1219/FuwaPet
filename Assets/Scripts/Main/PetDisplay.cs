using UnityEngine;
using UnityEngine.UI; // これが必要！

public class PetDisplay : MonoBehaviour
{
    [SerializeField] private Sprite[] petSprites;
    private Image petImage; // SpriteRenderer から Image に変更

    void Awake()
    {
        // UIの Image コンポーネントを探すように変更
        petImage = GetComponent<Image>();
    }

    void Start()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;

        string charId = SaveManager.Instance.Data.selectedCharacterId;
        Debug.Log("読み込んだキャラID: " + charId);

        int index = GetIndexById(charId);

        if (petImage != null && index >= 0 && index < petSprites.Length)
        {
            // Image の画像を差し替える！
            petImage.sprite = petSprites[index];
            Debug.Log(charId + " の画像を表示したよ！");
        }
    }

    private int GetIndexById(string id)
    {
        switch (id)
        {
            case "Dog": return 0;
            case "Cat": return 1;
            case "WhiteDog": return 2;
            case "WhiteCat": return 3;
            case "Rabit": return 4;
            case "WhiteBear": return 5;
            default:
                return 0;

        }
    }
}