using System;
using UnityEngine;

public class CharacterStaticDisplayController : MonoBehaviour
{
    private const string PokoId = "poko";

    [Header("既存Poko表示")]
    [SerializeField] private GameObject legacyPokoDisplayRoot;

    [Header("Static Character表示")]
    [SerializeField] private Transform characterDisplayAnchor;
    [SerializeField] private GameObject eruStaticPrefab;
    [SerializeField] private GameObject kokoStaticPrefab;
    [SerializeField] private GameObject paruStaticPrefab;
    [SerializeField] private GameObject piyokoStaticPrefab;

    private GameObject spawnedCharacter;

    private void Awake()
    {
        string characterId = ResolveCharacterId();

        if (characterId == PokoId)
        {
            ShowLegacyPoko("pokoを表示します");
            return;
        }

        GameObject staticPrefab = GetStaticPrefab(characterId);
        if (!CanShowStaticCharacter(characterId, staticPrefab))
        {
            ShowLegacyPoko($"{characterId}の表示準備が不足しているためpokoへフォールバックします");
            return;
        }

        try
        {
            spawnedCharacter = Instantiate(staticPrefab, characterDisplayAnchor, false);
            spawnedCharacter.transform.localPosition = Vector3.zero;
            spawnedCharacter.transform.localRotation = Quaternion.identity;
            spawnedCharacter.transform.localScale = Vector3.one;

            legacyPokoDisplayRoot.SetActive(false);
            Debug.Log($"<color=#00E5FF>[決定]</color> [CharacterDisplay] characterId={characterId} Static Prefabを表示しました");
        }
        catch (Exception exception)
        {
            if (spawnedCharacter != null)
            {
                Destroy(spawnedCharacter);
                spawnedCharacter = null;
            }

            Debug.LogWarning($"[CharacterDisplay] characterId={characterId} の生成に失敗しました。pokoへフォールバックします。理由: {exception.Message}");
            ShowLegacyPoko("Static Prefabの生成失敗");
        }
    }

    private string ResolveCharacterId()
    {
        SaveData data = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
        if (data == null)
        {
            Debug.LogWarning("[CharacterDisplay] SaveDataを取得できないためpokoへフォールバックします");
            return PokoId;
        }

        string rawId = !string.IsNullOrWhiteSpace(data.selectedCharacterId)
            ? data.selectedCharacterId
            : data.characterId;

        if (string.IsNullOrWhiteSpace(rawId))
        {
            Debug.LogWarning("[CharacterDisplay] キャラクターIDが空のためpokoへフォールバックします");
            return PokoId;
        }

        string normalizedId = rawId.Trim().ToLowerInvariant();
        switch (normalizedId)
        {
            case PokoId:
            case "eru":
            case "koko":
            case "paru":
            case "piyoko":
                return normalizedId;
            default:
                Debug.LogWarning($"[CharacterDisplay] 未知のキャラクターID '{rawId}' のためpokoへフォールバックします");
                return PokoId;
        }
    }

    private GameObject GetStaticPrefab(string characterId)
    {
        switch (characterId)
        {
            case "eru":
                return eruStaticPrefab;
            case "koko":
                return kokoStaticPrefab;
            case "paru":
                return paruStaticPrefab;
            case "piyoko":
                return piyokoStaticPrefab;
            default:
                return null;
        }
    }

    private bool CanShowStaticCharacter(string characterId, GameObject staticPrefab)
    {
        if (legacyPokoDisplayRoot == null)
        {
            Debug.LogWarning($"[CharacterDisplay] characterId={characterId}: legacyPokoDisplayRootが未結線です");
            return false;
        }

        if (characterDisplayAnchor == null)
        {
            Debug.LogWarning($"[CharacterDisplay] characterId={characterId}: characterDisplayAnchorが未結線です");
            return false;
        }

        if (staticPrefab == null)
        {
            Debug.LogWarning($"[CharacterDisplay] characterId={characterId}: 対応するStatic Prefabが未結線です");
            return false;
        }

        if (characterDisplayAnchor.IsChildOf(legacyPokoDisplayRoot.transform))
        {
            Debug.LogWarning($"[CharacterDisplay] characterId={characterId}: characterDisplayAnchorがlegacyPokoDisplayRoot配下にあるため表示できません");
            return false;
        }

        return true;
    }

    private void ShowLegacyPoko(string reason)
    {
        if (legacyPokoDisplayRoot == null)
        {
            Debug.LogWarning($"[CharacterDisplay] legacyPokoDisplayRootが未結線のためPoko表示を有効化できません。理由: {reason}");
            return;
        }

        legacyPokoDisplayRoot.SetActive(true);
        Debug.Log($"<color=#00E5FF>[決定]</color> [CharacterDisplay] characterId=poko 既存Poko表示を使用します。理由: {reason}");
    }
}
