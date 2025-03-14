"""
Use this script to build generated API docs.

To build for GitHub pages:
$ python docfx/build.py

To serve generated docs locally:
$ python docfx/build.py --serve
"""

import os
import shutil
import subprocess
import sys

docfx_dir = os.path.dirname(os.path.abspath(__file__))
repo_dir = os.path.abspath(os.path.join(docfx_dir, ".."))

tmpl_add = """
    <!-- Add Muml code block highlighting -->
    <script src="./{{_rel}}public/highlight.min.js"></script>
    <script src="./{{_rel}}public/highlight-muml.js"></script>
    <script>
    document.addEventListener("DOMContentLoaded", function () {
        hljs.highlightAll();
    });
    </script>
"""

def cp(src, dst):
    print("Copying %s to %s" % (src, dst))
    shutil.copy(os.path.join(repo_dir, src), os.path.join(repo_dir, dst))
    
def mv(src, dst):
    print("Moving %s to %s" % (src, dst))
    shutil.move(os.path.join(repo_dir, src), os.path.join(repo_dir, dst))

def run_cmd(cmd):
    metadata_process = subprocess.run(cmd)
    if metadata_process.returncode != 0:
        print("Command failed")
        sys.exit(1)

def insert(path, after, add):
    with open(path, "rt", encoding="utf-8") as f:
        src = f.read()
    i = src.index(after)
    src_new = src[:i] + after + add + src[i+len(after):]
    with open(path, "wt", encoding="utf-8") as f:
        f.write(src_new)

def __main__():
    os.chdir(repo_dir)

    if not os.path.exists("docfx/_exported_templates/modern"):
        print("Exporting docfx modern template")
        run_cmd(["docfx", "template", "export", "modern"])
        
        mv("_exported_templates", "docfx/_exported_templates")
        
        print("Patching docfx template")
        insert(
            "docfx/_exported_templates/modern/layout/_master.tmpl",
            r'<script type="module" src="./{{_rel}}public/docfx.min.js"></script>',
            tmpl_add,
        )
    
    cp("readme.md", "docfx/index.md")
    cp("docfx/lib/highlight.min.js", "docfx/_exported_templates/modern/public/highlight.min.js")
    cp("highlight-muml.js", "docfx/_exported_templates/modern/public/highlight-muml.js")
    
    print("Generating docfx metadata")
    run_cmd(["docfx", "metadata", "docfx/docfx.json"])

    print("Building with docfx")
    run_cmd(["docfx", "build", "docfx/docfx.json"] + sys.argv[1:])

if __name__ == "__main__":
    __main__()
