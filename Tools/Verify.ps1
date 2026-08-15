[CmdletBinding()]
param(
    [ValidateSet('Fast', 'Full', 'Web')]
    [string]$Tier = 'Fast',

    [switch]$StaticOnly,
    [switch]$SkipBrowserSmoke,
    [string]$UnityPath,
    [string]$ArtifactsRoot = 'Artifacts/Verification'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$script:Steps = [System.Collections.Generic.List[object]]::new()
$script:VerificationStatus = 'Passed'
$script:ExitCode = 0

function Add-StepResult {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][ValidateSet('Passed', 'Failed', 'Skipped')][string]$Status,
        [string]$Detail = '',
        [string[]]$Artifacts = @()
    )

    $script:Steps.Add([ordered]@{
        name      = $Name
        status    = $Status
        detail    = $Detail
        artifacts = $Artifacts
    })
}

function Write-Stage {
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Fail-Verification {
    param(
        [Parameter(Mandatory)][string]$Message,
        [int]$Code = 1
    )

    $script:VerificationStatus = 'Failed'
    $script:ExitCode = $Code
    throw $Message
}

function Get-ProjectRoot {
    $candidate = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
    if (-not (Test-Path -LiteralPath (Join-Path $candidate 'ProjectSettings/ProjectVersion.txt'))) {
        throw "Unity project root was not found above $PSScriptRoot."
    }

    return $candidate
}

function Get-UnityVersion {
    param([Parameter(Mandatory)][string]$ProjectRoot)

    $versionFile = Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt'
    $line = Get-Content -Encoding UTF8 -LiteralPath $versionFile | Where-Object { $_ -like 'm_EditorVersion:*' } | Select-Object -First 1
    if (-not $line) {
        throw "m_EditorVersion was not found in $versionFile."
    }

    return ($line -split ':', 2)[1].Trim()
}

function Resolve-UnityEditor {
    param(
        [Parameter(Mandatory)][string]$Version,
        [string]$ExplicitPath
    )

    $candidates = [System.Collections.Generic.List[string]]::new()
    if ($ExplicitPath) {
        $candidates.Add($ExplicitPath)
    }
    if ($env:UNITY_EDITOR_PATH) {
        $candidates.Add($env:UNITY_EDITOR_PATH)
    }

    $isWindowsPlatform = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
        [System.Runtime.InteropServices.OSPlatform]::Windows)
    $isMacPlatform = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
        [System.Runtime.InteropServices.OSPlatform]::OSX)

    if ($isWindowsPlatform) {
        $candidates.Add("C:\Program Files\Unity\Hub\Editor\$Version\Editor\Unity.exe")
    }
    elseif ($isMacPlatform) {
        $candidates.Add("/Applications/Unity/Hub/Editor/$Version/Unity.app/Contents/MacOS/Unity")
    }
    else {
        $candidates.Add("$HOME/Unity/Hub/Editor/$Version/Editor/Unity")
    }

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            return [System.IO.Path]::GetFullPath($candidate)
        }
    }

    throw "Unity $Version was not found. Pass -UnityPath or set UNITY_EDITOR_PATH."
}

function Test-ProjectLocked {
    param([Parameter(Mandatory)][string]$ProjectRoot)

    return Test-Path -LiteralPath (Join-Path $ProjectRoot 'Temp/UnityLockfile')
}

function Get-RelativePathSafe {
    param(
        [Parameter(Mandatory)][string]$BasePath,
        [Parameter(Mandatory)][string]$TargetPath
    )

    $normalizedBase = [System.IO.Path]::GetFullPath($BasePath).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $normalizedTarget = [System.IO.Path]::GetFullPath($TargetPath)
    $baseUri = [System.Uri]::new($normalizedBase)
    $targetUri = [System.Uri]::new($normalizedTarget)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString())
}

