#!/bin/sh

set -u

readonly script_path="$(cd "$(dirname "${0}")" && pwd)"

### Commands ###
jb_path="$(command -v "jb")"

if [ -z "${jb_path}" ] ; then
    echo "Cannot find ReSharper command line tools on the system."
    echo "Run '${script_path}/installresharper.sh' first to install them."
    exit 1
fi

set -e

echo "Found ReSharper command line tools: ${jb_path}"

### Options ###
# Include the extension directory with the specified name in code cleanup.
# - or -
# Use an empty string to include all extension directories in code cleanup.
extension_name=""

# Cleanup code using the specified profile.
# https://www.jetbrains.com/help/resharper/Code_Cleanup__Index.html#profiles
cleanup_profile="Built-in: Full Cleanup"

while [ $# -gt 0 ] ; do
    case "$1" in
        "--name") extension_name="$2" ;;
        "--profile") cleanup_profile="$2" ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: cleanupcode.sh [--name value] [--profile value]"
            exit 1
            ;;
    esac

    shift
    shift
done

### Validation ###
if [ -n "${extension_name}" ] && [ ! -d "${script_path}/${extension_name}" ] ; then
    echo "Extension directory does not exist: ${script_path}/${extension_name}"
    exit 1
fi

### Execution ###
for extension in "${script_path}"/* ; do
    if [ -d "${extension}" ] ; then
        if [ -z "${extension_name}" ] || [ "$(basename "${extension}")" = "${extension_name}" ] ; then
            for solution in "${extension}"/*.sln ; do
                echo "Cleaning up code in solution '${solution}'..."
                "${jb_path}" cleanupcode "${solution}" --profile="${cleanup_profile}"
            done
        fi
    fi
done
