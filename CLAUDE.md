# CLAUDE.md

このファイルは Claude Code がこのプロジェクトで作業する際のガイドです。

> **最終更新**: 2026年5月1日
> **更新履歴**: 末尾の「更新履歴」セクションを参照

---

## プロジェクト概要

**YURUFUワールド** - AIキャラクターと対話し、1年かけて「うちの子」を育てる癒やし系モバイルアプリ。

- **ジャンル**: 癒し系 × AIチャット × キャラ育成（たまごっち + どうぶつの森 + シムズ + ヤンデレコレの融合）
- **プラットフォーム**: iOS / Android
- **リリース目標**: 2027年春〜初夏
- **開発体制**: 一人開発（エンジニア1年目）
- **作業時間**: 平日帰宅後2〜3時間、土日可変
- **運営者**: Ami Seki（関 あみ）/ 屋号 Ami Seki

### 詳細な要件定義書
必ず `docs/requirements.md` を参照してください。このファイルにアプリ設計の全てが書かれています。

### 重要な公開URL
| 項目 | URL |
|---|---|
| 利用規約 | https://jagged-wombat-9c5.notion.site/YURUFU-35184120f12f80cba92bd4f91f2bdeae |
| プライバシーポリシー | https://jagged-wombat-9c5.notion.site/YURUFUWorld-35184120f12f80b4b2b7f16a179c5785 |
| お問い合わせ | https://forms.gle/cw6MdGnq1Kibqbdr7 |

### AWS 環境情報
| 項目 | 値 |
|---|---|
| リージョン | ap-northeast-1 |
| IAMユーザー | yurufu-dev |
| Account ID | 491852264509 |
| 予算アラート | $10/月（amisato.n1219@gmail.com 通知） |

---

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

---

## コーディング規約

### C# (Unity)
- **命名**: PascalCase（クラス、メソッド、publicプロパティ）、camelCase（ローカル変数、privateフィールド）
- **データアクセス層の分離**: Repository パターンを採用（DB変更に備える）
- **非同期処理**: async/await を優先、コルーチン最小化
- **null安全**: 可能な限り nullチェック、`??` 演算子活用

### Python (Lambda本体)
- **命名**: snake_case
- **型ヒント**: 可能な限り記述（`def handler(event: dict, context: Any) -> dict:`）
- **エラーハンドリング**: 必ず try/except、適切なログ出力（`print` ではなく `logging` モジュール使用）
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

---

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
- Transport: **HTTP モード**（stdio は使わない）
- `.mcp.json` の `"type"` フィールドは必須

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

**毎回の起動手順**:
1. Unity を Dock から起動
2. Window > MCP For Unity > Toggle MCP Window
3. Start Server をクリック
4. ターミナルで claude 起動
5. `/mcp` で connected / authenticated を確認

### 7. キャラクター3Dモデルの方針
- Level 0〜2 は Meshy デフォルト出力で十分（仮で進める）
- フェルト感・品質追求は Level 3（2026年10月）で実施
- 設計（性格・DB・AIプロンプト）は今ちゃんと決める
- 見た目は仮でOK、中身の設計は妥協しない

### 8. 夜バッチ実行時刻（2026/4/25 確定）
- **JST 3:00 AM**（= UTC 前日 18:00）
- 朝 7:30 のあいさつ通知までに分析完了が必要なため
- EventBridge ルールは「バッチ用」「通知用」で分けて作成

### 9. UI 設計確定事項（2026/4/25）
- HOME 画面は起動時専用（Tap to Start 画面）
- Care 画面のナビから HOME ボタンを削除
- Care 画面ナビは **Shop / Setting / MyCollection** の3個

### 10. Loading画面の方針（2026/5/1 確定）
- **てくてく歩く4キャラ（ぱる・ここ・ぽこ・える）の2フレーム歩行アニメ**
- 8フレームではなく **2フレーム交互ループ**（Aフレーム ⇄ Bフレーム、約0.2秒間隔）
- プログレスバー連動 / 「LOADING...」テキスト表示
- 表示タイミング: シーン遷移時 / API通信待機時の両方
- 配置先:
  - 素材: `Assets/Art/UI/Loading/`
  - スクリプト: `Assets/Scripts/UI/Loading/`

### 11. 起動フローとTutorialフローの保護（2026/5/2 確定）

#### 起動フロー（絶対に変えない）
```
アプリ起動 → Home.unity（Index 0）
  - onboardingCompleted == false（初回/アカウント削除後）
      → Tutorial.unity へ自動遷移
  - onboardingCompleted == true（2回目以降）
      → Home画面表示 → MainBtn → Main.unity
```