function Get-TextFileSet {
    param([Parameter(Mandatory)][string]$ProjectRoot)

    $files = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    $rootFiles = @('AGENTS.md', '.gitignore')
    foreach ($relative in $rootFiles) {
        $path = Join-Path $ProjectRoot $relative
        if (Test-Path -LiteralPath $path -PathType Leaf) {
            $files.Add((Get-Item -LiteralPath $path))
        }
    }

    foreach ($relativeDirectory in @('Docs', 'Assets/Game', '.agents/skills', 'Tools')) {
        $path = Join-Path $ProjectRoot $relativeDirectory
        if (-not (Test-Path -LiteralPath $path -PathType Container)) {
            continue
        }

        Get-ChildItem -LiteralPath $path -Recurse -File | Where-Object {
            $_.Extension -in @('.md', '.asmdef', '.yaml', '.yml', '.json', '.ps1', '.cs', '.js', '.mjs')
        } | ForEach-Object { $files.Add($_) }
    }

    return $files
}

function Test-StaticContracts {
    param([Parameter(Mandatory)][string]$ProjectRoot)

    $issues = [System.Collections.Generic.List[string]]::new()
    $utf8Strict = [System.Text.UTF8Encoding]::new($false, $true)
    foreach ($file in (Get-TextFileSet -ProjectRoot $ProjectRoot)) {
        $relative = Get-RelativePathSafe -BasePath $ProjectRoot -TargetPath $file.FullName
        try {
            $content = $utf8Strict.GetString([System.IO.File]::ReadAllBytes($file.FullName))
        }
        catch {
            $issues.Add("Invalid UTF-8: $relative")
            continue
        }

        if ($content.Contains([char]0xFFFD)) {
            $issues.Add("Replacement character found: $relative")
        }
        if ($content -match '(?m)[ \t]+$') {
            $issues.Add("Trailing whitespace: $relative")
        }
    }

    $agentsPath = Join-Path $ProjectRoot 'AGENTS.md'
    if (-not (Test-Path -LiteralPath $agentsPath)) {
        $issues.Add('Missing AGENTS.md')
    }
    elseif ((Get-Item -LiteralPath $agentsPath).Length -ge 32768) {
        $issues.Add('AGENTS.md is at or above the default 32 KiB combined instruction limit.')
    }

    $expectedSkillNames = @(
        'bombswap-gameplay-change',
        'bombswap-content-authoring',
        'bombswap-webgl-verify',
        'bombswap-playtest-review'
    )
    foreach ($skillName in $expectedSkillNames) {
        $expectedSkillPath = Join-Path $ProjectRoot ".agents/skills/$skillName/SKILL.md"
        if (-not (Test-Path -LiteralPath $expectedSkillPath -PathType Leaf)) {
            $issues.Add("Missing required project skill: $skillName")
        }
    }

    $requiredToolPaths = @(
        'Tools/Verify.ps1',
        'Tools/WebGLSmoke.mjs',
        'Tools/ArmoredWebGLSmoke.mjs',
        'Tools/DirectionalLineWebGLSmoke.mjs',
        'Tools/GamepadWebGLSmoke.mjs',
        'Tools/WebGLStaticServer.mjs',
        'Tools/WebGLStaticServerTests.mjs',
        'Tools/WebGLTemplateTests.mjs',
        'Tools/ServeWebGL.mjs',
        'Tools/PlaytestLogAnalyzer.mjs',
        'Tools/AnalyzePlaytestLog.mjs',
        'Tools/PlaytestLogAnalyzerTests.mjs'
    )
    foreach ($relativeToolPath in $requiredToolPaths) {
        if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $relativeToolPath) -PathType Leaf)) {
            $issues.Add("Missing required verification tool: $relativeToolPath")
        }
    }

    $skillFiles = @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot '.agents/skills') -Recurse -Filter 'SKILL.md' -ErrorAction SilentlyContinue)
    foreach ($skillFile in $skillFiles) {
        $content = Get-Content -Raw -Encoding UTF8 -LiteralPath $skillFile.FullName
        $relative = Get-RelativePathSafe -BasePath $ProjectRoot -TargetPath $skillFile.FullName
        if ($content -notmatch '(?s)\A---\s*\r?\nname:\s*([a-z0-9-]+)\s*\r?\ndescription:\s*(.+?)\r?\n---') {
            $issues.Add("Invalid skill frontmatter: $relative")
            continue
        }
        if ($Matches[1] -ne $skillFile.Directory.Name) {
            $issues.Add("Skill name does not match its folder: $relative")
        }
        if ($content -match '\[TODO:|Structuring This Skill') {
            $issues.Add("Skill still contains template TODO text: $relative")
        }
        $openAiYaml = Join-Path $skillFile.Directory.FullName 'agents/openai.yaml'
        if (-not (Test-Path -LiteralPath $openAiYaml -PathType Leaf)) {
            $issues.Add("Missing agents/openai.yaml: $relative")
        }
    }

    $markdownFiles = @(Get-Item -LiteralPath $agentsPath -ErrorAction SilentlyContinue) + @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Docs') -Recurse -Filter '*.md')
    foreach ($file in $markdownFiles) {
        if (-not $file) { continue }
        $content = Get-Content -Raw -Encoding UTF8 -LiteralPath $file.FullName
        $links = [regex]::Matches($content, '(?<!\!)\[[^\]]+\]\(([^)]+)\)')
        foreach ($link in $links) {
            $target = $link.Groups[1].Value.Trim()
            if ($target -match '^(https?://|mailto:|#)') { continue }
            if ($target.StartsWith('<') -and $target.EndsWith('>')) {
                $target = $target.Substring(1, $target.Length - 2)
            }
            $target = ($target -split '#', 2)[0]
            if ([string]::IsNullOrWhiteSpace($target)) { continue }
            $resolved = Join-Path $file.DirectoryName $target
            if (-not (Test-Path -LiteralPath $resolved)) {
                $relative = Get-RelativePathSafe -BasePath $ProjectRoot -TargetPath $file.FullName
                $issues.Add("Broken Markdown link: $relative -> $target")
            }
        }
    }

    $requiredAsmdefPaths = @(
        'Assets/Game/Core/BombSwap.Core.asmdef',
        'Assets/Game/BombSwap.Unity.asmdef',
        'Assets/Game/Editor/BombSwap.Editor.asmdef',
        'Assets/Game/Tests/EditMode/BombSwap.Core.Tests.asmdef',
        'Assets/Game/Tests/PlayMode/BombSwap.Unity.Tests.asmdef'
        'Assets/Game/Tests/EditorHarness/BombSwap.ConnectedTestHarness.asmdef'
    )
    foreach ($relativeAsmdefPath in $requiredAsmdefPaths) {
        if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $relativeAsmdefPath) -PathType Leaf)) {
            $issues.Add("Missing required assembly definition: $relativeAsmdefPath")
        }
    }

    $projectAsmdefs = @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets/Game') -Recurse -Filter '*.asmdef')
    $knownAssemblyNames = @{}
    foreach ($root in @('Assets', 'Library/PackageCache')) {
        $path = Join-Path $ProjectRoot $root
        if (-not (Test-Path -LiteralPath $path)) { continue }
        foreach ($asmdefFile in (Get-ChildItem -LiteralPath $path -Recurse -Filter '*.asmdef')) {
            try {
                $asmdef = Get-Content -Raw -Encoding UTF8 -LiteralPath $asmdefFile.FullName | ConvertFrom-Json
                if ($asmdef.name) { $knownAssemblyNames[$asmdef.name] = $true }
            }
            catch {
                if ($asmdefFile.FullName.StartsWith((Join-Path $ProjectRoot 'Assets/Game'))) {
                    $issues.Add("Invalid asmdef JSON: $(Get-RelativePathSafe -BasePath $ProjectRoot -TargetPath $asmdefFile.FullName)")
                }
            }
        }
    }

    foreach ($asmdefFile in $projectAsmdefs) {
        try {
            $asmdef = Get-Content -Raw -Encoding UTF8 -LiteralPath $asmdefFile.FullName | ConvertFrom-Json
            foreach ($reference in @($asmdef.references)) {
                if ($reference -notmatch '^GUID:' -and -not $knownAssemblyNames.ContainsKey($reference)) {
                    $issues.Add("Unresolved asmdef reference: $($asmdef.name) -> $reference")
                }
            }
        }
        catch {
            # Invalid JSON is reported above.
        }
    }

    $coreAsmdefPath = Join-Path $ProjectRoot 'Assets/Game/Core/BombSwap.Core.asmdef'
    if (-not (Test-Path -LiteralPath $coreAsmdefPath)) {
        $issues.Add('Missing BombSwap.Core.asmdef')
    }
    else {
        $coreAsmdef = Get-Content -Raw -Encoding UTF8 -LiteralPath $coreAsmdefPath | ConvertFrom-Json
        if (-not $coreAsmdef.noEngineReferences) {
            $issues.Add('BombSwap.Core must set noEngineReferences=true.')
        }
        if (@($coreAsmdef.references).Count -ne 0) {
            $issues.Add('BombSwap.Core must not reference other assemblies.')
        }
    }

    $corePath = Join-Path $ProjectRoot 'Assets/Game/Core'
    $coreScripts = @(Get-ChildItem -LiteralPath $corePath -Recurse -Filter '*.cs' -ErrorAction SilentlyContinue)
    foreach ($script in $coreScripts) {
        $content = Get-Content -Raw -Encoding UTF8 -LiteralPath $script.FullName
        if ($content -match '(?m)^\s*using\s+(UnityEngine|UnityEditor|UnityEngine\.InputSystem)\s*;') {
            $issues.Add("Forbidden Unity namespace in Core: $(Get-RelativePathSafe -BasePath $ProjectRoot -TargetPath $script.FullName)")
        }
        if ($content -match '\b(Time\.(time|deltaTime|fixedDeltaTime)|UnityEngine\.Random|Task\.Run|Thread\b|System\.Net\.Sockets|Reflection\.Emit)') {
            $issues.Add("Forbidden authority or WebGL API in Core: $(Get-RelativePathSafe -BasePath $ProjectRoot -TargetPath $script.FullName)")
        }
    }

    if ($issues.Count -gt 0) {
        $issues | ForEach-Object { Write-Error $_ -ErrorAction Continue }
        throw "Static verification failed with $($issues.Count) issue(s)."
    }

    return "Checked $($markdownFiles.Count) Markdown files, $($projectAsmdefs.Count) project asmdefs, and $($skillFiles.Count) project skills."
}

