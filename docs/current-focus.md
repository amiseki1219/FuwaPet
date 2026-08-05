# Current Focus

## キャラクター表情システム

### ブランチ

`feat/character-face-expression`

### 現在の状況

える（Eru）で表情システムを一通り通し、Main での表示まで確認できた段階。

### 完了したこと

- 表情キー9種を確定（Normal / Happy / Sad / Angry / Shy / Fun / Surprised / Close / Relaxed）
- 4キャラ（える・ぱる・ここ・ぴよこ）の顔テクスチャを Unity へ取り込み
- キャラ別の目・口の割り当てを確定
- `CharacterFaceController.cs` / `CharacterBlinkController.cs` を新規追加
- える の `FaceExpressionDatabase.asset` を作成し9表情を登録
- `Eru_Animated.prefab` へ表情コンポーネントを結線
- 顔パーツ用マテリアル `Mat_Eru_Face` を作成（Transparent）
- える の顔パーツが頭から浮いていた問題を Blender 側で修正し、FBX を書き出し直した
- ステータスの時間経過を修正（最終お世話時刻の永続化・減衰の二重適用の解消）

### 確認済みの動作

- Main で える の目・口が正しく表示される
- まばたきが動作する
- 横から見て顔パーツが浮いていない
- FBX 再書き出し後もアニメクリップ4本が健在

### 未確認・未実装

- 表情の切り替え動作（9表情の目視確認をしていない）
- 状態に応じた表情変化（Home から再生しないと `GameContext` が無く必ず Normal になる）
- ぱる・ここ・ぴよこ の結線
- Poko の `FaceController` からの移行

### 次に行う作業

1. Unity でコンパイルエラーが無いことを確認する
2. コミットを2つに分ける（表情システム / ステータス時間経過）
3. 9表情を目視確認する手段を用意する
4. ぱる・ここ・ぴよこ へ同じ手順を展開する
5. Care の参照を `*_Animated.prefab` へ変え、お世話後に `TriggerCareAction()` を呼ぶ
6. Bath へ Relaxed を出す処理を追加する

### 今回触らないもの

- `FaceController.cs` / `PokoFaceController.cs` / `PokoBlinkController.cs`（Poko が使用中）
- `CarePokoController.cs`
- Chat（会話AIが未実装のため）
- ぴよこのくちばし角度

### 判明している既知の不具合

CLAUDE.md の「未完了タスク」を参照。特に影響が大きいもの。

- Care / Bath が v01 モデル（顔パーツなし）を参照している
- `BathWashManager` が `save.clean` を直接書き換えている
- `OnApplicationPause` 未実装のためバックグラウンド復帰で状態が更新されない
- える の顔が黒すぎて見えにくい（絵の修正が必要）