#### Tutorialフロー（絶対に変えない）
```
Tutorial.unity 起動
  → Step1: TermsOfUsePanel
      同意ボタン → Next() → StoryPanel
      同意しない → DisAgreePanel
  → Step2: StoryPanel
      FinalStory の TapButton → PlayDoorAnimation() → 動画再生 → Next()
  → Step3: CharacterPanelCard
  → Step4: ProfileSelectionPanelCard
      StartButton → CompleteOnboarding() → onboardingCompleted=true → Main
```

#### 絶対にやってはいけないこと
- `HomeManager.cs` の振り分けロジックを削除する
- `PlayDoorAnimation()` の呼び出し元（StoryPanel の FinalStory TapButton）を変更する
- `CompleteOnboarding()` の呼び出し元（StartButton）を変更する
- `onboardingCompleted` をリセットせずにアカウント削除する
- Tutorial 内のパネルを削除する際に呼び出し元ボタンも一緒に消す

#### 関連ファイル
- `HomeManager.cs`: 起動振り分けロジック
- `OnboardingManager.cs`: Tutorialフロー管理
- `SceneLoader.cs`: `GoToStart()` / `GoToTutorial()`
- `SaveData.cs`: `onboardingCompleted` フラグ

### 12. Main画面とCare画面の役割分担（2026/5/3 確定）

#### Main画面（Main.unity）- アプリの拠点
```
ヘッダー左: ユーザー名・キャラクター名・出会って◯日・信頼度レベル・あと◯ptで次のLv
ヘッダー右: 無償コイン・有償コイン残高・広告なしボタン
中央: キャラクター名・コンディション表示（普通/絶好調等）・3Dキャラ
メインボタン: 「お世話する」（Care画面へ）/ 「会話する」（Chat画面へ）
下部ナビ（5つ）: コレクション・ガチャ・ショップ・お知らせ・クエスト
```
※ガチャは素材未準備のため後回し  
※クエストはデイリークエスト機能（Issue #167相当）

#### Care画面（Care.unity）- お世話専用
```
ヘッダー: キャラクター名・信頼度Lv・コンディション詳細（清潔/空腹/元気バー）
中央: 3Dキャラ
コイン残高表示
お世話ボタン5つ:
  - お風呂（30🪙）
  - なでる（10🪙）
  - あそぶ（20🪙）
  - ごはん（20🪙〜）→ 押下でショップパネルが開く
  - 寝る（FREE）
```

#### ごはんショップパネル（Care画面内）
| アイテム | 価格 | 効果 |
|---------|------|------|
| フード | 20🪙 | - |
| おやつビスケット | 20🪙 | - |
| ジャーキー | 20🪙 | - |
| 特製ごちそう | 50♡ | 空腹全回復 + 信頼度UP |
| バースデーケーキ | 100♡ | 空腹全回復 + 信頼度大UP + 特別メッセージ |

#### 画面遷移
```
Home → Main（拠点）
Main → Care（お世話する）
Main → Chat（会話する）
Main → Shop（ショップ）
Main → MyCollection（コレクション）
Main → Quest（クエスト）
Care → Main（戻る）
```

---

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

---

## 作業方針

### Claude Code への依頼の仕方
1. **Issueを明示**: 「Issue #X をやって」と伝えると Claude が該当タスクに集中できる
2. **要件定義書を参照**: 「`docs/requirements.md` のセクションY を参照して実装」と指示
3. **段階的に**: 大きい機能は小さいPRに分割

### Claude Code が迷ったら
- まず `docs/requirements.md` を参照
- それでも不明な場合はユーザーに質問
- 推測で進めず、必ず確認

### Git 操作方針
- Git 操作（add / commit / push / merge / PR作成）は**ユーザーが手動で行う**（Forkを使用）
- Claude Code は**コード生成・ファイル編集のみ**担当
- コミット前に必ず `git status` / `git diff` をユーザーが確認する
- ブランチは必ずユーザーが事前に作成する
- 作業開始前に必ず `git branch` で現在のブランチを確認する
- 指定されたブランチ以外では編集しない

### Issue管理
- **GitHub Projects** で管理
- Issue番号を明示して作業依頼するとスムーズ

### テストの方針
- **Unity**: PlayMode テストで主要ロジックをカバー
- **Lambda**: ユニットテスト + ローカル実行での動作確認
- **E2E**: 実機テスト（Level 6で実施）

---

## プロジェクト構造