function Invoke-Unity {
    param(
        [Parameter(Mandatory)][string]$UnityEditor,
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$LogPath
    )

    $allArguments = @(
        '-batchmode',
        '-nographics',
        '-projectPath', $ProjectRoot,
        '-logFile', $LogPath
    ) + $Arguments

    & $UnityEditor @allArguments
    return $LASTEXITCODE
}

function Test-NUnitResult {
    param(
        [Parameter(Mandatory)][string]$ResultPath,
        [Parameter(Mandatory)][string]$AssemblyName
    )

    if (-not (Test-Path -LiteralPath $ResultPath -PathType Leaf)) {
        throw "Unity did not write the expected test result: $ResultPath"
    }

    [xml]$result = Get-Content -Raw -Encoding UTF8 -LiteralPath $ResultPath
    $root = $result.DocumentElement
    if (-not $root) {
        throw "The test result XML is empty: $ResultPath"
    }

    $failed = [int]$root.GetAttribute('failed')
    $inconclusive = [int]$root.GetAttribute('inconclusive')
    $skipped = [int]$root.GetAttribute('skipped')
    $passed = [int]$root.GetAttribute('passed')
    $total = [int]$root.GetAttribute('total')

    if ($total -eq 0) {
        throw "No tests were discovered in $AssemblyName. Add a harness smoke test before claiming this tier."
    }
    if ($failed -gt 0 -or $inconclusive -gt 0) {
        throw "$AssemblyName tests failed or were inconclusive (passed=$passed failed=$failed inconclusive=$inconclusive skipped=$skipped)."
    }

    return "${AssemblyName}: passed=$passed skipped=$skipped total=$total"
}

