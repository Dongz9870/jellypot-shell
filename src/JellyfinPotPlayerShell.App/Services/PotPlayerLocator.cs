using System.IO;
using System.Runtime.InteropServices;
using JellyfinPotPlayerShell.Core.Playback;
using Microsoft.Win32;

namespace JellyfinPotPlayerShell.App.Services;

public sealed class PotPlayerLocator : IPotPlayerLocator
{
    private const string AppPathsKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\PotPlayerMini64.exe";

    public string? Locate(string? configuredPath)
    {
        return PotPlayerCandidateSelector.FindFirstValid(
            EnumerateCandidates(configuredPath));
    }

    private static IEnumerable<string?> EnumerateCandidates(string? configuredPath)
    {
        yield return configuredPath;

        foreach (var registryPath in ReadRegistryCandidates())
        {
            yield return registryPath;
        }

        foreach (var commonPath in GetCommonInstallCandidates())
        {
            yield return commonPath;
        }

        foreach (var pathCandidate in GetPathEnvironmentCandidates())
        {
            yield return pathCandidate;
        }

        foreach (var shortcutTarget in GetStartMenuShortcutTargets())
        {
            yield return shortcutTarget;
        }
    }

    private static IEnumerable<string> ReadRegistryCandidates()
    {
        var results = new List<string>();
        var hives = new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine };
        var views = new[] { RegistryView.Registry64, RegistryView.Registry32 };

        foreach (var hive in hives)
        {
            foreach (var view in views)
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var appPathKey = baseKey.OpenSubKey(AppPathsKey);
                    if (appPathKey?.GetValue(null) is string executablePath)
                    {
                        results.Add(executablePath);
                    }
                }
                catch (Exception exception) when (
                    exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                {
                    // A missing or inaccessible registry view is a normal detection miss.
                }
            }
        }

        return results;
    }

    private static IEnumerable<string> GetCommonInstallCandidates()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        };

        foreach (var root in roots.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            yield return Path.Combine(root, "DAUM", "PotPlayer", PotPlayerExecutable.FileName);
            yield return Path.Combine(root, "PotPlayer", PotPlayerExecutable.FileName);
            yield return Path.Combine(root, "Programs", "PotPlayer", PotPlayerExecutable.FileName);
            yield return Path.Combine(root, "Pure Codec", "x64", PotPlayerExecutable.FileName);
        }
    }

    private static IEnumerable<string> GetPathEnvironmentCandidates()
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            yield break;
        }

        foreach (var directory in pathValue.Split(
                     Path.PathSeparator,
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return Path.Combine(directory, PotPlayerExecutable.FileName);
        }
    }

    private static IEnumerable<string> GetStartMenuShortcutTargets()
    {
        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
        };

        foreach (var root in roots.Where(Directory.Exists))
        {
            IEnumerable<string> shortcuts;
            try
            {
                shortcuts = Directory.EnumerateFiles(
                    root,
                    "*PotPlayer*.lnk",
                    SearchOption.AllDirectories).Take(50).ToArray();
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var shortcut in shortcuts)
            {
                var target = TryResolveShortcut(shortcut);
                if (!string.IsNullOrWhiteSpace(target))
                {
                    yield return target;
                }
            }
        }
    }

    private static string? TryResolveShortcut(string shortcutPath)
    {
        object? shellObject = null;
        object? shortcutObject = null;

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return null;
            }

            shellObject = Activator.CreateInstance(shellType);
            if (shellObject is null)
            {
                return null;
            }

            dynamic shell = shellObject;
            shortcutObject = shell.CreateShortcut(shortcutPath);
            dynamic shortcut = shortcutObject;
            return shortcut.TargetPath as string;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (shortcutObject is not null && Marshal.IsComObject(shortcutObject))
            {
                Marshal.FinalReleaseComObject(shortcutObject);
            }

            if (shellObject is not null && Marshal.IsComObject(shellObject))
            {
                Marshal.FinalReleaseComObject(shellObject);
            }
        }
    }
}
