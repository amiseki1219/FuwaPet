using TMPro;
using UnityEngine;
using Game.Core; // これを追加！

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] TMP_Text coinText;

    void Update()
    {
        // もしテキスト枠が空っぽなら、エラーにならないようにここで止める
        if (coinText == null) return;

        // 【ここがポイント】
        // GameData.Instance がまだ準備できていない時に
        // 無理やり中身を見に行こうとしてエラーになっていたよ。
        // だから「準備ができるまで待つ」処理を入れたよ！
        if (GameData.Instance == null)
        {
            coinText.text = "---"; // 準備中はこう出す
            return;
        }

        // 準備ができたらラフ画通りに表示！
        coinText.text = $"{GameData.Instance.Coin} ";
    }
}