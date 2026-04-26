# YURUFUワールド

![Unity](https://img.shields.io/badge/Unity-6-000000?logo=unity)
![AWS](https://img.shields.io/badge/AWS-Lambda%20%7C%20DynamoDB-FF9900?logo=amazonaws)
![Firebase](https://img.shields.io/badge/Firebase-Auth%20%7C%20FCM-FFCA28?logo=firebase)
![Platform](https://img.shields.io/badge/Platform-iOS%20%7C%20Android-lightgrey)

AIキャラクターと対話しながら、1年かけて「うちの子」を育てる癒し系モバイルアプリです。

## プロジェクト概要

| 項目 | 内容 |
|------|------|
| ジャンル | 癒し系 × AIチャット × キャラ育成 |
| プラットフォーム | iOS / Android |
| リリース目標 | 2027年 春〜初夏 |
| 開発体制 | 個人開発 |

ユーザーの言葉をAIが理解し、キャラクターの性格が会話や行動を通じて変化していく「自分だけの子を育てる体験」を目指しています。

## 技術スタック

### フロントエンド
- **Unity 6**（uGUI） / C#
- **3Dキャラクター** — Meshy Pro（image-to-3D）+ Blender で調整

### バックエンド
- **AWS Lambda**（Python） + API Gateway
- **Amazon DynamoDB**（3テーブル構成）
- **AWS EventBridge**（夜間バッチスケジューラ）

### IaC
- **AWS CDK**（TypeScript）— L2コンストラクト中心

### 認証・通知・分析
- **Firebase** Authentication / Cloud Messaging / Analytics / Crashlytics

### AI
- **Google Gemini Flash** — リアルタイム会話
- **Google Gemini Pro** — 夜間バッチ分析（上位プラン）

### 開発支援
- Claude Code / Unity MCP / BlenderMCP

## 開発状況

現在 **Level 0（環境整備）** を進行中です。
詳細な進捗は [GitHub Issues](../../issues) および [Milestones](../../milestones) を参照してください。

## ドキュメント

- [要件定義書](docs/requirements.md) — アプリ設計の全体像
