#!/usr/bin/env bash
# Verifies the project WITHOUT a Unity installation:
#  1. downloads Unity reference assemblies from NuGet (UnityEngine.Modules 2021.3, Unity3D.SDK 2021.1 for UnityEditor)
#  2. type-checks all runtime scripts and all editor scripts (uGUI is replaced by a signature stub)
#  3. runs the gameplay-logic unit tests + balance simulation
# Requires: .NET 8 SDK, curl, unzip.
set -euo pipefail
cd "$(dirname "$0")"
CACHE="${UNITY_REF_CACHE:-$PWD/.unityrefs}"
mkdir -p "$CACHE"
if [ ! -f "$CACHE/mods/lib/net45/UnityEngine.CoreModule.dll" ]; then
  curl -sSL -o "$CACHE/mods.nupkg" https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg
  (mkdir -p "$CACHE/mods" && cd "$CACHE/mods" && unzip -qo ../mods.nupkg)
fi
if [ ! -f "$CACHE/sdk/lib/UnityEditor.dll" ]; then
  curl -sSL -o "$CACHE/sdk.nupkg" https://api.nuget.org/v3-flatcontainer/unity3d.sdk/2021.1.14.1/unity3d.sdk.2021.1.14.1.nupkg
  (mkdir -p "$CACHE/sdk" && cd "$CACHE/sdk" && unzip -qo ../sdk.nupkg)
fi
MODS="$CACHE/mods/lib/net45"
echo "== Runtime compile check"
dotnet build CompileCheck/Runtime/Runtime.csproj -nologo -v q -p:UnityModules="$MODS" -p:TreatWarningsAsErrors=true
echo "== Editor compile check"
dotnet build CompileCheck/Editor/Editor.csproj -nologo -v q -p:UnityModules="$MODS" -p:UnityEditorDll="$CACHE/sdk/lib/UnityEditor.dll"
echo "== Logic tests"
dotnet run --project LogicTests/LogicTests.csproj
