using UnityEngine;
using TMPro;

public class NameDisplay : MonoBehaviour
{
    // インスペクターでチェックを入れられるようにするよ
    [SerializeField] private bool isOwnerName;

    void Start()
    {
        var textMesh = GetComponent<TMP_Text>();

        // SaveManagerがあるかチェック
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null)
        {
            Debug.LogWarning("セーブデータが見つからないよ！");
            return;
        }

        var data = SaveManager.Instance.Data;

        // チェックが入っていれば「飼い主名」、入っていなければ「ペット名」を表示
        if (isOwnerName)
        {
            textMesh.text = data.ownerName;
        }
        else
        {
            textMesh.text = data.petName;
        }
    }
}