```
yurufu-world/
├── docs/
│   └── requirements.md        # 要件定義書（必読）
├── unity/                     # Unity プロジェクト
│   ├── Assets/
│   │   ├── Art/
│   │   │   └── UI/
│   │   │       └── Loading/   # Loading画面素材
│   │   ├── Scripts/
│   │   │   └── UI/
│   │   │       └── Loading/   # Loading画面スクリプト
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

---

## 現在の進捗フェーズ

**Level 0: 環境整備 → Level 1: 基盤実装フェーズ移行中**

### ✅ 完了済み
- 要件定義書作成（v1.0）
- AWS IAM ユーザー作成（yurufu-dev）+ CLI 設定（Issue #24）
- 予算アラート設定（$10/月）
- フォント（MPLUS Rounded）SDF 生成
- Notion 利用規約・プライバシーポリシー公開
- Google Forms お問い合わせフォーム作成
- **Tutorial 実装完了**（HomePanel → TermsOfUsePanel → StoryPanel → CharacterPanelCard → ProfileSelectionPanelCard → Care）
  - 動画再生（welcomeVideo.mp4）
  - キャラ選択（poko / eru / koko / paru）
  - ニックネーム保存
  - DisAgreePanel（TermsOfUsePanel の子要素）
- ownerName → userName 全置換
- Loading画面素材作成（4キャラ歩行 1枚絵 + Aフレーム切り出し進行中）

### 🔄 進行中
- **Loading画面の実装**（次の着手タスク）
  - Aフレーム素材切り出し（Figma作業中）
  - Bフレーム作成（Aフレーム完了後）
  - Unity実装（C# + Animator/スクリプト）

### 📅 次の予定
- Issue #23 Firebase プロジェクト作成
- 旧ブランチ rescue（feature/chat-ui 等）

最新の Issue / Milestone 状況は **GitHub Projects** を参照。

---

## Q&A（Claude Code からよくある質問）

### Q: テーブル構造を変えたいんだけど？
A: DynamoDB は柔軟に属性追加できるので、`schemaVersion` フィールドを活用してマイグレーションを検討してください。データアクセス層を経由するので、その層だけ修正すれば大丈夫なはずです。

### Q: Live2D に変えたい？
A: 既に Meshy + 3D に決定済み。理由は要件定義書の「付録B: 設計判断の履歴」参照。変更の前にユーザーに相談してください。

### Q: どのAIモデルを使うべき？
A: リアルタイムは Gemini Flash、夜バッチは Gemini Pro（プランによる）。コスト最適化済み。変更前にユーザーに相談してください。

### Q: 新機能を追加したい
A: まず要件定義書に照らして「v1.0 に必要か」判断。v1.1以降で良いものは後回し推奨。

### Q: Lambda を Node.js で書いていい？
A: 既に **Python で確定**済み。理由: AI/MLライブラリの豊富さ、Gemini SDK 整備、将来のAIエンジニア転職との整合性。変更前にユーザーに相談してください。

### Q: IaC を SAM や Terraform に変えていい？
A: **AWS CDK + TypeScript** で方向性確定。SAM は YAML の表現力の限界、Terraform は AWS特化ではないためツール連携で劣る、という判断。変更前にユーザーに相談してください。

### Q: CDK のスタック分割はどうする？
A: 機能単位で分割（例: `AuthStack`, `ApiStack`, `DataStack`, `NotificationStack`）。DataStack（DynamoDB）は他スタックから参照されるため、依存関係の起点になる。

### Q: Loading画面のフレーム数を増やしたい
A: v1.0は **2フレーム** で確定。リッチ化は v1.1 以降で検討してください。

---

## 連絡先・参考資料

- **リポジトリ**: github.com/amiseki1219/yurufu-world（private）
- **要件定義書**: docs/requirements.md
- **Issue管理**: GitHub Projects

---

## 更新履歴

| 日付 | 変更内容 |
|---|---|
| 2026/4/24 | 要件定義書 v1.0 作成 |
| 2026/4/25 | UI設計確定事項（HOME画面・Careナビ）、夜バッチ時刻確定（JST 3:00 AM） |
| 2026/5/1 | **2バージョンのCLAUDE.md統合**、Loading画面方針を「2フレーム歩行アニメ」に確定、AWS環境情報・公開URL追記、進捗フェーズ更新 |
| 2026/5/2 | 起動フロー・Tutorialフロー保護ルールを追記（§11） |
| 2026/5/3 | Main画面・Care画面・ごはんショップの詳細UI構成を追記（§12） |
