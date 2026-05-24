#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DevTools
{
    [MenuItem("Dev/Clear PlayerPrefs (Reset Save Data)")]
    public static void ClearPlayerPrefs()
    {
        if (EditorUtility.DisplayDialog(
            "セーブデータをリセット",
            "PlayerPrefs を全削除します。次回プレイ時に初期状態で起動します。よろしいですか？",
            "リセット", "キャンセル"))
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[DevTools] PlayerPrefs をクリアしました。");
        }
    }
}
#endif
