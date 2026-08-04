using UnityEngine;

namespace Game.Core
{
    public class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }
        public PetStatus PetStatus { get; private set; }

        /// <summary>
        /// アプリがバックグラウンドから復帰したときに発火する。
        /// 意味は「アプリが復帰した」であって「ステータスが更新された」ではない。
        /// 受け取った側が、自分の画面で何をやり直すかを自分で決める。
        /// </summary>
        public static event System.Action OnAppResumed;

        // 現在アプリが動いている状態か。OnApplicationPause と OnApplicationFocus の
        // 二重実行を防ぐためのフラグ。状態が変わらない呼び出しは何もしない。
        private bool _isRunning = true;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PetStatus = new PetStatus();

            // SaveManager.Awake()より後に実行されるよう Script Execution Order で保証すること
            if (SaveManager.Instance?.Data != null)
                PetStatus.LoadFromSave(SaveManager.Instance.Data);
        }

        // お世話後などに呼ぶ保存メソッド
        public void SavePetStatus()
        {
            if (SaveManager.Instance?.Data == null) return;
            PetStatus.SaveToSave(SaveManager.Instance.Data);
            SaveManager.Instance.Save();
        }

        // ── アプリの中断・復帰 ────────────────────────────────────────────
        // iOS では OnApplicationQuit が呼ばれないことがあるため、保存は中断側に置く。
        // OnApplicationQuit は追加しない。

        private void OnApplicationPause(bool pause)
        {
            SetRunning(!pause);
        }

        // エディタでは OnApplicationPause の呼ばれ方が環境によって異なるため併用する。
        // #if UNITY_EDITOR で囲まず実機でも同じ経路を通すが、_isRunning フラグにより
        // OnApplicationPause と二重には走らない。
        private void OnApplicationFocus(bool hasFocus)
        {
            SetRunning(hasFocus);
        }

        /// <summary>
        /// 中断・復帰の実処理。状態が変わらない呼び出しは何もしないため、
        /// OnApplicationPause と OnApplicationFocus の両方から呼ばれても二重に走らない。
        /// </summary>
        private void SetRunning(bool running)
        {
            // Awake で Destroy した重複インスタンスにも、破棄が確定するまでのあいだ
            // Unity は OnApplicationPause / OnApplicationFocus を送る。重複側は Awake を
            // 途中で return するため PetStatus が null で、触ると例外で落ちる。
            // Home と Care の両方に GameContext があるので、Care へ遷移するたびに発生しうる。
            if (Instance != this) return;

            if (_isRunning == running) return;
            _isRunning = running;

            if (!running)
            {
                // 中断時は保存だけ。
                SavePetStatus();
                return;
            }

            // 復帰時の順序は要件。入れ替えないこと。
            // 減衰より先に通知すると各画面が古い値で描画し、保存より先に通知すると
            // SaveData 経由で表示している信頼度が保存前の値を読んでずれる。
            // なお Android では起動直後に OnApplicationPause(false) が呼ばれることがあるが、
            // ApplyTimeDecay() は LastDecayAt 基準のため二重に呼んでも減算は増えない。
            PetStatus.ApplyTimeDecay();
            SavePetStatus();
            OnAppResumed?.Invoke();
        }
    }
}