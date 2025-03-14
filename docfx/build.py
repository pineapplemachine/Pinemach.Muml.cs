"""
Use this script to build generated API docs.

To serve generated docs locally:
$ python docfx/build.py --serve
"""

import os
import shutil
import subprocess
import sys

# Determine script directory and set working directory to ../ relative to script
script_dir = os.path.dirname(os.path.abspath(__file__))
repo_dir = os.path.abspath(os.path.join(script_dir, ".."))
os.chdir(repo_dir)

print("Copying repo ../readme.md to docfx ./index.md")
shutil.copy(
    os.path.join(repo_dir, "readme.md"),
    os.path.join(repo_dir, "docfx/index.md"),
)

print("Copying repo ../prism-muml.js to docfx template public/")
shutil.copy(
    os.path.join(repo_dir, "highlight-muml.js"),
    os.path.join(repo_dir, "docfx/templates/modern-muml/public/highlight-muml.js"),
)

metadata_command = ["docfx", "metadata", "docfx/docfx.json"]
print("Generating docfx metadata")
metadata_process = subprocess.run(metadata_command)

if metadata_process.returncode != 0:
    print("Failed to generate docfx metadata")
    sys.exit(1)

build_command = ["docfx", "build", "docfx/docfx.json"] + sys.argv[1:]
print("Building with docfx")
build_process = subprocess.run(build_command)

if build_process.returncode != 0:
    print("Failed to build with docfx")
    sys.exit(1)
