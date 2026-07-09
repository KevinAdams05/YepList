#!/usr/bin/env bash
#
# Build the YepList Android client on Linux.
#
#   ./build-android.sh                 # default: :app:assembleDebug
#   ./build-android.sh :app:assembleRelease
#   ./build-android.sh clean :app:assembleDebug
#   ./build-android.sh tasks
#
# Requirements (one-time setup, see docs/sync-overhaul-notes.md):
#   - A JDK with javac, version 17+ (this script prefers ~/jdks/jdk-21).
#   - Android SDK at $ANDROID_HOME (default ~/Android/Sdk) with
#     platform-tools, platforms;android-35, build-tools;35.0.0.
#   - clients/android/local.properties pointing sdk.dir at that SDK.
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# --- JDK: need a real JDK (with javac), 17 or newer ---
if [ -x "${JAVA_HOME:-}/bin/javac" ]; then
    : # caller already supplied a valid JDK via JAVA_HOME
elif [ -x "$HOME/jdks/jdk-21/bin/javac" ]; then
    export JAVA_HOME="$HOME/jdks/jdk-21"
elif command -v javac >/dev/null 2>&1; then
    : # a JDK is on PATH; let Gradle use it
else
    echo "ERROR: No JDK with 'javac' found." >&2
    echo "Install one, e.g. a userspace Temurin under ~/jdks/jdk-21," >&2
    echo "or 'sudo apt install openjdk-21-jdk-headless'." >&2
    exit 1
fi

# --- Android SDK ---
export ANDROID_HOME="${ANDROID_HOME:-$HOME/Android/Sdk}"
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export PATH="$ANDROID_HOME/cmdline-tools/latest/bin:$ANDROID_HOME/platform-tools:$PATH"

if [ ! -d "$ANDROID_HOME/platforms" ]; then
    echo "WARNING: $ANDROID_HOME looks empty — install SDK packages first:" >&2
    echo "  sdkmanager 'platform-tools' 'platforms;android-35' 'build-tools;35.0.0'" >&2
fi

echo "JAVA_HOME=${JAVA_HOME:-<PATH java>}"
echo "ANDROID_HOME=$ANDROID_HOME"

# --- Build ---
if [ "$#" -eq 0 ]; then
    set -- :app:assembleDebug
fi

./gradlew "$@"

echo
echo "APK output(s):"
find app/build/outputs/apk -name '*.apk' 2>/dev/null || echo "  (none — non-assemble task?)"
