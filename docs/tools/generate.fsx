// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
let mbraceBinaries = [ "MBrace.Core.dll" ; "MBrace.Runtime.Core.dll" ; "MBrace.Azure.Store.dll" ; "MBrace.Azure.Client.dll" ]
let mbraceFlowBinaries = [ "MBrace.Flow.dll"  ]
// Web site location for the generated documentation
//let website = "http://nessos.github.io/MBrace"
let website = "http://www.m-brace.net"

let githubLink = "http://github.com/mbraceproject/MBrace.Core"

// Specify more information about your project
let info =
  [ "project-name", "MBrace"
    "project-author", "Jan Dzik, Nick Palladinos, Kostas Rontogiannis, Eirik Tsarpalis"
    "project-summary", "An open source framework for large-scale distributed computation and data processing written in F#."
    "project-github", githubLink
    "project-nuget", "http://www.nuget.org/packages/MBrace.Core" ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

#load @"../../packages/FSharp.Formatting/FSharp.Formatting.fsx"
#r "../../packages/FAKE/tools/FakeLib.dll"
open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ @@ "../output")
#endif

// Paths with template/source/output locations
let mbracePkg  = __SOURCE_DIRECTORY__ @@ "../../packages/MBrace.Azure.Standalone/tools"
let mbraceFlowPkg  = __SOURCE_DIRECTORY__ @@ "../../packages/MBrace.Flow/lib/net45"
let content    = __SOURCE_DIRECTORY__ @@ "../content"
let output     = __SOURCE_DIRECTORY__ @@ "../output"
let files      = __SOURCE_DIRECTORY__ @@ "../files"
let templates  = __SOURCE_DIRECTORY__ @@ "templates"
let formatting = __SOURCE_DIRECTORY__ @@ "../../packages/FSharp.Formatting/"
let docTemplate = formatting @@ "templates/docpage.cshtml"

// Where to look for *.csproj templates (in this order)
let layoutRoots =
  [ templates; formatting @@ "templates"
    formatting @@ "templates/reference" ]

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
  CopyRecursive files output true |> Log "Copying file: "
  ensureDirectory (output @@ "content")
  CopyRecursive (formatting @@ "styles") (output @@ "content") true 
    |> Log "Copying styles and scripts: "

// Build API reference from XML comments
let buildReference () =
  CleanDir (output @@ "reference")
  let binaries =
    [ for lib in mbraceBinaries -> mbracePkg @@ lib
      for lib in mbraceFlowBinaries -> mbraceFlowPkg @@ lib ]
    
  MetadataFormat.Generate
    ( binaries , output @@ "reference", layoutRoots, 
      parameters = ("root", root)::info,
      sourceRepo = githubLink @@ "tree/master",
      sourceFolder = __SOURCE_DIRECTORY__ @@ ".." @@ "..",
      libDirs = [mbracePkg; mbraceFlowPkg; __SOURCE_DIRECTORY__ + "/../../packages/Streams/lib/net45"],
      //assemblyReferences = [__SOURCE_DIRECTORY__ + "/../../packages/Streams/lib/net45/Streams.Core.dll"],
      publicOnly = true )

// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =
  let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.AllDirectories)
  for dir in Seq.append [content] subdirs do
    let sub = if dir.Length > content.Length then dir.Substring(content.Length + 1) else "."
    Literate.ProcessDirectory
      ( dir, docTemplate, output @@ sub, replacements = ("root", root)::info,
        layoutRoots = layoutRoots, generateAnchors = true )

// Generate
CleanDir output
CreateDir output
copyFiles()
buildDocumentation()
buildReference()