# キャラクターアニメーション基盤の設計判断

## 概要

Main と Care で Poko の既存動作を維持しながら、Koko／Piyoko へ Idle／Walk を段階的に追加するための調査結果と設計判断を記録する。
この文書は実装前の開発メモであり、正式仕様ではない。

---

## 環境

| 項目 | 内容 |
|------|------|
| ブランチ | `feat/character-animation-foundation` |
| Unity | 6000.3.14f1 |
| 対象 Scene | Main、Care |
| 対象キャラ | Poko、Koko、Piyoko |
| 調査状態 | read-only 調査完了、実装・Play Mode 確認は未実施 |

---

## 調査で判明した既存構造

### Main

- `WalkSystem` 配下で `CharacterDisplaySystem`、`CharacterDisplayAnchor`、`PokoWalkRoot` が兄弟になっている
- `CharacterDisplaySystem` に `CharacterStaticDisplayController` がある
- `PokoWalkRoot` に `PetoWalk` がある
- `PokoWalkRoot` の子に `PokoVisualRoot` がある
- `PetoWalk` は自身の `transform`、つまり `PokoWalkRoot` の位置を移動する
- `PetoWalk` は `visualRoot`、つまり `PokoVisualRoot` を回転する
- Animator は `PetoWalk.Start()` の `GetComponentInChildren<Animator>()` で取得する
- Poko 以外は `CharacterStaticDisplayController.Awake()` で `CharacterDisplayAnchor` へ生成される
- 生成した GameObject は private な `spawnedCharacter` に保持されるが、外部公開されていない
- Poko 以外を生成した後、`legacyPokoDisplayRoot` として結線された `PokoWalkRoot` 全体を非アクティブにする
- `Koko_Static.prefab`／`Piyoko_Static.prefab` は静止表示用の旧 v01 モデルを参照している

### Care

- Poko は Main と同じ Prefab／Animator Controller を使う
- Scene 側の `CarePokoController`、FaceController、表情、まばたき、Eat 用結線は Poko 専用
- Care では通常時に `IsWalking=false` とし、Eat は `IsEating` と専用コルーチンで制御する
- Happy／Eat／Talk は Idle／Walk の移動基盤から分離できる

### モデル命名ルール

- 静止表示用: `〇〇_Character_v01.fbx`
- アニメーション対応: `〇〇_Character_v02.fbx`
- Koko／Piyoko の v02 は Generic Rig
- Avatar Definition は Create From This Model
- Optimize Game Objects と Apply Root Motion は OFF
- Idle／Happy／Eat／Walk の4クリップのみを明示 Import する
- Idle／Walk は Loop Time・Loop Pose ON
- Happy／Eat は Loop Time OFF

---

## 検討した案

### A. 既存 PetoWalk を直接5キャラ対応へ大改造

既存の移動、Animator取得、Poko表示制御へキャラ別分岐を追加する案。

### B. 共通 CharacterAnimationController と Poko互換フォールバック

Animator操作を共通 `CharacterAnimationController` へ分離し、実行時生成キャラを `PetoWalk` へ登録する案。
登録がない場合は現在のPoko動作をそのまま使う。

### C. キャラクターごとの個別スクリプト

Poko、Koko、Piyokoごとに移動・Animator制御スクリプトを作る案。

### D. CharacterMovementRootを新設

Poko表示Rootと移動Rootを完全分離し、共通移動Rootへ全キャラを配置する案。

---

## 採用案

共通 `CharacterAnimationController` と Poko互換フォールバックを採用する。

- キャラごとに Animator Controller を用意する
- 共通 Parameter 名は `IsWalking`
- `CharacterStaticDisplayController` が生成キャラを `PetoWalk` へ登録する
- 登録キャラがある場合、登録された Transform を移動・回転する
- 登録キャラがない場合、現在の Poko 動作へフォールバックする
- キャラ固有の向き補正は `CharacterAnimationController` が保持する
- `PetoWalk` は現段階では `PokoWalkRoot` に残す
- `PokoWalkRoot` 自体は非アクティブにせず、Poko以外では `PokoVisualRoot` だけを非表示にする

---

## 不採用案と理由

### PetoWalkの直接大改造

**何が問題か**:
既存Pokoの移動、回転、Animator取得、障害物回避へキャラ別条件が混在する。

