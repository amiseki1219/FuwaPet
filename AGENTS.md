# YURUFUワールド — Codex作業ルール

## 役割と情報源

- `docs/requirements.md` はアプリ仕様の正本。
- `CLAUDE.md` は詳細な設計判断・実装履歴・長期メモ。
- `AGENTS.md` はCodexが日々の作業で守る実務ルール。
- 最新のユーザー指示が、すべての既存ドキュメントより優先される。
- 設計判断が必要で複数案がある場合は、編集前に選択肢と影響を報告して確認を取る。

## Gitと作業開始時のルール

- 作業開始前に必ず `git branch`、`git status`、必要に応じて `git diff --stat` を読み取り、現在の状態を報告する。
- Gitの状態変更は禁止。
  - `git add` / `commit` / `push` / `pull` / `merge`
  - branch作成・切替
  - `reset` / `checkout` / `clean`
- Git操作、Forkでのコミット・push・mergeはユーザーが手動で行う。
- `git clean -fd` は絶対に実行しない。
- 作業ツリーがdirtyの場合、既存変更を勝手に削除・退避・上書きせず、先に報告する。
- 機能ごとに変更を分ける。土台改修・機能実装・リファクタリングを同じ変更単位に混ぜない。

## Unity資産の安全ルール

- `.unity`、`.prefab`、`.meta`、`.blend`、FBX、Animator Controller、ProjectSettingsは明示的な許可なしに編集しない。
- Scene配置、Prefab化、FBXインポート設定、SerializeFieldのInspector結線はユーザーがUnity上で手作業する。
- Unity資産の移動・再生成・GUID変更を勝手に行わない。
- Assets/_Archive と Assets/_Recovery は実装元として扱わず、通常の機能実装対象にしない。
- Build Settingsを変更する場合は、アーカイブSceneを参照していないか必ず確認する。
- 現在、MyCollectionはBuild Settingsで `Assets/_Archive/MyCollection.unity` を参照している。明示指示なしに修正しない。

## 調査と実装の進め方

- 新しい機能領域に入る前は、必ずread-onlyで関連コード・Scene構造・依存関係を調査する。
- 調査段階ではファイル編集、Unity変更、Git変更をしない。
- 実装で編集してよいC#ファイルを事前に明示する。触らないファイルも明示する。
- 新規SerializeFieldは用意のみ行い、Inspectorでユーザーが結線する対象を一覧で報告する。
- null安全を優先し、既存データがない場合は安全にフォールバックする。
- `Pet` 系から `Character` 系への命名移行は段階的に行い、一括置換はしない。

## キャラクター・オンボーディングの重要ルール

- 正規キャラクターIDはすべて小文字文字列。
  - `poko`
  - `eru`
  - `koko`
  - `paru`
  - `piyoko`
- 新規表示ロジックでは `selectedCharacterId` を正とする。
- 旧 `characterId` は既存セーブ互換のため、明示指示なしに削除・一括移行しない。
- Tutorialのキャラ決定時に行う、ID保存・初期性格設定・Save処理の流れを壊さない。
- `onboardingCompleted` と Home / Tutorial / Main の起動振り分けを壊さない。
- Main / Care / Bath のPokoは同じPrefab参照でも、Scene側の追加Component・Animator・表情結線が異なる。
- 既存PokoのFBX、Prefab、Animator、PetoWalk、FaceController、PokoBlinkController、CarePokoControllerを無断で置換・削除・共通化しない。
- 5キャラ対応は、最初に「キャラクターIDから静止Prefabを表示する」段階を実装する。
- Poko専用の歩行、食事、表情、まばたきを他キャラへ無条件に流用しない。
- 5キャラをSceneへ全配置してActive切替する設計は、不要なAnimatorやCoroutineが同時実行されるため採用しない。

## ログ・検証・完了報告

- デバッグログには種別プレフィックスを付ける。
  - 例：`[Character]`、`[Onboarding]`、`[Bath]`
- 決定イベントのログは水色を使用する。
  - 例：`<color=#00E5FF>[決定]</color>`
- SceneまたはPrefabにユーザーが手作業で変更を入れた後は、Missing Script、Missing Reference、Prefab Override、Console、対象Sceneを確認するよう案内する。
- 完了時は必ず以下を報告する。
  1. 変更ファイル
  2. 変更内容
  3. Inspectorでユーザーが結線する対象
  4. Unityコンパイルエラーの有無（未確認なら未確認と明記）
  5. 実施した確認
  6. 手を止めた箇所・未解決事項
  7. `git status`
