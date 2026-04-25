# Issue #20: Unity MCP セットアップ手順・ハマりどころ

## 概要

macOS で MCP for Unity v9.6.6 + Claude Code を疎通させた手順と、
セットアップ時に遭遇した問題・解決策の記録。

## 環境

| 項目 | バージョン |
|------|-----------|
| OS | macOS (Darwin) |
| Unity | 6.3 LTS |
| MCP for Unity | v9.6.6 |
| Python | 3.12.13 |
| uv | 0.11.7 (Homebrew) |
| Claude Code | CLI |

## 主要な詰まりポイント

### 1. `.mcp.json` の書式に `"type"` フィールドが必須

Claude Code は `.mcp.json` 内の各サーバー定義に `"type"` フィールドを要求する。
`"type": "http"` を省略すると stdio モードとして解釈され、接続に失敗する。

```json
// NG: type がない
{
  "mcpServers": {
    "unityMCP": {
      "url": "http://127.0.0.1:8080/mcp"
    }
  }
}

// OK: type を明示
{
  "mcpServers": {
    "unityMCP": {
      "type": "http",
      "url": "http://127.0.0.1:8080/mcp"
    }
  }
}
```

### 2. Unity の Configure ボタンで `.mcp.json` が空に上書きされた

MCP for Unity の Unity エディタ内「Configure」ボタンを押すと、
既存の `.mcp.json` が stdio モード用の設定で上書きされることがある。
Claude Code で HTTP モードを使う場合は、Configure ボタンを使わず手動で `.mcp.json` を編集すること。

### 3. stdio モードと HTTP モードの混在による別プロセス問題

stdio モードの設定が残った状態で HTTP モードの設定も追加すると、
Claude Code が stdio 側のプロセスを起動してしまい、
Unity 側の HTTP サーバーとは別のプロセスに接続する状態になる。
結果として Unity エディタとの疎通ができない。

**なぜ別プロセスになるのか**:
Claude Code は `.mcp.json` を読んで MCP サーバーを子プロセスとして起動する。
一方、Unity 側は MCP Window から独立して HTTP サーバーを起動するため、
両者は別プロセスとして動く。同じ MCP サーバーを共有するには、
`.mcp.json` を HTTP モードにして、Unity が起動した HTTP サーバーに
Claude Code 側も接続するように設定する必要がある。

**対策**: `.mcp.json` には HTTP モードの設定のみを記述し、stdio の設定は削除する。

### 4. Reconnect では設定再読込されず、Claude Code 完全再起動が必要

`.mcp.json` を修正した後、Claude Code の `/mcp` メニューから Reconnect しても
新しい設定は反映されない。設定変更後は Claude Code を完全に終了して再起動する必要がある。

## 最終的な `.mcp.json`

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

## 毎回の起動手順

1. **Unity を起動**（Dock またはターミナルから）
2. **MCP Window を開く**: Unity メニュー → `Window` → `MCP for Unity`
3. **Start Server** をクリックして HTTP サーバーを起動（ポート 8080）
4. **Claude Code を起動**: ターミナルで `claude` コマンド実行
5. **接続確認**: `/mcp` でステータスが connected / authenticated であることを確認

## トラブルシューティング

接続できないときの確認手順:

1. Unity の MCP Window で **Session Active** になっているか確認
2. ターミナルで `lsof -i :8080 | grep -v grep` を実行し、ポート 8080 が Listen されているか確認
3. Claude Code で `/mcp` を開き、接続状態（connected / authenticated）を確認
4. `/mcp` の Diagnostics でパースエラーが出ていないか確認
5. `.mcp.json` の書式を確認（`"type": "http"` が必須）
6. プロセスの並走確認:
   `ps aux | grep -i "mcp-for-unity\|uvx" | grep -v grep`
   → MCP サーバープロセスが複数走っていないか確認
   → stdio と http の両方が動いていたら設定の混在を疑う

## 動作確認テスト結果

- **テストA**: MCP 接続ステータス確認 → 成功（connected, 43 tools）
- **テストB**: Care シーンの GameObject 一覧取得 → 成功（10オブジェクト全取得）
- **テストC**: Cube 作成 → スキップ

## Install Skills について

Unity の MCP Window で「Install Skills」ボタンを押すと、
`~/.claude/skills/unity-mcp-skill/` に以下3ファイルが追加される:

- `SKILL.md` (282行) - ツール使い方・ベストプラクティス
- `references/tools-reference.md` (60KB)
- `references/workflows.md` (74KB)

これは Claude Code が Unity を操作する際に参照する追加コンテキスト。
接続設定とは別物のため、接続失敗時に Skills を削除しても解決はしない。
ただし Claude Code の Unity 操作精度向上には寄与する。

## 参考

- [MCP for Unity GitHub (CoplayDev)](https://github.com/CoplayDev/unity-mcp)
- [PyPI: mcpforunityserver](https://pypi.org/project/mcpforunityserver/)
- 関連 Issue: #20