**不採用理由**:
Pokoを壊す危険性が高く、Happy／Eat／Talk追加時にさらに責務が肥大化するため。

### キャラ別スクリプト

**何が問題か**:
移動状態、WalkZone、到着判定、Animator切り替えがキャラ数分重複する。

**不採用理由**:
修正漏れと挙動差が生じやすく、Main／Careへの展開コストが増えるため。

### CharacterMovementRootの新設

**何が問題か**:
Scene階層、Pokoの配置、既存Inspector結線を同じ実装単位で変更する必要がある。

**現段階で不採用とする理由**:
Idle／Walk基盤の実装と土台改修を混ぜず、既存 `PokoWalkRoot` を維持して段階移行するため。将来の整理候補としては残す。

---

## Poko互換フォールバック

`PetoWalk` に登録キャラがない場合は、現在のPoko向け処理を維持する。

```text
登録キャラあり
  -> 登録された Transform と CharacterAnimationController を使用

登録キャラなし
  -> this.transform、visualRoot、GetComponentInChildren<Animator>() を使用
```

このフォールバックにより、PokoのScene結線を一括変更せず、Piyokoから先に動作確認できる。
Pokoの既存Animator、FaceController、まばたき、Care Eat処理は置換しない。

---

## 実行時生成キャラを登録する理由

実行時に `Instantiate` されるキャラは、Scene保存時には存在しないためInspectorへ事前結線できない。
自動検索だけに依存すると、複数Animator、非アクティブ状態、階層変更によって誤取得しやすい。

生成元の `CharacterStaticDisplayController` は生成直後のGameObjectを確実に保持しているため、そこから `PetoWalk` へ明示登録する。
生成処理は `Awake()` で行われ、`PetoWalk.Start()` より先に完了する。

---

## 最初の実装手順

1. `CharacterAnimationController.cs` を新規追加
2. `PetoWalk.cs` に実行時キャラ登録 API を追加
3. 未登録時のPoko互換フォールバックを維持
4. `CharacterStaticDisplayController.cs` から生成キャラを登録
5. SerializeField は用意のみとする
6. ユーザーがScene／Prefab／Animator／Inspectorを結線
7. MainでPiyokoのIdle／Walkを確認

---

## 動作確認テスト結果

- **Koko v02 Import**: 正式4 Source Take、不要 Source Take なし
- **Piyoko v02 Import**: 正式4 Source Take、不要 Source Take なし
- **Rig**: Koko／PiyokoともGeneric、Create From This Model
- **Loop**: Idle／WalkはLoop Time・Loop Pose ON、Happy／EatはLoop Time OFF
- **Root Motion**: Koko／PiyokoともApply Root Motion OFF
- **Main Play Mode**: 未確認
- **Poko回帰確認**: 未確認
- **Piyoko Idle／Walk**: 未確認

---

## 未確認事項

- 新しいKoko／Piyoko用PrefabとAnimator Controllerの正式パス
- キャラごとの向き補正値とスケール
- 実装後のAwake／Start実行順序
- MainでのPoko既存動作の回帰確認
- MainでのPiyoko Idle／Walk
- Piyoko確認後のKoko展開
- CareでのIdle表示と将来のEat／Happy連携
- Consoleに残る旧Import Error履歴との時系列区別

---

## 今後の段階移行方針

1. MainでPiyokoのIdle／Walk基盤を実装・確認
2. Pokoの既存移動・Animator・表情・まばたきを回帰確認
3. 同じ基盤をKokoへ展開
4. Mainで安定後、Careの通常Idle表示へ展開
5. Happy／Eat／Talkは別の変更単位で追加
6. 必要性が確認できた段階でCharacterMovementRoot新設を再検討

---

## メモ・気づき

- 実行時生成オブジェクトはInspector結線ではなく生成元から登録する
- 共通化対象はAnimationClipや骨格ではなく、`IsWalking` などの命令面に限定する
- Generic Rigのため、キャラ別Animator Controllerを許容する
- 機能実装とScene土台改修を同じ変更単位に混ぜない

---

## 参考

- 現在の作業内容: `docs/current-focus.md`
- 正式仕様: `docs/requirements.md`（今回変更なし）
- 長期メモ: `CLAUDE.md`（実装・動作確認前のため今回変更なし）
