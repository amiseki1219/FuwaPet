# フォント設定手順

## フォントファイルの入手
Google Fonts から M PLUS Rounded 1c をダウンロード
https://fonts.google.com/specimen/M+PLUS+Rounded+1c

全ウェイトの .ttf をこのフォルダに配置

## 文字セット生成
プロジェクトルートで以下を実行:
python Assets/Fonts/generate_characters.py

## SDF フォントアセット生成
Unity → Window → TextMeshPro → Font Asset Creator
- Source Font File: MPLUSRounded1c-Regular.ttf
- Atlas Resolution: 4096 x 4096
- Character Set: Characters from File
- Character File: Assets/Fonts/japanese_characters.txt
- Generate Font Atlas → Save
