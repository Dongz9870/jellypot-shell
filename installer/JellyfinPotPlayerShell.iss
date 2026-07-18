#ifndef AppVersion
  #define AppVersion "0.8.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif

#ifndef PrerequisiteDir
  #define PrerequisiteDir "..\artifacts\prerequisites"
#endif

#ifndef InstallerOutputDir
  #define InstallerOutputDir "..\artifacts\installer"
#endif

#define AppName "Jellyfin PotPlayer Shell"
#define AppExeName "JellyfinPotPlayerShell.exe"
#define WebView2Bootstrapper "MicrosoftEdgeWebview2Setup.exe"

[Setup]
AppId={{8F3D7B1B-622F-4CA7-A2A8-E7B394A45D20}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=Jellyfin PotPlayer Shell Contributors
DefaultDirName={localappdata}\Programs\Jellyfin PotPlayer Shell
DefaultGroupName=Jellyfin PotPlayer Shell
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir={#InstallerOutputDir}
OutputBaseFilename=JellyfinPotPlayerShell-Setup-x64
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes
CloseApplications=yes
RestartApplications=no
CloseApplicationsFilter={#AppExeName}
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}
VersionInfoDescription={#AppName} x64 Installer
SetupIconFile=..\src\JellyfinPotPlayerShell.App\Assets\JellyPot.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "appsettings.example.json"; DestDir: "{app}\config"; Flags: ignoreversion
Source: "{#PrerequisiteDir}\{#WebView2Bootstrapper}"; Flags: dontcopy

[Icons]
Name: "{group}\Jellyfin PotPlayer Shell"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{group}\卸载 Jellyfin PotPlayer Shell"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Jellyfin PotPlayer Shell"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,Jellyfin PotPlayer Shell}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[Code]
const
  WebView2ClientKey = 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}';

function HasValidWebView2Version(RootKey: Integer): Boolean;
var
  Version: String;
begin
  Result := RegQueryStringValue(RootKey, WebView2ClientKey, 'pv', Version) and
    (Version <> '') and (Version <> '0.0.0.0');
end;

function IsWebView2RuntimeInstalled(): Boolean;
begin
  Result := HasValidWebView2Version(HKLM32) or
    HasValidWebView2Version(HKCU32) or
    HasValidWebView2Version(HKLM64) or
    HasValidWebView2Version(HKCU64);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Result := '';
  if IsWebView2RuntimeInstalled() then
    Exit;

  ExtractTemporaryFile('{#WebView2Bootstrapper}');
  if not Exec(
      ExpandConstant('{tmp}\{#WebView2Bootstrapper}'),
      '/silent /install',
      '',
      SW_HIDE,
      ewWaitUntilTerminated,
      ResultCode) then
  begin
    Result := '无法启动 Microsoft Edge WebView2 Runtime 安装程序。';
    Exit;
  end;

  if ResultCode = 3010 then
  begin
    NeedsRestart := True;
    Exit;
  end;

  if ResultCode <> 0 then
    Result := Format('Microsoft Edge WebView2 Runtime 安装失败，错误代码：%d。', [ResultCode]);
end;
