#!/bin/sh

set -u

readonly script_path="$(cd "$(dirname "${0}")" && pwd)"

### Commands ###
dotnet_path="$(command -v "dotnet")"

if [ -z "${dotnet_path}" ] ; then
    echo "Cannot find the .NET command on the system."
    echo "Install the .NET SDK first: https://dotnet.microsoft.com/en-us/download"
    exit 1
fi

set -e

echo "Found the .NET command: ${dotnet_path}"

### Options ###
# Install or update to the specified version of the ReSharper command line tools.
# - or -
# Use an empty string to install the latest stable version of the ReSharper command line tools if not installed.
tools_version=""

while [ $# -gt 0 ] ; do
    case "$1" in
        "--version") tools_version="$2" ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: installresharper.sh [--version value]"
            exit 1
            ;;
    esac

    shift
    shift
done

### Execution ###
installed_tools=""
installed_tools_version=""

for installed_tool in $("${dotnet_path}" tool list --global) ; do
    if [ -n "${installed_tools}" ] ; then
        installed_tools_version="${installed_tool}"
        break
    fi

    case "${installed_tool}" in
        "jetbrains.resharper.globaltools")
            installed_tools="${installed_tool}"
            ;;
    esac
done

if [ -z "${installed_tools}" ] ; then
    if [ -z "${tools_version}" ] ; then
        "${dotnet_path}" tool install "JetBrains.ReSharper.GlobalTools" --global
    else
        "${dotnet_path}" tool install "JetBrains.ReSharper.GlobalTools" --global --version "${tools_version}"
    fi
elif [ \( -n "${tools_version}" \) -a \( "${tools_version}" > "${installed_tools_version}" \) ] ; then
    "${dotnet_path}" tool update "JetBrains.ReSharper.GlobalTools" --global --version "${tools_version}"
else
    echo "ReSharper command line tools version ${installed_tools_version} are already installed."
fi
