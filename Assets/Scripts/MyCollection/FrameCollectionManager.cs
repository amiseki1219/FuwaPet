using UnityEngine;
using UnityEngine.UI;

public class FrameCollectionManager : MonoBehaviour
{
    public static FrameCollectionManager Instance;

    [Header("アイコンの上に重ねるフレーム用画像")]
    public RawImage MyFrameImage;

    [Header("14個のフレーム枠たち")]
    public CollectionFrameItem[] allFrameItems;

    private string tempSelectedFrameId;

    private void Awake() => Instance = this;

    void Start()
    {
        RefreshAllFrames();
        ShowCurrentFrames();
    }

    public void ShowCurrentFrames()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("SaveManagerが見つからないお！ヒエラルキーにいるかな？");
            return;
        }

        // SaveDataからIDを取得
        tempSelectedFrameId = SaveManager.Instance.Data.selectedFrameId;

        // ★もし空っぽなら「DefaultFrame」を入れるガードを追加しておいたお
        if (string.IsNullOrEmpty(tempSelectedFrameId))
        {
            tempSelectedFrameId = "DefaultFrame";
        }

        UpdateFramePreview(tempSelectedFrameId);
        UpdateSelectionVisuals();
    }

    public void OnSelectFrame(string id)
    {
        // IDに余計なスペースが入っていないかチェック！
        tempSelectedFrameId = id.Trim();
        Debug.Log($"<color=cyan>フレームクリック！ 選んだID: [{tempSelectedFrameId}]</color>");

        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        int matchCount = 0;
        foreach (var item in allFrameItems)
        {
            if (item == null) continue;

            // IDを比較する（大文字小文字やスペースを無視！）
            bool isTarget = string.Equals(item.myFrameId.Trim(), tempSelectedFrameId, System.StringComparison.OrdinalIgnoreCase);
            item.SetSelected(isTarget);

            if (isTarget) matchCount++;
        }
        Debug.Log($"枠の更新完了！ 一致したフレーム数: {matchCount}");
    }

    public void OnClickConfirm()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("決定ボタン：SaveManagerがないから保存できないお🥵");
            return;
        }

        // データの更新（selectedFrameId に保存）
        SaveManager.Instance.Data.selectedFrameId = tempSelectedFrameId;
        SaveManager.Instance.Save();

        // 上の画像を更新
        UpdateFramePreview(tempSelectedFrameId);
        Debug.Log($"<color=yellow>決定！ 保存されたフレームID: {tempSelectedFrameId}</color>");
    }

    private void UpdateFramePreview(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        Texture tex = LoadFrameTexture(id);
        if (tex != null) MyFrameImage.texture = tex;
    }

    private Texture LoadFrameTexture(string id)
    {
        // フォルダ名は SpecialFrameUI（または FrameUI）に合わせてね！
        Texture tex = Resources.Load<Texture>("SpecialFrameUI/" + id);



        if (tex == null) Debug.LogWarning($"フレーム画像が見つからないお: {id}");
        return tex;
    }

    public void RefreshAllFrames()
    {
        if (SaveManager.Instance == null) return;

        // SaveDataの ownedFrames（持っているリスト）を使う
        var ownedList = SaveManager.Instance.Data.ownedFrames;

        foreach (var item in allFrameItems)
        {
            if (item == null) continue;
            bool hasIt = ownedList.Contains(item.myFrameId);
            item.Setup(hasIt);
        }
    }
}