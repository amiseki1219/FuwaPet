using UnityEngine;
using UnityEngine.UI;

public class MyCollectionManager : MonoBehaviour
{
    public static MyCollectionManager Instance;

    public RawImage MyPrfileImage;

    public CollectionIconItem[] allIconItems;

    private string tempSelectedId;

    private void Awake() => Instance = this;

    void Start()
    {
        RefreshAllIcons();
        ShowCurrentIcons();
    }

    public void ShowCurrentIcons()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManagerが見つからないお！ヒエラルキーにいるかな？");
            return;
        }

        tempSelectedId = SaveManager.Instance.Data.iconId;
        UpdatePreview(tempSelectedId);
        UpdateSelectionVisuals();

    }

    public void OnSelectIcon(string id)
    {
        // IDに余計なスペースが入っていないかチェック！
        tempSelectedId = id.Trim();
        Debug.Log($"<color=cyan>アイコンクリック！ 選んだID: [{tempSelectedId}]</color>");

        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        int matchCount = 0;
        foreach (var item in allIconItems)
        {
            if (item == null) continue;

            // IDを比較する（大文字小文字やスペースを無視するように強化！）
            bool isTarget = string.Equals(item.myIconId.Trim(), tempSelectedId, System.StringComparison.OrdinalIgnoreCase);
            item.SetSelected(isTarget);

            if (isTarget) matchCount++;
        }
        Debug.Log($"枠の更新完了！ 一致したアイコン数: {matchCount}");
    }

    public void OnClickConfirm()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("決定ボタン：SaveManagerがないから保存できないお🥵");
            return;
        }

        // データの更新
        SaveManager.Instance.Data.iconId = tempSelectedId;
        SaveManager.Instance.Data.profileImagePath = tempSelectedId;
        SaveManager.Instance.Save();

        // 上の画像を更新
        UpdatePreview(tempSelectedId);
        Debug.Log($"<color=yellow>決定！ 保存されたID: {tempSelectedId}</color>");
    }

    private void UpdatePreview(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        Texture tex = LoadIconTexture(id);
        if (tex != null) MyPrfileImage.texture = tex;
    }

    private Texture LoadIconTexture(string id)
    {
        Texture tex = Resources.Load<Texture>("SpecialIcon/" + id);
        if (tex == null) tex = Resources.Load<Texture>("Icon/" + id);

        if (tex == null) Debug.LogWarning($"画像が見つからないお: {id}");
        return tex;
    }

    public void RefreshAllIcons()
    {
        if (SaveManager.Instance == null) return;
        var ownedList = SaveManager.Instance.Data.ownedIcons;
        foreach (var item in allIconItems)
        {
            if (item == null) continue;
            bool hasIt = ownedList.Contains(item.myIconId);
            item.Setup(hasIt);
        }
    }
}