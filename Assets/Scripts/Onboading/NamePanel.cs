using UnityEngine;
using UnityEngine.UI;
using System;

public class NamePanel : MonoBehaviour
{
    [Serializable]
    public class CharacterView
    {
        public string id;      // "Dog" とか
        public Sprite sprite;  // そのキャラ画像
    }

    [SerializeField] private Image characterImage;
    [SerializeField] private CharacterView[] characters;

    private void OnEnable()
    {
        if (characterImage == null)
        {
            Debug.LogError("NamePanel: characterImage が null です");
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManager がまだ準備できていません");
            return;
        }

        if (SaveManager.Instance.Data == null)
        {
            Debug.LogError("SaveManager.Data が null です");
            return;
        }

        var id = SaveManager.Instance.Data.selectedCharacterId;

        // IDに一致するSpriteを探す
        foreach (var c in characters)
        {
            if (c != null && c.id == id)
            {
                if (c.sprite != null)
                {
                    characterImage.sprite = c.sprite;
                    characterImage.enabled = true;
                }
                else
                {
                    Debug.LogWarning($"NamePanel: {id} のスプライトが null です");
                    characterImage.enabled = false;
                }
                return;
            }
        }

        // 見つからない時は非表示
        Debug.LogWarning($"NamePanel: ID '{id}' のキャラクターが見つかりません");
        characterImage.enabled = false;
    }
}