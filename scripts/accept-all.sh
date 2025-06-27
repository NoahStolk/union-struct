#!/bin/bash

# Directory to process (default is current directory if not provided)
DIR="${1:-.}"

# Step 1: Delete all files ending with .verified.cs (in the specified directory only)
find "$DIR" -maxdepth 1 -type f -name '*.verified.cs' -exec rm -f {} \;

# Step 2: Rename all files ending with .received.cs to .verified.cs
for file in "$DIR"/*.received.cs; do
    # Check if the glob matched any file
    [ -e "$file" ] || continue
    new_name="${file%.received.cs}.verified.cs"
    mv "$file" "$new_name"
done
