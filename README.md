## mbrace-docs

MBrace website and document generation repository, pushed to http://mbrace.io

### Building and Publishing

Use ``build.sh`` or ``build.cmd`` to build a local copy of the docs. This runs `docs/tools/generate.fsx` and places the results in `docs/output` as HTML.

Use ``build.sh Release`` or ``build.cmd Release`` to push the docs to the website.  This builds the docs, then pushed the contents of `docs/output` into the `gh-pages` branch of this repository (if you have permissions).  Once in that branch, GitHub automatically publishes these contents at  http://mbrace.io.



