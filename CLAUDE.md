# CLAUDE.md

このファイルは Claude Code がこのプロジェクトで作業する際のガイドです。

## プロジェクト概要

**YURUFUワールド** - AIキャラクターと対話し、1年かけて「うちの子」を育てる癒やし系モバイルアプリ。

- **ジャンル**: 癒し系 × AIチャット × キャラ育成（たまごっち+どうぶつの森+シムズ+ヤンデレコレの融合）
- **プラットフォーム**: iOS / Android
- **リリース目標**: 2027年春〜初夏
- **開発体制**: 一人開発（エンジニア1年目）
- **作業時間**: 平日帰宅後2〜3時間、土日可変

### 詳細な要件定義書
必ず `docs/requirements.md` を参照してください。このファイルにアプリ設計の全てが書かれています。

## 技術スタック

### フロントエンド
- **Unity 6**（uGUI）
- **C#** でスクリプト
- **3Dキャラクター**（Meshy生成、Unity で3Dレンダリング、背景は2D）

### バックエンド
- **AWS Lambda**（Python or Node.js、要決定）
- **AWS API Gateway**
- **AWS DynamoDB**（3テーブル構成: YurufuUsers / YurufuEvents / YurufuMemories）
- **AWS EventBridge**（夜バッチスケジューラ）

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

### Python/Node.js (Lambda)
- **命名**: snake_case（Python）、camelCase（Node.js）
- **エラーハンドリング**: 必ず try/catch、適切なログ出力
- **環境変数**: 秘密情報はAWS環境変数、絶対にコミットしない

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
├── backend/                   # AWS Lambda コード
│   ├── functions/
│   │   ├── chat/
│   │   ├── analyze_nightly/
│   │   └── ...
│   └── infra/                 # IaC（CDK/Terraform 検討）
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

## 連絡先・参考資料

- リポジトリ: github.com/amiseki1219/yurufu-world（private）
- 要件定義書: docs/requirements.md
- Figma/デザイン: （あれば記載）
- 公式Twitter: （開設したら記載）
