# MvcApp.Template (VSIX project template)

Packages the MvcApp modular monolith as a Visual Studio 2022 multi-project
template in a VSIX.

## Regenerate the template after source changes

```powershell
powershell -ExecutionPolicy Bypass -File MvcApp.Template\Tools\New-Template.ps1
```

The script copies every `MvcApp.*` project, replaces the literal `MvcApp` token
with the VS template parameter `$safeprojectname$` (contents + project file
name), sanitizes credentials in `appsettings.json` to placeholders, and emits a
`.vstemplate` per project plus the root multi-project template.

Excluded automatically: `obj`, `bin`, `.vs`, `.opencode`, `node_modules`,
`wwwroot\lib` (restored by libman during build), IPTV `wwwroot\downloads`/`*.apk`
(add `-IncludeApk` to keep them), `*.xlsx`, `*.user`/`*.suo`/`*.tmp`, and
machine-specific files (`AGENTS.md`, `opencode.json`, `check_admin.sql`,
`Videocapabilities`).

## Build the VSIX

```powershell
dotnet build MvcApp.Template\MvcApp.Template.csproj -c Release
```

Output: `MvcApp.Template\bin\Release\MvcApp.Template.vsix`

## Install

Double-click the `.vsix` (or `vsixinstaller.exe /q`) and restart Visual Studio.
Then File > New > Project > search "MvcApp" and enter the desired project name;
all 18 projects are created as `<name>.Core`, `<name>.Web`, etc.

## Notes

- Requires the .NET 10 SDK and an internet connection on first build (NuGet +
  libman restore).
- The hub state (`VideoChatHub`) is in-memory: single instance only unless you
  add a Redis backplane.
- The generated app uses a classic `.sln` (VS creates it on new-project). The
  `.slnx` in the repo is not part of the template.
