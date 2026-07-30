#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
unity_editor="${UNITY_EDITOR:-}"

if [[ -z "${unity_editor}" ]]; then
    for candidate in unity-editor Unity unity; do
        if command -v "${candidate}" >/dev/null 2>&1; then
            unity_editor="$(command -v "${candidate}")"
            break
        fi
    done
fi

if [[ -z "${unity_editor}" || ! -x "${unity_editor}" ]]; then
    echo "Unity Editor CLI not found. Set UNITY_EDITOR to the Unity executable." >&2
    exit 2
fi

project_path="$(mktemp -d "${TMPDIR:-/tmp}/unity-zed-smoke.XXXXXX")"
trap 'rm -rf "${project_path}"' EXIT

mkdir -p "${project_path}/Assets/Editor" "${project_path}/Packages"

python3 - "${repo_root}" "${project_path}/Packages/manifest.json" <<'PY'
import json
import pathlib
import sys

repository = pathlib.Path(sys.argv[1]).resolve().as_uri()
manifest = {
    "dependencies": {
        "com.maligan.unity-zed": repository,
        "com.unity.ide.visualstudio": "2.0.20",
    }
}
pathlib.Path(sys.argv[2]).write_text(json.dumps(manifest, indent=2) + "\n")
PY

cat > "${project_path}/Assets/Editor/UnityZedSmoke.asmdef" <<'EOF'
{
    "name": "UnityZed.SmokeTests",
    "references": ["com.maligan.zed-unity"],
    "includePlatforms": ["Editor"],
    "autoReferenced": false
}
EOF

cat > "${project_path}/Assets/Editor/UnityZedSmoke.cs" <<'EOF'
using System;
using UnityZed;

namespace UnityZedSmoke
{
    public static class Runner
    {
        public static void Run()
        {
            // Let exceptions escape. In batch mode Unity reports an execute-method
            // exception as a failed process instead of accidentally returning success.
            new ZedSettings().Sync();
            new ZedDiscovery().GetInstallations();
            Console.WriteLine("UNITY_ZED_SMOKE_TEST_PASSED");
        }
    }
}
EOF

log_path="${project_path}/unity.log"
if ! "${unity_editor}" \
    -batchmode \
    -nographics \
    -quit \
    -forgetProjectPath \
    -projectPath "${project_path}" \
    -executeMethod UnityZedSmoke.Runner.Run \
    -logFile - 2>&1 | tee "${log_path}"; then
    echo "Unity batch-mode smoke test failed." >&2
    exit 1
fi

if ! grep -Fq "UNITY_ZED_SMOKE_TEST_PASSED" "${log_path}"; then
    echo "Unity exited without executing the smoke-test method." >&2
    exit 1
fi
