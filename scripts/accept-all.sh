#!/bin/bash

# Directory to process (default is current directory if not provided)
DIR="${1:-.}"

# For each *.received.* file (generated sources are .cs, diagnostics are .txt), remove the corresponding
# *.verified.* file and rename the received one
for received_file in "$DIR"/*.received.*; do
    # Check if the glob matched any files
    [ -e "$received_file" ] || continue

    # Derive the verified name by swapping the .received. infix
    verified_file="${received_file/.received./.verified.}"

    # If a corresponding verified file exists, delete it
    if [ -f "$verified_file" ]; then
        rm -f "$verified_file"
    fi

    # Rename the received file to verified
    mv "$received_file" "$verified_file"
done
