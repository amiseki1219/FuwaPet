using UnityEngine;

public class MainUIManager : MonoBehaviour
{
    [Header("ぽよんメニューの参照")]
    [SerializeField] private MenuPoyonController careMenu;    // ★Care復活！
    [SerializeField] private MenuPoyonController shopMenu;
    [SerializeField] private MenuPoyonController settingMenu;

    // 今どれが開いているか（-1は何もない状態）
    private int currentOpenIndex = -1;

    public void OnMainButtonClicked(int index)
    {
        // 1（Chat）はLoadingManagerにお任せ！
        if (index == 1) return;

        // 1. もし今開いているボタンを「もう一度」押したら、全部閉じておしまい
        if (currentOpenIndex == index)
        {
            CloseAllSubMenus();
            currentOpenIndex = -1; // 状態をリセット
            return;
        }

        // 2. 違うボタンが押されたら、まずは一旦全部閉じる
        CloseAllSubMenus();

        // 3. 押された番号に応じて「開く」
        switch (index)
        {
            case 0: // Care（復活！）
                if (careMenu != null) careMenu.OpenMenu();
                break;
            case 2: // Shop
                if (shopMenu != null) shopMenu.OpenMenu();
                break;
            case 3: // Setting
                if (settingMenu != null) settingMenu.OpenMenu();
                break;
        }

        // 4. 今開いた番号を記録する
        currentOpenIndex = index;
    }

    private void CloseAllSubMenus()
    {
        if (careMenu != null) careMenu.CloseMenu();
        if (shopMenu != null) shopMenu.CloseMenu();
        if (settingMenu != null) settingMenu.CloseMenu();
    }
}