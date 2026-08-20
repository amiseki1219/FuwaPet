# YURUFUワールド

![Unity](https://img.shields.io/badge/Unity-6000.3.14f1-000000?logo=unity)
![C#](https://img.shields.io/badge/C%23-11,600%20lines-239120?logo=csharp)
![URP](https://img.shields.io/badge/URP-Linear-2196F3)
![Platform](https://img.shields.io/badge/Platform-iOS%20%7C%20Android-lightgrey)

AIキャラクターと対話しながら、時間をかけて「うちの子」を育てる癒し系モバイルアプリ。個人開発です。

| | |
|---|---|
| ジャンル | 癒し系 × AIチャット × キャラ育成 |
| プラットフォーム | iOS / Android（現在は iOS 実機で動作確認中） |
| リリース目標 | 2027年 春〜初夏 |
| 開発体制 | 個人開発 |
| 規模 | C# 92ファイル / 約11,600行、シーン16本 |

---

## 開発状況

**リリース前のため、仕様の詳細は公開していません。**

### 実装済み

- オンボーディング（規約同意 → キャラクター選択 → 初期設定 → 起動フローの振り分け）
- 拠点画面・お世話画面の UI とロジック
- キャラクターの状態パラメータと、実時間にもとづく経過処理
- 育成度の算出と進捗表示
- キャラクター表情システム（状態に応じた切り替え・まばたき）
- 3Dキャラクターの表示・移動・アニメーション制御
- ルームカスタマイズ
- クエストの進捗管理と報酬付与
- ローカルセーブ（項目追加に対する後方互換つき）
- iOS 実機でのビルド・インストール・起動確認

### 設計完了・実装はこれから

- AI会話基盤（リアルタイム応答とバッチ処理の2段構成）
- サーバーサイド（AWS）
- 認証・プッシュ通知・分析基盤
- マネタイズ設計

### 未着手

ソーシャル機能、ミニゲーム。

---

## 技術スタック

### クライアント

- **Unity 6000.3.14f1** / C#
- **URP**（Linear 色空間）
- **uGUI** + TextMeshPro
- ScriptableObject によるデータ駆動
- 3Dキャラクター（image-to-3D で生成 → Blender で調整 → Unity）

### サーバー（設計済み・実装前）

- AWS Lambda（Python）/ API Gateway / DynamoDB / EventBridge
- AWS CDK（TypeScript）
- Firebase Authentication / Cloud Messaging / Analytics / Crashlytics

---

## プロジェクト構成

```
Assets/
├── Art/
│   ├── 3D/           キャラクター・部屋・家具
│   └── UI/           UI素材
├── Scenes/           16シーン
├── Scripts/
│   ├── Core/         全体で共有する状態と日次処理
│   ├── Save/         セーブデータとその管理
│   ├── Character/    表示・移動・アニメーション
│   ├── Pet/          状態パラメータ・育成度の計算
│   ├── Room/         ルームカスタマイズ
│   ├── Care/         お世話画面
│   ├── Tutorial/     オンボーディング
│   ├── Main/         拠点画面
│   ├── Quest/        クエスト
│   └── UI/           共通UI部品
└── Resources/
```

---

## 開発について

Claude Code / Unity MCP / BlenderMCP を併用し、コード生成と3Dモデルの調整を支援させながら開発しています。
Scene 配置・Prefab 編集・Inspector の結線は、構造を理解するため手作業で行っています。
