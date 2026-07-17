# Jellyfin PotPlayer Shell 开发资料包

按顺序阅读：

1. `docs/DEVELOPMENT_MANUAL.md`
2. `AGENTS.md`
3. `config/appsettings.example.json`
4. `VSCODE_START_PROMPT.md`

建议把整个文件夹复制到新仓库根目录，再让 VS Code 编码助手按里程碑开发。

本方案采用：

```text
WPF + WebView2 + 运行时 JavaScript 注入 + C# 调用 PotPlayer
```

目标是让普通用户只安装、打开一个应用，就能在 Jellyfin 原播放按钮旁点击黄色 PotPlayer 按钮。
