# CLAUDE.md

このファイルは Claude Code がこのプロジェクトで作業する際のガイドです。

## プロジェクト概要

**YURUFUワールド** - AIキャラクターと対話し、1年かけて「うちの子」を育てる癒やし系モバイルアプリ。

- **ジャンル**: 癒し系 × AIチャット × キャラ育成（たまごっち+どうぶつの森+シムズ+ヤンデレコレの融合）
- **プラットフォーム**: iOS / Android
- **リリース目標**: 2027年春〜初夏
- **開発体制**: 個人開発
- **作業時間**: 平日帰宅後2〜3時間、土日可変

### 詳細な要件定義書
必ず `docs/requirements.md` を参照してください。このファイルにアプリ設計の全てが書かれています。

## 技術スタック

### フロントエンド
- **Unity 6**（uGUI）
- **C#** でスクリプト
- **3Dキャラクター**（Meshy生成、Unity で3Dレンダリング、背景は2D）

### バックエンド
- **AWS Lambda**（**Python**）
  - AI/ML系ライブラリが豊富、Gemini SDK の整備が良い
  - 将来のAIエンジニア転職との整合性
- **AWS API Gateway**
- **AWS DynamoDB**（3テーブル構成: YurufuUsers / YurufuEvents / YurufuMemories）
- **AWS EventBridge**（夜バッチスケジューラ、JST 3:00 AM = UTC 前日 18:00）

### IaC（Infrastructure as Code）
- **AWS CDK + TypeScript**（方向性確定、Level 1 着手直前に最終判断）
  - Java 経験者にとって TypeScript は学習コスト低
  - Lambda本体はPython、インフラ定義はTypeScript のハイブリッド構成
  - **L2 コンストラクト中心**に使用（L1は低レベルすぎ、L3は過剰抽象）
  - GW中にCDK Getting Started 素振りを実施予定

### 認証・通知・分析
- **Firebase Authentication**（匿名認証 + Apple/Google/メアド）
- **Firebase Cloud Messaging**（プッシュ通知）
- **Firebase Analytics**
- **Firebase Crashlytics**

### AI
- **Google Gemini Flash**（リアルタイム会話、全プラン共通）
- **Google Gemini Pro**（夜バッチ分析、仲良し/運命の絆プランのみ）

### アセット生成
- **Meshy Pro**（$10/月、image-to-3D、自動リギング）
- **Blender**（3Dモデル調整、BlenderMCP で Claude 操作可能）

### 開発支援
- **Claude Code**（コーディング支援）
- **Unity MCP**（Unity エディタを Claude から操作）
- **BlenderMCP**（Blender を Claude から操作）

## コーディング規約

### C# (Unity)
- **命名**: PascalCase（クラス、メソッド、publicプロパティ）、camelCase（ローカル変数、privateフィールド）
- **データアクセス層の分離**: Repository パターンを採用（DB変更に備える）
- **非同期処理**: async/await を優先、コルーチン最小化
- **null安全**: 可能な限り nullチェック、?? 演算子活用

### Python (Lambda本体)
- **命名**: snake_case
- **型ヒント**: 可能な限り記述（`def handler(event: dict, context: Any) -> dict:`）
- **エラーハンドリング**: 必ず try/except、適切なログ出力（print ではなく logging モジュール使用）
- **環境変数**: 秘密情報はAWS環境変数、絶対にコミットしない
- **依存管理**: requirements.txt（または Poetry）

### TypeScript (CDK / IaC)
- **命名**: camelCase（変数・関数）、PascalCase（クラス・型・インターフェース）
- **型定義**: any は避ける、明示的な型を優先
- **CDKコンストラクト**: L2 を基本に使用
- **スタック分割**: 機能単位（AuthStack / ApiStack / DataStack 等）で責務分離
- **環境分け**: dev / prod の2環境想定、context で切り替え

### 共通
- **コミットメッセージ**: Conventional Commits 形式推奨
  - `feat:` 新機能、`fix:` バグ修正、`refactor:` リファクタ、`docs:` ドキュメント
- **ブランチ戦略**: main / develop / feature/{issue番号}-{概要}

## 重要な設計判断（要件定義書より抜粋）

### 1. 癒し系アプリの世界観を守る
- **罪悪感を煽らない**: ポイント減点なし、コンディション下限30（0にはならない）
- **解約引き止めUIを作らない**: ダークパターン回避、誠実さ優先
- **エラー時はキャラのセリフで世界観維持**（AI失敗時のみ、通信/課金は事務的）

### 2. データは消さない
- 会話履歴は全ユーザー全会話を永久保存（GDPR 削除申請時を除く）
- 解約時もデータ保持、再加入で全復活
- プロンプトに渡す件数でプラン差別化（10/20/50/100件）

### 3. AI呼び出しは2段構成
- リアルタイム会話: Flash（コスト最小）
- 夜バッチ分析: プラン別（無料/寄り添いはFlash、仲良し/運命はPro）

### 4. キャラ固有性格システム（核心機能）
- 2層構造: 不変コア（全員共通） + ユーザー固有性格（5パラ、行動で変化）
- 「うちのえる」を作る
- 性格変化判定は夜バッチで実施

### 5. 3Dキャラ採用、Live2Dは不採用
- Meshyで画像→3D変換
- BlenderMCP + Unity MCP で高速組み込み
- 将来の部屋3D化との整合性

