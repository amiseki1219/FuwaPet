# Current Focus

## キャラクターアニメーション基盤

### ブランチ

`feat/character-animation-foundation`

### 現在作業中の内容

Main と Care で Poko の既存動作を維持しながら、Koko／Piyoko へ Idle／Walk アニメーションを段階的に対応する共通基盤を準備している。

### 完了済みの準備

- Koko／Piyoko のアニメーション対応 FBX を Unity へ取り込み
- アニメーション対応モデルの命名を `〇〇_Character_v02.fbx` に統一
- 静止表示用モデルの命名を `〇〇_Character_v01.fbx` に統一
- Koko／Piyoko とも Generic Rig
- Avatar Definition は Create From This Model
- Optimize Game Objects は OFF
- Apply Root Motion は OFF
- Idle／Happy／Eat／Walk の4クリップを設定
- Idle／Walk は Loop Time・Loop Pose ON
- Happy／Eat は Loop Time OFF
- Koko／Piyoko とも不要 Source Take なし
- Koko／Piyoko 関連の新規 Import Error なし

### 現在の問題

- `Koko_Static.prefab`／`Piyoko_Static.prefab` は旧 v01 モデルを参照している
- Poko 以外は `CharacterDisplayAnchor` へ実行時生成される
- `CharacterDisplayAnchor` と `PokoWalkRoot` は兄弟関係にある
- `PetoWalk` は `PokoWalkRoot` を移動する
- Poko 以外を選択すると、`legacyPokoDisplayRoot` として `PokoWalkRoot` 全体が非アクティブになり、`PetoWalk` も停止する
- 実行時生成キャラは事前に Inspector 結線できない

### 確定した設計

- キャラ別 C# スクリプトは作らない
- 共通 `CharacterAnimationController` を追加する
- キャラごとに Animator Controller を用意する
- 共通 Parameter 名は `IsWalking`
- `PetoWalk` は `PokoWalkRoot` に残す
- `PokoWalkRoot` 自体は非アクティブにしない
- Poko 以外では `PokoVisualRoot` だけを非表示にする
- `CharacterStaticDisplayController` が生成キャラを `PetoWalk` へ登録する
- 登録キャラがある場合、`PetoWalk` は登録された Transform を移動・回転する
- 登録キャラがない場合、現在の Poko 動作へフォールバックする
- キャラ固有の向き補正は `CharacterAnimationController` 側で保持する
- Poko の既存 Animator、FaceController、まばたき、Care Eat 処理を維持する
- Happy／Eat／Talk 対応は Idle／Walk 基盤と分離する
- 最初は Main で Piyoko の Idle／Walk を確認する
- Piyoko 確認後に Koko へ展開し、その後 Care へ対応する

### 最初の実装範囲

1. `CharacterAnimationController.cs` を新規追加
2. `PetoWalk.cs` へ実行時キャラ登録 API と Poko フォールバックを追加
3. `CharacterStaticDisplayController.cs` から生成キャラを登録
4. SerializeField は用意するだけ
5. Scene／Prefab／Inspector 結線はユーザーが手作業
6. Main で Piyoko の Idle／Walk を確認

### 今回触らないもの

- `CarePokoController`
- `FaceController`
- `PokoChan_Animator.controller`
- Happy／Eat／Talk
- Scene／Prefab／meta
- SaveData
- Resources パス
- PersistentManagers

### 次に行う作業

1. 最初の実装範囲で編集する C# と維持する Poko 動作を再確認
2. 共通 `CharacterAnimationController` を追加
3. `PetoWalk` に実行時登録 API と互換フォールバックを追加
4. `CharacterStaticDisplayController` から生成した Piyoko を登録
5. ユーザーが Main の Prefab／Animator／Inspector を結線
6. Main で Piyoko の Idle／Walk と Poko の既存動作を手動確認

