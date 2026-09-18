#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
cd "$ROOT"

dotnet build apps/Spaces/LinuxHost/CakeOS.Spaces.LinuxHost.csproj -c Release

PROVIDER_OUT="$ROOT/apps/Spaces/LinuxHost/bin/Release/net10.0"
HOST_OUT="$ROOT/HUI/LinuxHost/bin/Release/net10.0"

for file in   CakeOS.Spaces.LinuxHost.dll   CakeOS.Spaces.App.dll   CakeOS.Spaces.Hui.dll   CakeOS.Hui.SharedComponents.dll
 do
  if [[ ! -f "$PROVIDER_OUT/$file" ]]; then
    echo "Missing build output: $PROVIDER_OUT/$file" >&2
    exit 1
  fi
  cp "$PROVIDER_OUT/$file" "$HOST_OUT/$file"
done

exec dotnet run --project HUI/LinuxHost/CakeOS.HuiLinuxHost.csproj -c Release --no-build --   --hui-root-provider-assembly "$HOST_OUT/CakeOS.Spaces.LinuxHost.dll"   --hui-root-provider-type CakeOS.Spaces.LinuxHost.SpacesLinuxRootProvider