### 6. Unity MCP 接続設定（確定済み）
- Transport: HTTP モード（stdio は使わない）
- .mcp.json の "type" フィールドは必須
```json
{
  "mcpServers": {
    "unityMCP": {
      "type": "http",
      "url": "http://127.0.0.1:8080/mcp"
    }
  }
}
```
- 毎回の起動手順:
  1. Unity を Dock から起動
  2. Window > MCP For Unity > Toggle MCP Window
  3. Start Server をクリック
  4. ターミナルで claude 起動
  5. /mcp で connected / authenticated を確認

### 7. キャラクター3Dモデルの方針
- Level 0〜2 は Meshy デフォルト出力で十分（仮で進める）
- フェルト感・品質追求は Level 3（2026年10月）で実施
- 設計（性格・DB・AIプロンプト）は今ちゃんと決める
- 見た目は仮でOK、中身の設計は妥協しない

### 8. 確定済み技術選定（2026/4/25）
- 夜バッチ実行時刻: JST 3:00 AM（= UTC 前日 18:00）
  - 朝 7:30 のあいさつ通知までに分析完了が必要なため
  - EventBridge ルールは「バッチ用」「通知用」で分けて作成

### 9. UI 設計確定事項（2026/4/25）
- HOME 画面は起動時専用（Tap to Start 画面）
- Care 画面のナビから HOME ボタンを削除
- Care 画面ナビは Shop / Setting / MyCollection の3個

## セキュリティ・プライバシー

### 絶対にコミットしないもの
- API キー（Gemini、Firebase、AWS）
- 秘密鍵
- サービスアカウント JSON
- .env ファイル
- Apple 署名証明書

### .gitignore に入れるべきもの
```
# Unity
Library/
Temp/
Obj/
Build/
Builds/
Logs/
UserSettings/

# IDE
.vs/
.vscode/
.idea/

# OS
.DS_Store
Thumbs.db

# Secrets
*.env
.env.*
secrets/
google-services.json
GoogleService-Info.plist

# Python
__pycache__/
*.pyc
venv/
.venv/
```

## 作業方針

### Claude Code への依頼の仕方
1. **Issueを明示**: 「Issue #X をやって」と伝えると Claude が該当タスクに集中できる
2. **要件定義書を参照**: 「docs/requirements.md のセクションY を参照して実装」と指示
3. **段階的に**: 大きい機能は小さいPRに分割

### Claude Code が迷ったら
- まず `docs/requirements.md` を参照
- それでも不明な場合はユーザーに質問
- 推測で進めず、必ず確認

### Git 操作方針
- Git 操作（add / commit / push / merge / PR作成）はユーザーが手動で行う（Fork を使用）
- Claude Code はコード生成・ファイル編集のみ担当
- コミット前に必ず git status / diff をユーザーが確認する
- ブランチは必ずユーザーが事前に作成する

### テストの方針
- Unity: PlayMode テストで主要ロジックをカバー
- Lambda: ユニットテスト + ローカル実行での動作確認
- E2E は実機テスト（Level 6）

## プロジェクト構造（想定）

```
yurufu-world/
├── docs/
│   └── requirements.md        # 要件定義書
├── unity/                     # Unity プロジェクト
│   ├── Assets/
│   │   ├── Scripts/
│   │   ├── Prefabs/
│   │   ├── Scenes/
│   │   └── ...
│   └── ...
├── backend/                   # AWS Lambda コード + IaC
│   ├── functions/             # Lambda本体（Python）
│   │   ├── chat/
│   │   ├── analyze_nightly/
│   │   └── ...
│   └── infra/                 # IaC（AWS CDK + TypeScript）
├── CLAUDE.md                  # このファイル
├── README.md
└── .gitignore
```

## 現在の進捗フェーズ

**Level 0: 環境整備**

現在、要件定義を完了し、実装に着手する段階。
最新の Issue / Milestone 状況は GitHub Project を参照。

## Q&A（Claude Code からよくある質問）

### Q: テーブル構造を変えたいんだけど？
A: DynamoDB は柔軟に属性追加できるので、`schemaVersion` フィールドを活用してマイグレーションを検討してください。データアクセス層を経由するので、その層だけ修正すれば大丈夫なはずです。

### Q: Live2D に変えたい？
A: 既にMeshy + 3D に決定済み。理由は要件定義書の「付録B: 設計判断の履歴」参照。変更の前にユーザーに相談してください。

### Q: どのAIモデルを使うべき？
A: リアルタイムは Gemini Flash、夜バッチは Gemini Pro（プランによる）。コスト最適化済み。変更前にユーザーに相談してください。

### Q: 新機能を追加したい
A: まず要件定義書に照らして「v1.0 に必要か」判断。v1.1以降で良いものは後回し推奨。

### Q: Lambda を Node.js で書いていい？
A: 既に **Python で確定**済み。理由: AI/MLライブラリの豊富さ、Gemini SDK 整備、将来のAIエンジニア転職との整合性。変更前にユーザーに相談してください。

### Q: IaC を SAM や Terraform に変えていい？
A: **AWS CDK + TypeScript** で方向性確定（Level 1 着手直前に最終判断予定）。SAM は YAML の表現力の限界、Terraform は AWS特化ではないためツール連携で劣る、という判断。変更前にユーザーに相談してください。

### Q: CDK のスタック分割はどうする？
A: 機能単位で分割（例: `AuthStack`, `ApiStack`, `DataStack`, `NotificationStack`）。DataStack（DynamoDB）は他スタックから参照されるため、依存関係の起点になる。

## 連絡先・参考資料

- リポジトリ: github.com/amiseki1219/yurufu-world（private）
- 要件定義書: docs/requirements.md
- Figma/デザイン: （あれば記載）
- 公式Twitter: （開設したら記載）
