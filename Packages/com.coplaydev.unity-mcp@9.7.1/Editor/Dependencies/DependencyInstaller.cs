using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace MCPForUnity.Editor.Dependencies
{
    /// <summary>
    /// One-click dependency installer for uv + Python (via uv).
    /// Cross-platform: macOS opens Terminal with .sh, Windows opens PowerShell with .ps1.
    /// Idempotent: skips already-installed dependencies.
    /// </summary>
    public static class DependencyInstaller
    {
        /// <summary>
        /// Quick check: are any required dependencies still missing?
        /// </summary>
        public static bool IsInstallNeeded()
        {
            var result = DependencyManager.CheckAllDependencies();
            return !result.IsSystemReady;
        }

        /// <summary>
        /// Generate and launch platform-specific install script.
        /// Returns the temp script path for debugging.
        /// </summary>
        public static string RunInstall()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return RunMacOSInstall();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return RunWindowsInstall();
            throw new PlatformNotSupportedException(
                "Automatic installation is only supported on macOS and Windows.");
        }

        private static string RunMacOSInstall()
        {
            var script = @"#!/bin/bash
set -e

echo ""========================================""
echo "" MCP for Unity - Dependency Installer""
echo ""========================================""
echo """"

# 1. Install uv if missing
if command -v uv &>/dev/null; then
    echo ""[✓] uv already installed: $(uv --version)""
else
    echo ""[~] Installing uv package manager...""
    curl -LsSf https://astral.sh/uv/install.sh | sh
    # Source uv into current session so next steps can use it
    export PATH=""$HOME/.local/bin:$PATH""
    echo ""[✓] uv installed: $(uv --version)""
fi

echo """"

# 2. Install Python 3.12 via uv if missing or too old
PYTHON_OK=false
if command -v python3 &>/dev/null; then
    PY_VER=$(python3 --version 2>&1)
    echo ""  Detected: $PY_VER""
    if [[ $PY_VER =~ Python\ 3\.([0-9]+) ]]; then
        MINOR=""${BASH_REMATCH[1]}""
        if [ ""$MINOR"" -ge 10 ]; then
            PYTHON_OK=true
        fi
    fi
fi

if [ ""$PYTHON_OK"" = true ]; then
    echo ""[✓] Python 3.10+ already available""
else
    echo ""[~] Installing Python 3.12 via uv...""
    uv python install 3.12
    echo ""[✓] Python 3.12 installed""
fi

echo """"
echo ""========================================""
echo "" Installation complete!""
echo "" Close this window and click ""Refresh"" in Unity.""
echo ""========================================""
# Keep terminal open so user can read the result
echo """"
read -p ""Press Enter to close...""
";
            return WriteAndOpenScript(script, ".sh");
        }

        private static string RunWindowsInstall()
        {
            var script = @"Write-Host ""======================================= ""
Write-Host "" MCP for Unity - Dependency Installer""
Write-Host ""======================================= ""
Write-Host """"

# 1. Install uv if missing
if (Get-Command uv -ErrorAction SilentlyContinue) {
    Write-Host (""[✓] uv already installed: "" + (& uv --version))
} else {
    Write-Host ""[~] Installing uv package manager...""
    irm https://astral.sh/uv/install.ps1 | iex
    # Refresh PATH so subsequent steps see uv
    $env:Path = [Environment]::GetEnvironmentVariable('Path', 'User') + ';' + $env:Path
    Write-Host (""[✓] uv installed: "" + (& uv --version))
}

Write-Host """"

# 2. Install Python 3.12 via uv if missing or too old
$pythonOk = $false
$pyVer = & python3 --version 2>$null
if ($LASTEXITCODE -eq 0 -and $pyVer -match 'Python 3\.(\d+)') {
    $minor = [int]$Matches[1]
    Write-Host ""  Detected: $pyVer""
    if ($minor -ge 10) {
        $pythonOk = $true
    }
}

if ($pythonOk) {
    Write-Host ""[✓] Python 3.10+ already available""
} else {
    Write-Host ""[~] Installing Python 3.12 via uv...""
    & uv python install 3.12
    Write-Host ""[✓] Python 3.12 installed""
}

Write-Host """"
Write-Host ""======================================= ""
Write-Host "" Installation complete!""
Write-Host "" Close this window and click 'Refresh' in Unity.""
Write-Host ""======================================= ""
Read-Host ""`nPress Enter to close...""
";
            return WriteAndOpenScript(script, ".ps1");
        }

        private static string WriteAndOpenScript(string content, string extension)
        {
            // Use a stable-ish temp name so user can find it if needed
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var tempFile = Path.Combine(Path.GetTempPath(), $"mcp-unity-install-{timestamp}{extension}");
            File.WriteAllText(tempFile, content);

            if (extension == ".sh")
            {
                // Make executable
                using (var chmod = Process.Start("chmod", $"+x \"{tempFile}\""))
                {
                    chmod?.WaitForExit(5000);
                }
                // Open in Terminal.app
                Process.Start("open", $"-a Terminal \"{tempFile}\"");
            }
            else // .ps1
            {
                Process.Start("powershell.exe",
                    $"-ExecutionPolicy ByPass -File \"{tempFile}\"");
            }

            return tempFile;
        }
    }
}
