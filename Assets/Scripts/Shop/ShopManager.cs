using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("メインのメニューパネル")]
    [SerializeField] private GameObject menuPanel;

    [Header("表示・非表示を切り替えるパネルたち")]
    [SerializeField] private GameObject oyatuPanel;
    [SerializeField] private GameObject ticketPanel;
    [SerializeField] private GameObject myDecoPanel;
    [SerializeField] private GameObject homeDecoPanel;
    [SerializeField] private GameObject petClothPanel;

    // --- 各ボタンから呼ばれる関数 ---

    public void OpenOyatu() => ShowPanel(oyatuPanel);
    public void OpenTicket() => ShowPanel(ticketPanel);
    public void OpenMyDeco() => ShowPanel(myDecoPanel);
    public void OpenHomeDeco() => ShowPanel(homeDecoPanel);
    public void OpenPetCloth() => ShowPanel(petClothPanel);


    // すべてのパネルを閉じる（ReturnButton用）
    public void CloseAllPanels()
    {
        oyatuPanel.SetActive(false);
        ticketPanel.SetActive(false);
        myDecoPanel.SetActive(false);
        homeDecoPanel.SetActive(false);
        petClothPanel.SetActive(false);

        menuPanel.SetActive(true);
    }

    // 指定したパネルを出す時：メニューを隠す！
    private void ShowPanel(GameObject targetPanel)
    {
        // 他のパネルを全部閉じる処理
        oyatuPanel.SetActive(false);
        ticketPanel.SetActive(false);
        myDecoPanel.SetActive(false);
        homeDecoPanel.SetActive(false);
        petClothPanel.SetActive(false);

        menuPanel.SetActive(false); // ★メニューを隠すお！
        targetPanel.SetActive(true); // 呼びたいパネルだけ出す！
    }
}