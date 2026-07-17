# VS Code 开工提示词

请先阅读：

- AGENTS.md
- docs/DEVELOPMENT_MANUAL.md
- config/appsettings.example.json

先不要实现整个项目。

请完成 M0 和 M1：

1. 初始化 .NET 8 WPF 解决方案；
2. 集成 Microsoft.Web.WebView2；
3. 建立依赖注入、配置和日志；
4. 使用 `%LOCALAPPDATA%\JellyfinPotPlayerShell\WebView2` 作为持久化用户目录；
5. 设置页可以输入 Jellyfin Server URL；
6. 主窗口加载 Jellyfin Web；
7. 重启应用后保留 Jellyfin 登录；
8. 不修改官方 Jellyfin；
9. 不实现 PotPlayer 按钮；
10. 修改前列出文件计划；
11. 完成后运行 dotnet build 和 dotnet test；
12. 修复所有错误后再结束。
