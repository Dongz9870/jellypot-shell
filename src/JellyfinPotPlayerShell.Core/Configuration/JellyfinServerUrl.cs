namespace JellyfinPotPlayerShell.Core.Configuration;

public static class JellyfinServerUrl
{
    public static bool TryNormalize(string? value, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "请输入 Jellyfin Server URL。";
            return false;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            error = "地址必须是完整的 http:// 或 https:// URL。";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            error = "服务器地址不能包含用户名或密码。";
            return false;
        }

        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
        {
            error = "服务器地址不能包含查询参数或片段。";
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        return true;
    }
}