function Get-JsonArtifact {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label was not written: $Path"
    }

    try {
        return Get-Content -Raw -Encoding UTF8 -LiteralPath $Path | ConvertFrom-Json
    }
    catch {
        throw "$Label is not valid JSON: $Path ($($_.Exception.Message))"
    }
}

function Invoke-UnityTestAssembly {
    param(
        [Parameter(Mandatory)][string]$UnityEditor,
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][ValidateSet('editmode', 'playmode')][string]$Platform,
        [Parameter(Mandatory)][string]$AssemblyName,
        [Parameter(Mandatory)][string]$ResultPath,
        [Parameter(Mandatory)][string]$LogPath
    )

    $exitCode = Invoke-Unity -UnityEditor $UnityEditor -ProjectRoot $ProjectRoot -LogPath $LogPath -Arguments @(
        '-runTests',
        '-testPlatform', $Platform,
        '-assemblyNames', $AssemblyName,
        '-testResults', $ResultPath,
        '-quit'
    )
    if ($exitCode -ne 0) {
        throw "Unity test process exited with code $exitCode. See $LogPath"
    }

    return Test-NUnitResult -ResultPath $ResultPath -AssemblyName $AssemblyName
}

function Invoke-BrowserSmoke {
    param(
        [Parameter(Mandatory)][string]$ProjectRoot,
        [Parameter(Mandatory)][string]$BuildPath,
        [Parameter(Mandatory)][string]$ArtifactDirectory
    )

    $node = Get-Command node -ErrorAction SilentlyContinue
    if (-not $node) {
        throw 'Node.js is required for browser smoke. Install Node.js or run Web with -SkipBrowserSmoke and report Partial.'
    }

    $scriptPath = Join-Path $ProjectRoot 'Tools/WebGLSmoke.mjs'
    $gamepadScriptPath = Join-Path $ProjectRoot 'Tools/GamepadWebGLSmoke.mjs'
    $serverTestPath = Join-Path $ProjectRoot 'Tools/WebGLStaticServerTests.mjs'
    $templateTestPath = Join-Path $ProjectRoot 'Tools/WebGLTemplateTests.mjs'
    $playtestAnalyzerPath = Join-Path $ProjectRoot 'Tools/AnalyzePlaytestLog.mjs'
    $playtestAnalyzerTestPath = Join-Path $ProjectRoot 'Tools/PlaytestLogAnalyzerTests.mjs'
    $reportPath = Join-Path $ArtifactDirectory 'browser-smoke.json'
    $playtestLogPath = Join-Path $ArtifactDirectory 'playtest-events.json'
    $gamepadReportPath = Join-Path $ArtifactDirectory 'gamepad-smoke.json'
    $gamepadScreenshotPath = Join-Path $ArtifactDirectory 'gamepad-paused.png'
    $browserLogPath = Join-Path $ArtifactDirectory 'browser-smoke.log'
    & $node.Source $templateTestPath *> $browserLogPath
    if ($LASTEXITCODE -ne 0) {
        throw "WebGL template tests failed with code $LASTEXITCODE. See $browserLogPath"
    }

    & $node.Source $serverTestPath *>> $browserLogPath
    if ($LASTEXITCODE -ne 0) {
        throw "WebGL static server tests failed with code $LASTEXITCODE. See $browserLogPath"
    }

    & $node.Source $playtestAnalyzerTestPath *>> $browserLogPath
    if ($LASTEXITCODE -ne 0) {
        throw "Playtest log analyzer tests failed with code $LASTEXITCODE. See $browserLogPath"
    }

    & $node.Source $scriptPath --buildPath $BuildPath --reportPath $reportPath *>> $browserLogPath
    if ($LASTEXITCODE -ne 0) {
        throw "Browser smoke failed with code $LASTEXITCODE. See $browserLogPath"
    }

    & $node.Source $playtestAnalyzerPath --input $playtestLogPath --outputDirectory $ArtifactDirectory *>> $browserLogPath
    if ($LASTEXITCODE -ne 0) {
        throw "Exported playtest log analysis failed with code $LASTEXITCODE. See $browserLogPath"
    }

    & $node.Source $gamepadScriptPath --buildPath $BuildPath --reportPath $gamepadReportPath --screenshotPath $gamepadScreenshotPath *>> $browserLogPath
    if ($LASTEXITCODE -ne 0) {
        throw "Virtual gamepad browser smoke failed with code $LASTEXITCODE. See $browserLogPath"
    }

    return "WebGL template/static server/analyzer tests, keyboard browser smoke, exported log analysis, and virtual gamepad browser smoke passed. Reports: $reportPath, $gamepadReportPath"
}

