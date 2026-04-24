# GitHub Issues: Level 0（環境整備）

Level 0 全体の Milestone: `Level 0: 環境整備`

以下を1つずつ Issue として作成してください。

---

## Issue #1: Unity MCP 導入
**Labels**: type:infra, priority:high, area:infra
**Milestone**: Level 0: 環境整備

### 概要
Claude Code から Unity Editor を操作できるよう、Unity MCP を導入する。

### タスク
- [ ] CoplayDev/unity-mcp のドキュメントを読む
- [ ] Unity Package Manager から Unity MCP パッケージを追加
- [ ] Claude Code の設定ファイルに Unity MCP を登録
- [ ] 動作確認（「赤いキューブを作って」等の簡易プロンプトで検証）

### 完了条件
Claude Code から Unity Editor の操作が成功すること。

### 参考
- https://github.com/CoplayDev/unity-mcp

---

## Issue #2: BlenderMCP 導入
**Labels**: type:infra, priority:high, area:infra
**Milestone**: Level 0: 環境整備

### 概要
Claude Code から Blender を操作できるよう、BlenderMCP を導入する。

### タスク
- [ ] Blender をインストール（3.0以上）
- [ ] ahujasid/blender-mcp のドキュメントを読む
- [ ] Blender アドオンをインストール
- [ ] MCP サーバーをセットアップ（uvx blender-mcp）
- [ ] Claude Code の設定ファイルに登録
- [ ] 動作確認（「球体を作って赤くして」等で検証）

### 完了条件
Claude Code から Blender の操作が成功すること。

### 参考
- https://github.com/ahujasid/blender-mcp

---

## Issue #3: Claude Code 設定（プロジェクト連携）
**Labels**: type:infra, priority:high, area:infra
**Milestone**: Level 0: 環境整備

### 概要
Claude Code がプロジェクトのコンテキストを適切に把握できるよう設定する。

### タスク
- [ ] CLAUDE.md の作成（プロジェクト概要、技術スタック、コーディング規約）
- [ ] .clauderc や関連設定ファイルの配置
- [ ] 要件定義書をリポジトリに追加
- [ ] Claude Code が要件定義書を読み込めることを確認

### 完了条件
Claude Code が要件定義書と CLAUDE.md を参照して的確な支援ができる状態。

---

## Issue #4: Firebase プロジェクト作成
**Labels**: type:infra, priority:high, area:infra
**Milestone**: Level 0: 環境整備

### 概要
Firebase Authentication / FCM / Analytics / Crashlytics を使うため、Firebase プロジェクトを作成する。

### タスク
- [ ] Firebase Console でプロジェクト作成（プロジェクト名: yurufu-world）
- [ ] Authentication を有効化
  - 匿名認証有効化
  - Apple 認証（後日設定、今は無効）
  - Google 認証（後日設定、今は無効）
  - メール/パスワード認証（後日設定、今は無効)
- [ ] Firestore は使わない（DynamoDB 採用のため、作成しない）
- [ ] Firebase SDK for Unity を Unity プロジェクトに導入
- [ ] google-services.json / GoogleService-Info.plist の配置

### 完了条件
Unity エディタ上で Firebase 接続が成功し、匿名ログインができること。

---

## Issue #5: AWS アカウント準備
**Labels**: type:infra, priority:high, area:infra
**Milestone**: Level 0: 環境整備

### 概要
AWS Lambda / DynamoDB / API Gateway を使うため、AWS 環境を整える。

### タスク
- [ ] AWS アカウント作成（既存でも可）
- [ ] IAM ユーザー作成（プログラマティックアクセス用）
- [ ] AWS CLI のインストールと設定
- [ ] リージョン選定（推奨: ap-northeast-1 東京）
- [ ] コスト予算アラートの設定（月$20 等で）

### 完了条件
AWS CLI から DynamoDB へのアクセスが成功すること。

---

## Issue #6: Meshy Pro プラン契約
**Labels**: type:infra, priority:medium, area:assets
**Milestone**: Level 0: 環境整備

### 概要
キャラクターの3D化のため、Meshy Pro プランを契約する。

### タスク
- [ ] Meshy アカウント作成
- [ ] Pro プラン契約（$10/月）
- [ ] える1キャラで image-to-3D 試験
- [ ] 品質評価（フェルト質感、ポーズ、UV等）
- [ ] 合格の場合: 残り3キャラも3D化
- [ ] 不合格の場合: Tripo AI 等の代替を検討

### 完了条件
キャラクターの3Dモデル（FBX形式）が4キャラ分揃うこと。
または、代替手段（スプライトアニメ等）に切り替える判断がつくこと。

---

## Issue #7: GitHub リポジトリ整備
**Labels**: type:docs, priority:high, area:infra
**Milestone**: Level 0: 環境整備

### 概要
プロジェクトのリポジトリ管理方針を整える。

### タスク
- [ ] リポジトリ名の統一（現状: FuwaPet → yurufu-world 等へ）
- [ ] private 化の確認
- [ ] .gitignore の見直し（Unity用、OS用）
- [ ] README.md の更新
- [ ] ブランチ戦略の決定（main / develop / feature/*）
- [ ] ブランチ保護ルール設定（main への直接 push 禁止等）
- [ ] GitHub Actions の検討（任意）

### 完了条件
リポジトリがプロの個人開発者が運用している水準に整備されていること。

---

## Issue #8: 既存 Unity プロジェクトの棚卸し
**Labels**: type:refactor, priority:high, area:ui
**Milestone**: Level 0: 環境整備

### 概要
過去の試行錯誤で残った不要ファイルの特定と削除、Prefab化の整理。

### タスク
- [ ] Assets/Scripts 内の全スクリプトをリストアップ
- [ ] 各スクリプトの用途を明記（使用中 / 未使用 / 要確認）
- [ ] 未使用スクリプトの削除
- [ ] 命名規則の統一（PascalCase / camelCase 等のルール決定）
- [ ] Prefab化が未完のUIパーツの特定とPrefab化
- [ ] 現状の画面一覧と機能マップの作成

### 完了条件
プロジェクト構造が整理され、どこに何があるか把握できている状態。

---

## Issue #9: 要件定義書のリポジトリ追加
**Labels**: type:docs, priority:medium, area:infra
**Milestone**: Level 0: 環境整備

### 概要
この要件定義書をリポジトリで管理する。

### タスク
- [ ] docs/ ディレクトリ作成
- [ ] 要件定義書を docs/requirements.md として配置
- [ ] リポジトリ README から参照リンクを張る

### 完了条件
リポジトリをクローンすれば誰でも（Claude Code 含む）要件定義書を参照できる状態。

