using System;
using System.Globalization;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    public class PetStatus
    {
        // ─── パラメーター ───────────────────────────
        public float Hunger { get; private set; } = 50f;
        public float Clean  { get; private set; } = 50f;
        public float Energy { get; private set; } = 50f;
        public float Mood   => Mathf.Clamp((Hunger + Clean + Energy) / 3f, 10f, 100f);
        public int   Trust  { get; private set; } = 0;

        // ─── 最終お世話時刻（放置日数の判定専用。減衰計算には使わない） ───
        public DateTime LastFedTime   { get; private set; } = DateTime.Now;
        public DateTime LastBathTime  { get; private set; } = DateTime.Now;
        public DateTime LastPlayTime  { get; private set; } = DateTime.Now;

        // ─── 減衰の会計専用の基準時刻 ────────────────
        /// <summary>ApplyTimeDecay() が減算した時点。ここからの経過時間だけを減らすので二重適用しない。</summary>
        public DateTime LastDecayAt { get; private set; } = DateTime.Now;

        // ─── SaveDataから初期値を読み込む ────────────
        public void LoadFromSave(SaveData save)
        {
            Hunger = save.hunger;
            Clean  = save.clean;
            Energy = save.energy;
            Trust  = save.trust;

            LastFedTime  = ParseOrNow(save.statusLastFedAt);
            LastBathTime = ParseOrNow(save.statusLastBathAt);
            LastPlayTime = ParseOrNow(save.statusLastPlayAt);
            LastDecayAt  = ParseOrNow(save.statusLastDecayAt);
        }

        // ─── SaveDataに書き込む ──────────────────────
        public void SaveToSave(SaveData save)
        {
            save.hunger = Hunger;
            save.clean  = Clean;
            save.energy = Energy;
            save.trust  = Trust;

            save.statusLastFedAt   = LastFedTime.ToString("o");
            save.statusLastBathAt  = LastBathTime.ToString("o");
            save.statusLastPlayAt  = LastPlayTime.ToString("o");
            save.statusLastDecayAt = LastDecayAt.ToString("o");
        }

        /// <summary>
        /// 保存文字列を DateTime に戻す。空文字・パース失敗は「今」を返す。
        /// 未記録のセーブを大昔と解釈すると更新直後に全パラが下限まで落ちるため、必ず経過ゼロ扱いにする。
        /// </summary>
        private static DateTime ParseOrNow(string s)
        {
            if (string.IsNullOrEmpty(s)) return DateTime.Now;

            if (DateTime.TryParse(s, null, DateTimeStyles.RoundtripKind, out DateTime dt))
                return dt;

            return DateTime.Now;
        }

        // ─── 加算メソッド ────────────────────────────
        public void AddHunger(float v) => Hunger = Mathf.Clamp(Hunger + v, 10f, 100f);
        public void AddClean(float v)  => Clean  = Mathf.Clamp(Clean  + v, 10f, 100f);
        public void AddEnergy(float v) => Energy = Mathf.Clamp(Energy + v, 10f, 100f);
        public void AddTrust(int v)    => Trust  = Mathf.Max(0, Trust + v);

        // ─── お世話時刻の更新 ────────────────────────
        public void OnFed()    => LastFedTime  = DateTime.Now;
        public void OnBath()   => LastBathTime = DateTime.Now;
        public void OnPlayed() => LastPlayTime = DateTime.Now;

        // ─── 時間経過ペナルティ ──────────────────────
        /// <summary>
        /// LastDecayAt からの経過時間ぶんだけ減衰させ、基準時刻を now へ進める。
        /// 連続で呼んでも2回目以降の経過はほぼゼロになるため、二重適用しない。
        /// </summary>
        public void ApplyTimeDecay()
        {
            DateTime now = DateTime.Now;

            float hours = (float)(now - LastDecayAt).TotalHours;

            // 端末時計が巻き戻った場合など。増やさないよう 0 に丸める。
            if (hours < 0f) hours = 0f;

            AddHunger(-5f * hours);
            AddClean( -4f * hours);
            AddEnergy(-4f * hours);

            // 減算した直後に必ず基準時刻を進める
            LastDecayAt = now;

            Debug.Log($"[PetStatus] TimeDecay適用 経過:{hours:F2}h Hunger:{Hunger} Clean:{Clean} Energy:{Energy} Mood:{Mood}");
        }
    }
}