$projectRoot = Get-ProjectRoot
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$artifactRootAbsolute = if ([System.IO.Path]::IsPathRooted($ArtifactsRoot)) {
    [System.IO.Path]::GetFullPath($ArtifactsRoot)
} else {
    [System.IO.Path]::GetFullPath((Join-Path $projectRoot $ArtifactsRoot))
}
$runLabel = if ($StaticOnly) { 'static' } else { $Tier.ToLowerInvariant() }
$artifactDirectory = Join-Path $artifactRootAbsolute "$timestamp-$runLabel"
New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
$summaryPath = Join-Path $artifactDirectory 'summary.json'

$unityVersion = $null
$resolvedUnity = $null
$caughtError = $null

try {
    Write-Stage 'Static contracts'
    $staticDetail = Test-StaticContracts -ProjectRoot $projectRoot
    Add-StepResult -Name 'Static contracts' -Status 'Passed' -Detail $staticDetail

    if ($StaticOnly) {
        $script:VerificationStatus = 'StaticOnly'
        Add-StepResult -Name 'Unity compile' -Status 'Skipped' -Detail 'StaticOnly was requested; compilation and tests were not run.'
        if ($Tier -eq 'Web') {
            Add-StepResult -Name 'WebGL build' -Status 'Skipped' -Detail 'StaticOnly was requested.'
            Add-StepResult -Name 'Browser smoke' -Status 'Skipped' -Detail 'StaticOnly was requested.'
        }
    }
    else {
        if (Test-ProjectLocked -ProjectRoot $projectRoot) {
            Fail-Verification -Message 'This Unity project is already open (Temp/UnityLockfile exists). Close the Editor or validate through its connected Unity tools before running the batch harness.' -Code 3
        }

        $unityVersion = Get-UnityVersion -ProjectRoot $projectRoot
        $resolvedUnity = Resolve-UnityEditor -Version $unityVersion -ExplicitPath $UnityPath

        Write-Stage 'Unity compile and Editor validation'
        $compileLog = Join-Path $artifactDirectory 'unity-compile.log'
        $compileExit = Invoke-Unity -UnityEditor $resolvedUnity -ProjectRoot $projectRoot -LogPath $compileLog -Arguments @(
            '-executeMethod', 'BombSwap.Editor.Verification.CommandLineVerification.CompileAndValidate',
            '-bombswapArtifacts', $artifactDirectory,
            '-quit'
        )
        if ($compileExit -ne 0) {
            throw "Unity compile/validation exited with code $compileExit. See $compileLog"
        }
        $editorValidationPath = Join-Path $artifactDirectory 'editor-validation.json'
        $editorValidation = Get-JsonArtifact -Path $editorValidationPath -Label 'Editor validation report'
        if ($editorValidation.status -ne 'passed') {
            throw "Editor validation report did not pass. See $editorValidationPath"
        }
        Add-StepResult -Name 'Unity compile' -Status 'Passed' -Detail 'First-party assemblies compiled and Editor validators completed.' -Artifacts @($compileLog, $editorValidationPath)

        Write-Stage 'Core EditMode tests'
        $editModeResult = Join-Path $artifactDirectory 'editmode-results.xml'
        $editModeLog = Join-Path $artifactDirectory 'unity-editmode.log'
        $editModeDetail = Invoke-UnityTestAssembly -UnityEditor $resolvedUnity -ProjectRoot $projectRoot -Platform 'editmode' -AssemblyName 'BombSwap.Core.Tests' -ResultPath $editModeResult -LogPath $editModeLog
        Add-StepResult -Name 'Core EditMode tests' -Status 'Passed' -Detail $editModeDetail -Artifacts @($editModeResult, $editModeLog)

        if ($Tier -in @('Full', 'Web')) {
            Write-Stage 'First-party PlayMode tests'
            $playModeResult = Join-Path $artifactDirectory 'playmode-results.xml'
            $playModeLog = Join-Path $artifactDirectory 'unity-playmode.log'
            $playModeDetail = Invoke-UnityTestAssembly -UnityEditor $resolvedUnity -ProjectRoot $projectRoot -Platform 'playmode' -AssemblyName 'BombSwap.Unity.Tests' -ResultPath $playModeResult -LogPath $playModeLog
            Add-StepResult -Name 'First-party PlayMode tests' -Status 'Passed' -Detail $playModeDetail -Artifacts @($playModeResult, $playModeLog)
        }

        if ($Tier -eq 'Web') {
            Write-Stage 'Development WebGL build'
            $webBuildPath = Join-Path $artifactDirectory 'WebGLBuild'
            $webLogPath = Join-Path $artifactDirectory 'unity-webgl-build.log'
            $webExit = Invoke-Unity -UnityEditor $resolvedUnity -ProjectRoot $projectRoot -LogPath $webLogPath -Arguments @(
                '-buildTarget', 'WebGL',
                '-executeMethod', 'BombSwap.Editor.Verification.CommandLineVerification.BuildDevelopmentWebGL',
                '-bombswapArtifacts', $artifactDirectory,
                '-bombswapBuildPath', $webBuildPath,
                '-quit'
            )
            if ($webExit -ne 0) {
                throw "WebGL build exited with code $webExit. See $webLogPath"
            }
            $webBuildReportPath = Join-Path $artifactDirectory 'webgl-build-report.json'
            $webBuildReport = Get-JsonArtifact -Path $webBuildReportPath -Label 'WebGL build report'
            if ($webBuildReport.result -ne 'Succeeded') {
                throw "WebGL build report did not succeed. See $webBuildReportPath"
            }
            if (-not (Test-Path -LiteralPath (Join-Path $webBuildPath 'index.html') -PathType Leaf)) {
                throw "WebGL build did not produce index.html in $webBuildPath"
            }
            Add-StepResult -Name 'WebGL build' -Status 'Passed' -Detail "Development build: $webBuildPath" -Artifacts @($webLogPath, $webBuildReportPath)

            if ($SkipBrowserSmoke) {
                $script:VerificationStatus = 'Partial'
                $script:ExitCode = 2
                Add-StepResult -Name 'Browser smoke' -Status 'Skipped' -Detail 'Explicitly skipped. Web tier is partial and must not be reported as passed.'
            }
            else {
                Write-Stage 'Browser smoke'
                $browserDetail = Invoke-BrowserSmoke -ProjectRoot $projectRoot -BuildPath $webBuildPath -ArtifactDirectory $artifactDirectory
                Add-StepResult -Name 'Browser smoke' -Status 'Passed' -Detail $browserDetail -Artifacts @(
                    (Join-Path $artifactDirectory 'browser-smoke.json'),
                    (Join-Path $artifactDirectory 'playtest-events.json'),
                    (Join-Path $artifactDirectory 'playtest-log-summary.json'),
                    (Join-Path $artifactDirectory 'playtest-log-summary.md'),
                    (Join-Path $artifactDirectory 'gamepad-smoke.json'),
                    (Join-Path $artifactDirectory 'gamepad-paused.png'),
                    (Join-Path $artifactDirectory 'browser-smoke.log')
                )
            }
        }
    }
}
catch {
    $caughtError = $_.Exception.Message
    if ($script:VerificationStatus -ne 'Failed') {
        $script:VerificationStatus = 'Failed'
        $script:ExitCode = 1
    }
    Add-StepResult -Name 'Failure' -Status 'Failed' -Detail $caughtError
}
finally {
    $summary = [ordered]@{
        schemaVersion     = 1
        project           = 'BombSwap'
        requestedTier     = $Tier
        staticOnly        = [bool]$StaticOnly
        status            = $script:VerificationStatus
        exitCode          = $script:ExitCode
        startedAt         = $timestamp
        completedAt       = (Get-Date).ToString('o')
        projectRoot       = $projectRoot
        unityVersion      = $unityVersion
        unityPath         = $resolvedUnity
        artifacts         = $artifactDirectory
        error             = $caughtError
        steps             = $script:Steps
    }
    $summary | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 -LiteralPath $summaryPath

    Write-Host "`nVerification status: $($script:VerificationStatus)" -ForegroundColor $(if ($script:ExitCode -eq 0) { 'Green' } elseif ($script:ExitCode -eq 2) { 'Yellow' } else { 'Red' })
    Write-Host "Summary: $summaryPath"
}

exit $script:ExitCode
