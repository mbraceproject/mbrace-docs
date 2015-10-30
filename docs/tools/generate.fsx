// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
let mbraceCoreBinaries = [ "MBrace.Core.dll" ]
let mbraceRuntimeBinaries = [ "MBrace.Runtime.dll" ]
let mbraceThespianBinaries = [ "MBrace.Thespian.dll" ]
let mbraceFlowBinaries = [ "MBrace.Flow.dll"  ]
let mbraceAzureBinaries = [ "MBrace.Azure.dll" ]
// Web site location for the generated documentation
//let website = "http://nessos.github.io/MBrace"
let website = "http://www.m-brace.net"

let mbraceCoreGithubLink = "http://github.com/mbraceproject/MBrace.Core"
let mbraceAzureGithubLink = "http://github.com/mbraceproject/MBrace.Azure"
let mbraceFlowGithubLink = "http://github.com/mbraceproject/MBrace.Flow"
let mbraceThespianGithubLink = "http://github.com/mbraceproject/MBrace.Thespian"

// Specify more information about your project
let info =
  [ "project-author", "Jan Dzik, Nick Palladinos, Kostas Rontogiannis, Eirik Tsarpalis"
    "project-summary", "An open source framework for large-scale distributed computation and data processing written in F#."
    "project-github", mbraceCoreGithubLink
    "project-nuget", "http://www.nuget.org/packages/MBrace.Core" ]

let samplesInfo =  [ "project-name", "MBrace.Core and MBrace.Azure" ] @ info
    

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
let mbraceThespianPkgDir  = __SOURCE_DIRECTORY__ @@ ".." @@ ".." @@ "packages" @@ "MBrace.Thespian" @@ "tools"
let mbraceRuntimePkgDir  = mbraceThespianPkgDir
let mbraceCorePkgDir  = mbraceThespianPkgDir
let mbraceFlowPkgDir  = __SOURCE_DIRECTORY__ @@ ".." @@ ".." @@ "packages" @@ "MBrace.Flow" @@ "lib" @@ "net45"
let mbraceAzurePkgDir = __SOURCE_DIRECTORY__ @@ ".." @@ ".." @@ "packages" @@ "MBrace.Azure" @@ "tools"
let streamsPkgDir     = __SOURCE_DIRECTORY__ @@ ".." @@ ".." @@ "packages" @@ "Streams" @@ "lib" @@ "net45"
let content    = __SOURCE_DIRECTORY__ @@ ".." @@ "content"
let starterKit = __SOURCE_DIRECTORY__ @@ ".." @@ "starterkit"
let starterKitImg = __SOURCE_DIRECTORY__ @@ ".." @@ "starterkit" @@ "HandsOnTutorial" @@ "img"
let output     = __SOURCE_DIRECTORY__ @@ ".." @@ "output"
let files      = __SOURCE_DIRECTORY__ @@ ".." @@ "files"
let templates  = __SOURCE_DIRECTORY__ @@ "templates"
let formatting = __SOURCE_DIRECTORY__ @@ ".." @@ ".." @@ "packages" @@ "FSharp.Formatting/"
let docTemplate = formatting @@ "templates" @@ "docpage.cshtml"

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
  let coreBinaries = [ for lib in mbraceCoreBinaries -> mbraceCorePkgDir @@ lib ]
  let thespianBinaries = [ for lib in mbraceThespianBinaries -> mbraceThespianPkgDir @@ lib ]
  let runtimeBinaries = [ for lib in mbraceRuntimeBinaries -> mbraceThespianPkgDir @@ lib ]
  let flowBinaries = [ for lib in mbraceFlowBinaries -> mbraceFlowPkgDir @@ lib ]
  let azureBinaries = [ for lib in mbraceAzureBinaries -> mbraceAzurePkgDir @@ lib ]
  let libDirs = [mbraceThespianPkgDir; mbraceFlowPkgDir; mbraceAzurePkgDir; streamsPkgDir ]
    
  for (proj, binaries, outdir, githubLink) in 
      [("MBrace.Core", coreBinaries, output @@ "reference" @@ "core", mbraceCoreGithubLink)
       ("MBrace.Flow", flowBinaries, output @@ "reference" @@ "flow", mbraceFlowGithubLink)
       ("MBrace.Runtime", runtimeBinaries, output @@ "reference" @@ "runtime", mbraceCoreGithubLink)
       ("MBrace.Azure", azureBinaries, output @@ "reference" @@ "azure", mbraceAzureGithubLink)
       ("MBrace local cluster simulator", thespianBinaries, output @@ "reference" @@ "thespian", mbraceThespianGithubLink)] do
      CleanDir outdir
      MetadataFormat.Generate
        ( binaries, outdir, layoutRoots, 
          parameters = ("project-name", proj)::("root", root)::info,
          sourceRepo = githubLink  @@ "tree/master",
          sourceFolder = __SOURCE_DIRECTORY__ @@ ".." @@ "..",
          libDirs = libDirs,
          publicOnly = true )


// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =

  Fake.FileHelper.CleanDir starterKit
  Fake.Git.Repository.cloneSingleBranch __SOURCE_DIRECTORY__ "https://github.com/mbraceproject/MBrace.StarterKit" "master" starterKit
  if Fake.ProcessHelper.Shell.Exec(__SOURCE_DIRECTORY__ @@ ".." @@ ".." @@ ".paket" @@ "paket.exe","install",starterKit) <> 0 then
      failwith "paket restore failed"

  let processDir topInDir topOutDir = 
      let subdirs = Directory.EnumerateDirectories(topInDir, "*", SearchOption.AllDirectories)
      for dir in Seq.append [topInDir] subdirs do
        let sub = 
            if dir.Length > topInDir.Length && dir.StartsWith(topInDir) then dir.Substring(topInDir.Length + 1) 
            else "."
        Literate.ProcessDirectory
          ( dir, docTemplate, topOutDir @@ sub, replacements = ("root", root)::samplesInfo,
            layoutRoots = layoutRoots, generateAnchors = true )
  processDir content output
  processDir starterKit (output @@ "starterkit")
<<<<<<< Updated upstream
  CopyRecursive starterKitImg (output @@ "starterkit" @@ "HandsOnTutorial" @@ "img") true |> Log "Copying StarterKit img files: "
=======
  CopyRecursive (starterKit @@ "HandsOnTutorial" @@ "img") (output @@ "starterkit" @@ "HandsOnTutorial" @@ "img") true |> Log "Copying file: "
>>>>>>> Stashed changes


// Generate
CleanDir output
CreateDir output
copyFiles()
buildReference()
buildDocumentation()
