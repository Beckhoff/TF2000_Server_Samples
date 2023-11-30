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
# Include the extension directory with the specified name in code inspection.
# - or -
# Use an empty string to include all extension directories in code inspection.
extension_name=""

# Write the output of the code inspection in one of the following formats:
# - Xml
# - Html
# - Text
# - Sarif
# https://www.jetbrains.com/help/resharper/InspectCode.html#auxiliary-parameters
output_format="Xml"

while [ $# -gt 0 ] ; do
    case "$1" in
        "--name") extension_name="$2" ;;
        "--format") output_format="$2" ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: inspectcode.sh [--name value] [--format value]"
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

case "${output_format}" in
    "Xml")
        output_file_extension="xml"
        max_output_length=12
        ;;
    "Html")
        output_file_extension="html"
        max_output_length=15
        ;;
    "Text")
        output_file_extension="txt"
        max_output_length=1
        ;;
    "Sarif")
        output_file_extension="sarif"
        max_output_length=32
        ;;
    *)
        echo "Unknown output format: ${output_format}"
        exit 1
        ;;
esac

### Execution ###
for extension in "${script_path}"/* ; do
    if [ -d "${extension}" ] ; then
        if [ -z "${extension_name}" ] || [ "$(basename "${extension}")" = "${extension_name}" ] ; then
            for solution in "${extension}"/*.sln ; do
                output_path="${extension}/inspectcode.${output_file_extension}"

                echo "Inspecting code in solution '${solution}'..."
                "${jb_path}" inspectcode "${solution}" --output="${output_path}" --format="${output_format}" --no-build --swea

                if [ "$(wc -l "${output_path}" | awk '{print $1}')" -gt "${max_output_length}" ] ; then
                    echo -e "\e[0;31mCode inspection detected issues and wrote them to the following file: ${output_path}\e[0;37m"
                fi
            done
        fi
    fi
done
