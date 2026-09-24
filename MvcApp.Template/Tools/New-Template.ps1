<#
.SYNOPSIS
    Regenerates the VS multi-project template tree from the MvcApp source into
    MvcApp.Template\ProjectTemplates\MvcAppMonolith.

.DESCRIPTION
    Copies every project, replaces the literal "MvcApp" token with the VS template
    parameter $safeprojectname$ (both in file contents and in the project file
    name via TargetFileName), sanitizes appsettings secrets, and emits a
    .vstemplate per project plus the root multi-project .vstemplate.

    Excluded from the template: obj, bin, .vs, .opencode, node_modules,
    wwwroot\lib (libman restores it on build), *.apk / wwwroot\downloads
    (IPTV site content), *.xlsx, *.user/*.suo/*.tmp, and machine-specific files
    (AGENTS.md, opencode.json, check_admin.sql, Videocapabilities).

    Run again after any source change, then rebuild the VSIX.
#>
[CmdletBinding()]
param(
    [string]$SourceRoot,
    [string]$TemplateDir,
    [string]$BaseName = 'MvcApp',
    [switch]$IncludeApk,
    [switch]$WithWizard
)

$ErrorActionPreference = 'Stop'

if (-not $SourceRoot) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if (-not $scriptPath) { $scriptPath = (Get-Location).Path }
    $SourceRoot = Split-Path (Split-Path (Split-Path $scriptPath -Parent) -Parent) -Parent
}
if (-not $TemplateDir) {
    $scriptPath = $MyInvocation.MyCommand.Path
    if (-not $scriptPath) { $scriptPath = (Get-Location).Path }
    $TemplateDir = Join-Path (Split-Path (Split-Path $scriptPath -Parent) -Parent) 'ProjectTemplates\MvcAppMonolith'
}

$textExtensions = @(
    '.cs', '.cshtml', '.razor', '.csproj', '.slnx', '.json', '.js', '.css', '.scss',
    '.sass', '.txt', '.md', '.xml', '.config', '.sql', '.html', '.htm', '.gitignore',
    '.editorconfig', '.map', '.ts', '.jsx', '.tsx', '.pubxml', '.props', '.targets',
    '.resx', '.xaml', '.csproj.user'
)
$binaryExtensions = @(
    '.png', '.jpg', '.jpeg', '.gif', '.ico', '.svg', '.woff', '.woff2', '.eot',
    '.ttf', '.otf', '.apk', '.pdf', '.xlsx', '.zip'
)

$excludeDirNames = @('obj', 'bin', '.vs', '.opencode', 'node_modules', '.git', 'logs')

function Test-IsTextFile([string]$path) {
    $ext = [System.IO.Path]::GetExtension($path).ToLowerInvariant()
    if ($binaryExtensions -contains $ext) { return $false }
    if ($textExtensions -contains $ext) { return $true }
    return $false
}

function Get-RelPath([string]$full, [string]$root) {
    return $full.Substring($root.Length).TrimStart('\', '/')
}

function Get-ExcludedRelative([string]$full, [string]$root, [bool]$includeApk) {
    $rel = Get-RelPath $full $root
    $seg = $rel -split '[\\/]'
    foreach ($part in $seg) {
        if ($excludeDirNames -contains $part) { return $true }
        # user-upload photo folders are GUID-named; never ship them in a template
        if ($part -match '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$') { return $true }
    }
    for ($i = 0; $i -lt $seg.Length - 1; $i++) {
        if ($seg[$i] -eq 'wwwroot' -and $seg[$i + 1] -eq 'lib') { return $true }
        if ($seg[$i] -eq 'images' -and $seg[$i + 1] -eq 'ads') { return $true }
    }
    if ($rel -match '\\wwwroot\\downloads(\\|$)') { return $true }
    return $false
}

function Get-ExcludedFile([string]$name) {
    if ($name -in @('Videocapabilities', 'check_admin.sql', 'opencode.json', 'AGENTS.md')) { return $true }
    if ($name -like '*.user' -or $name -like '*.suo' -or $name -like '*.tmp') { return $true }
    if ($name -like '*.xlsx') { return $true }
    if ($name -like '*.apk' -and -not $IncludeApk) { return $true }
    return $false
}

function ConvertTo-Utf8NoBom([string]$content) {
    $enc = New-Object System.Text.UTF8Encoding($false)
    return ,$enc.GetBytes($content)
}

# Sanitize real credentials out of all text files (keeps structure/sections).
function ConvertTo-SanitizedAppSettings([string]$content) {
    $map = [ordered]@{
        'Uid=swan3344;Pwd=stevenP@2025www;'      = 'Uid=yourusername;Pwd=yourpassword;'
        'User=swan3344;Password=stevenP@2025www;' = 'User=yourusername;Password=yourpassword;'
        'Database=Identity_db'              = 'Database=$safeprojectname$_Identity'
        'Database=Localisation_db'          = 'Database=$safeprojectname$_Localisation'
        'admin@frenzyzone.com'              = 'admin@yourdomain.com'
        'svr.frenzyzone.com'                = 'svr.yourdomain.com'
        'Electro@2013'                      = 'yourpassword'
        'AWuMuZZDEaAerszlAAryuePRbFQrUZgmr-AhaW0yx8p22byAZAXLELNvIO2hMJZKSQCWdp_t_eeDyInt' = 'your-paypal-client-id'
        'EFccSwsaD2ltOcJeVyew5KYrI4zBDFZ77B8uF0qt3zO_Hli85enTOdfc_Xu5B7RaT9fnuJOVE4n4kkOJ' = 'your-paypal-client-secret'
        'd849b26678864886ae628510db90ba07'  = 'yourApiKey'
        '0aa94010-9c34-4e77-7822-c40707fa5e83:fx' = 'yourAuthKey'
        'payment@tvquebec.com'              = 'admin@yourdomain.com'
        'admin@tvquebec.com'                = 'admin@yourdomain.com'
    }
    foreach ($k in $map.Keys) {
        $content = $content.Replace($k, $map[$k])
    }
    return $content
}

# When -WithWizard is set, replace sanitized placeholder text with $token$ placeholders
# so the VS template wizard can inject user-chosen values at project creation time.
function ConvertTo-Tokenized([string]$content) {
    # --- SmtpSettings (unique keys) ---
    $content = $content.Replace('"From": "admin@yourdomain.com"', '"From": "$smtpfrom$"')
    $content = $content.Replace('"Host": "svr.yourdomain.com"', '"Host": "$smtpserver$"')
    $content = $content.Replace('"Port": 587', '"Port": $smtpport$')
    $content = $content.Replace('"Username": "admin@yourdomain.com"', '"Username": "$smtpuser$"')
    # --- Connection strings ---
    $content = $content.Replace('Uid=yourusername;Pwd=yourpassword;', 'Uid=$dbuid$;Pwd=$dbpwd$;')
    $content = $content.Replace('Server=localhost;Database=$safeprojectname$_logs;User=yourusername;Password=yourpassword;',
        'Server=$dbserver$;Database=$safeprojectname$_logs;User=$dbuid$;Pwd=$dbpwd$;')
    $content = $content.Replace('Database=$safeprojectname$_Identity', 'Database=$identitydbname$')
    $content = $content.Replace('Database=$safeprojectname$_Localisation', 'Database=$localisationdbname$')
    # --- Kestrel ---
    $content = $content.Replace('"Url": "http://localhost:9001"', '"Url": "http://localhost:$httpport$"')
    # --- Encryption key ---
    $content = $content.Replace('your-very-secure-encryption-key-here-32-chars', '$encrypkey$')
    # --- PayPal ---
    $content = $content.Replace('"PayPalMeUsername": "spweb063"', '"PayPalMeUsername": "$paypalmeuser$"')
    $content = $content.Replace('your-paypal-client-id', '$paypalclientid$')
    $content = $content.Replace('your-paypal-client-secret', '$paypalclientsecret$')
    # --- External services ---
    $content = $content.Replace('"ApiKey": "yourApiKey"', '"ApiKey": "$geolocationkey$"')
    $content = $content.Replace('"AuthKey": "yourAuthKey"', '"AuthKey": "$deepakey$"')
    # --- Branding ---
    $content = $content.Replace('"SiteName": "$safeprojectname$.Web"', '"SiteName": "$sitename$"')
    # --- Admin username (before generic email replacement) ---
    $content = $content.Replace('"Username": "admin"', '"Username": "$adminuser$"')
    # --- Password (both SmtpSettings and Administrator — same token) ---
    $content = $content.Replace('"Password": "yourpassword"', '"Password": "$adminpassword$"')
    # --- Generic email (catches Interact.Email, AdminEmail, Administrator.User) ---
    $content = $content.Replace('admin@yourdomain.com', '$adminemail$')
    # --- launchSettings.json ports ---
    $content = $content.Replace('"applicationUrl": "http://localhost:5106"', '"applicationUrl": "http://localhost:$httpport$"')
    $content = $content.Replace('"applicationUrl": "https://localhost:7190;http://localhost:5106"',
        '"applicationUrl": "https://localhost:7$httpport$;http://localhost:$httpport$"')
    # --- Seeder.cs fallbacks ---
    $content = $content.Replace('?? "admin@yourdomain.com"', '?? "$adminemail$"')
    $content = $content.Replace('?? "yourpassword"', '?? "$adminpassword$"')
    return $content
}

function ConvertTo-TemplatedContent([string]$content) {
    return $content.Replace($BaseName, '$safeprojectname$')
}

function Copy-ProjectIntoTemplate([string]$projectDir, [string]$destDir) {
    $name = Split-Path $projectDir -Leaf
    $suffix = $name.Substring($BaseName.Length)
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null

    $files = Get-ChildItem $projectDir -Recurse -File -Force 2>$null
    $items = @()
    foreach ($f in $files) {
        $rel = Get-RelPath $f.FullName $projectDir
        if (Get-ExcludedRelative $f.FullName $projectDir $IncludeApk) { continue }
        if (Get-ExcludedFile $f.Name) { continue }

        $target = Join-Path $destDir $rel
        $targetDir = Split-Path $target -Parent
        if (-not (Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force | Out-Null }

        if (Test-IsTextFile $f.FullName) {
            try {
                $content = [System.IO.File]::ReadAllText($f.FullName)
                $content = ConvertTo-TemplatedContent $content
                $content = ConvertTo-SanitizedAppSettings $content
                if ($WithWizard) { $content = ConvertTo-Tokenized $content }
                [System.IO.File]::WriteAllBytes($target, (ConvertTo-Utf8NoBom $content))
            } catch {
                throw "Failed on text file $rel : $($_.Exception.Message)"
            }
            $replace = 'true'
        } else {
            Copy-Item -LiteralPath $f.FullName -Destination $target -Force
            $replace = 'false'
        }

        $item = "        <ProjectItem ReplaceParameters=""$replace"">$rel</ProjectItem>"
        $items += $item
    }

    $projFile = "$name.csproj"
    $tplName  = "$name.vstemplate"
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
    [void]$sb.AppendLine('<VSTemplate Version="3.0.0" Type="Project" xmlns="http://schemas.microsoft.com/developer/vstemplate/2005">')
    [void]$sb.AppendLine('  <TemplateData>')
    [void]$sb.AppendLine("    <Name>`$safeprojectname`$$suffix</Name>")
    [void]$sb.AppendLine("    <Description>MvcApp $name project</Description>")
    [void]$sb.AppendLine('    <ProjectType>CSharp</ProjectType>')
    [void]$sb.AppendLine('    <ProjectSubType></ProjectSubType>')
    [void]$sb.AppendLine("    <DefaultName>`$safeprojectname`$$suffix</DefaultName>")
    [void]$sb.AppendLine('    <CreateInPlace>true</CreateInPlace>')
    [void]$sb.AppendLine('    <ProvideDefaultName>true</ProvideDefaultName>')
    [void]$sb.AppendLine('    <Hidden>true</Hidden>')
    [void]$sb.AppendLine('  </TemplateData>')
    [void]$sb.AppendLine('  <TemplateContent>')
    [void]$sb.AppendLine("    <Project File=""$projFile"" TargetFileName=""`$safeprojectname`$$suffix.csproj"" ReplaceParameters=""true"" />")
    foreach ($it in $items) { [void]$sb.AppendLine($it) }
    [void]$sb.AppendLine('  </TemplateContent>')
    [void]$sb.AppendLine('</VSTemplate>')

    $tplPath = Join-Path $destDir $tplName
    [System.IO.File]::WriteAllBytes($tplPath, (ConvertTo-Utf8NoBom $sb.ToString()))
    Write-Host "  $name -> $($items.Count) files"
}

# ---------- main ----------
Write-Host "Regenerating template into $TemplateDir"
if (Test-Path $TemplateDir) { Remove-Item -LiteralPath $TemplateDir -Recurse -Force }
New-Item -ItemType Directory -Path $TemplateDir -Force | Out-Null

$projectDirs = Get-ChildItem $SourceRoot -Directory | Where-Object {
    $_.Name -ne "$BaseName.Template" -and
    $_.Name -like "$BaseName*" -and (Test-Path (Join-Path $_.FullName "$($_.Name).csproj"))
}

$links = @()
foreach ($proj in $projectDirs) {
    $suffix = $proj.Name.Substring($BaseName.Length)
    $dest = Join-Path $TemplateDir $proj.Name
    Copy-ProjectIntoTemplate $proj.FullName $dest
    $links += "      <ProjectTemplateLink ProjectName=""`$safeprojectname`$$suffix"" CopyParameters=""true"">$($proj.Name)/$($proj.Name).vstemplate</ProjectTemplateLink>"
}

# root vstemplate
$rootTpl = Join-Path $TemplateDir "$BaseName.vstemplate"
$sb2 = New-Object System.Text.StringBuilder
[void]$sb2.AppendLine('<?xml version="1.0" encoding="utf-8"?>')
[void]$sb2.AppendLine('<VSTemplate Version="3.0.0" Type="ProjectGroup" xmlns="http://schemas.microsoft.com/developer/vstemplate/2005">')
[void]$sb2.AppendLine('  <TemplateData>')
[void]$sb2.AppendLine('    <Name>MvcApp Modular Monolith (ASP.NET Core 10)</Name>')
[void]$sb2.AppendLine('    <Description>Multi-project modular monolith: ASP.NET Core 10 MVC, Identity, MySQL, SignalR and 10 feature modules. Projects are named with your chosen prefix (e.g. MyApp.Core, MyApp.Web).</Description>')
    [void]$sb2.AppendLine('    <ProjectType>CSharp</ProjectType>')
    [void]$sb2.AppendLine('    <LanguageTag>C#</LanguageTag>')
    [void]$sb2.AppendLine('    <PlatformTag>Windows</PlatformTag>')
    [void]$sb2.AppendLine('    <ProjectTypeTag>Web</ProjectTypeTag>')
    [void]$sb2.AppendLine('    <SortOrder>1000</SortOrder>')
[void]$sb2.AppendLine('    <DefaultName>MvcApp</DefaultName>')
[void]$sb2.AppendLine('    <CreateNewFolder>true</CreateNewFolder>')
[void]$sb2.AppendLine('    <ProvideDefaultName>true</ProvideDefaultName>')
[void]$sb2.AppendLine('    <LocationField>Enabled</LocationField>')
[void]$sb2.AppendLine('    <PromptForSaveOnCreation>true</PromptForSaveOnCreation>')
[void]$sb2.AppendLine('    <EnableLocationBrowseButton>true</EnableLocationBrowseButton>')
[void]$sb2.AppendLine('  </TemplateData>')
[void]$sb2.AppendLine('  <TemplateContent>')
[void]$sb2.AppendLine('    <ProjectTemplateLinks>')
foreach ($l in $links) { [void]$sb2.AppendLine($l) }
[void]$sb2.AppendLine('    </ProjectTemplateLinks>')
[void]$sb2.AppendLine('  </TemplateContent>')
if ($WithWizard) {
    [void]$sb2.AppendLine('  <WizardExtension>')
    [void]$sb2.AppendLine('    <Assembly>TemplateWizard, Version=1.0.0.0, Culture=neutral, PublicKeyToken=bdc294accda97181</Assembly>')
    [void]$sb2.AppendLine('    <FullClassName>TemplateWizard.CustomProjectWizard</FullClassName>')
    [void]$sb2.AppendLine('  </WizardExtension>')
}
[void]$sb2.AppendLine('</VSTemplate>')
[System.IO.File]::WriteAllBytes($rootTpl, (ConvertTo-Utf8NoBom $sb2.ToString()))

$count = (Get-ChildItem $TemplateDir -Recurse -File).Count
$size = (Get-ChildItem $TemplateDir -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Host ("Done. {0} files, {1:N1} MB" -f $count, ($size / 1MB))
