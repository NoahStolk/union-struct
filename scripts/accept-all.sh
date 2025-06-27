#!/bin/bash

# Directory to process (default is current directory if not provided)
DIR="${1:-.}"

# For each *.received.cs file, remove the corresponding *.verified.cs and rename the received one
for received_file in "$DIR"/*.received.cs; do
    # Check if the glob matched any files
    [ -e "$received_file" ] || continue

    # Derive the base name (without .received.cs)
    base="${received_file%.received.cs}"
    verified_file="${base}.verified.cs"

    # If a corresponding verified file exists, delete it
    if [ -f "$verified_file" ]; then
        rm -f "$verified_file"
    fi

    # Rename the received file to verified
    mv "$received_file" "$verified_file"
done
