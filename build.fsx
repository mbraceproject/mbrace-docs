// --------------------------------------------------------------------------------------
// FAKE build script 
// --------------------------------------------------------------------------------------

#I "packages/FAKE/tools"
#r "packages/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper

open System
open System.IO


let project = "MBrace"
let authors = [ "Jan Dzik" ; "Nick Palladinos" ; "Kostas Rontogiannis" ; "Eirik Tsarpalis" ]

let description = """
    An open source framework for large-scale distributed computation and data processing written in F#.
"""

let tags = "F# cloud mapreduce distributed"

// --------------------------------------------------------------------------------------
// Read release notes & version info from RELEASE_NOTES.md
Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = parseReleaseNotes (IO.File.ReadAllLines "RELEASE_NOTES.md") 
let nugetVersion = release.NugetVersion

let gitHome = "https://github.com/mbraceproject"
let gitName = "MBrace"

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
    let attributes =
        [ 
            Attribute.Title project
            Attribute.Product project
            Attribute.Company "Nessos Information Technologies"
            Attribute.Copyright "\169 Nessos Information Technologies."
            Attribute.Trademark "{m}brace"
            Attribute.Version release.AssemblyVersion
            Attribute.FileVersion release.AssemblyVersion
        ]

    !! "./src/**/AssemblyInfo.fs"
    |> Seq.iter (fun info -> CreateFSharpAssemblyInfo info attributes)
)


// --------------------------------------------------------------------------------------
// Clean and restore packages


Target "RestorePackages" (fun _ ->
    !! "./**/packages.config"
    |> Seq.iter (RestorePackage (fun p -> { p with ToolPath = "./.nuget/NuGet.exe" }))
)

Target "Clean" (fun _ ->
    CleanDirs (!! "**/bin/Release/")
    CleanDir "bin/"
)

// --------------------------------------------------------------------------------------
// Build


let configuration = environVarOrDefault "Configuration" "Release"
let ignoreClusterTests = environVarOrDefault "IgnoreClusterTests" "false" |> Boolean.Parse

Target "Build" (fun _ ->
    // Build the rest of the project
    { BaseDirectory = __SOURCE_DIRECTORY__
      Includes = [ project + ".sln" ]
      Excludes = [] } 
    |> MSBuild "" "Build" ["Configuration", configuration]
    |> Log "AppBuild-Output: "
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner & kill test runner when complete


let testAssemblies = 
    [
        yield "bin/MBrace.Store.Tests.dll"
        yield "bin/MBrace.Core.Tests.dll"
        if not ignoreClusterTests then
            yield "bin/MBrace.Runtime.Tests.dll"
    ]

Target "RunTests" (fun _ ->
    let nunitVersion = GetPackageVersion "packages" "NUnit.Runners"
    let nunitPath = sprintf "packages/NUnit.Runners.%s/tools" nunitVersion
    ActivateFinalTarget "CloseTestRunner"
        
    testAssemblies
    |> NUnit (fun p -> 
        { p with
            Framework = "v4.0.30319"
            ToolPath = nunitPath
            DisableShadowCopy = true
            TimeOut = TimeSpan.FromMinutes 60.
            ExcludeCategory = "ChaosMonkeyTests"
            OutputFile = "TestResults.xml" })
)

FinalTarget "CloseTestRunner" (fun _ ->  
    ProcessHelper.killProcess "nunit-agent.exe"
)

// Nuget packages

let addFile (target : string) (file : string) =
    if File.Exists (Path.Combine("nuget", file)) then (file, Some target, None)
    else raise <| new FileNotFoundException(file)

let addAssembly (target : string) assembly =
    let includeFile force file =
        let file = file
        if File.Exists (Path.Combine("nuget", file)) then [(file, Some target, None)]
        elif force then raise <| new FileNotFoundException(file)
        else []

    seq {
        yield! includeFile true assembly
        yield! includeFile false <| Path.ChangeExtension(assembly, "pdb")
        yield! includeFile false <| Path.ChangeExtension(assembly, "xml")
        yield! includeFile false <| assembly + ".config"
    }

Target "Nuget.Core" (fun _ ->
    let nugetPath = ".nuget/NuGet.exe"
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = "MBrace.Core"
            Summary = "Core libraries for the MBrace programming model."
            Description = description
            Version = nugetVersion
            ReleaseNotes = String.concat " " release.Notes
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = []
            Files =
                [
                    yield! addAssembly @"lib\net45" @"..\bin\MBrace.Core.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\MBrace.Lib.dll"
                ]
        })
        ("nuget/MBrace.nuspec")
)

Target "NuGet.Store" (fun _ ->
    let nugetPath = ".nuget/NuGet.exe"
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = "MBrace.Store"
            Summary = "Interface definitions to the MBrace storage API."
            Description = description
            Version = nugetVersion
            ReleaseNotes = String.concat " " release.Notes
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            References = 
                [
                    "MBrace.Store.dll"
                ]
            Files =
                [
                    yield! addAssembly @"lib\net45" @"..\bin\MBrace.Store.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\MBrace.Utils.dll"

                ]
        })
        ("nuget/MBrace.nuspec")
)

Target "NuGet.Client" (fun _ ->
    let nugetPath = ".nuget/NuGet.exe"
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = "MBrace.Client"
            Summary = "Client API for interfacing with MBrace runtimes."
            Description = description
            Version = nugetVersion
            ReleaseNotes = String.concat " " release.Notes
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Files =
                [
                    yield! addAssembly @"lib\net45" @"..\bin\MBrace.Utils.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\MBrace.Runtime.Base.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\MBrace.Client.dll"
                ]
            Dependencies = 
                [
                    "UnionArgParser",                               "0.8.0"
                    "Unquote",                                      "2.2.2"
                    "FsPickler",                                    RequireExactly "1.0.2"
                    "FsPickler.Json",                               RequireExactly "1.0.2"
                    "Thespian",                                     RequireExactly "0.0.11-alpha"
                    "Vagrant",                                      RequireExactly "0.2.6"
                    "MBrace.Core",                                  RequireExactly release.NugetVersion
                    "MBrace.Store",                                 RequireExactly release.NugetVersion
                ]
        })
        ("nuget/MBrace.nuspec")
)

Target "NuGet.Azure" (fun _ ->
    let nugetPath = ".nuget/NuGet.exe"
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = "MBrace.Azure"
            Summary = "MBrace bindings for Azure storage."
            Description = description
            Version = nugetVersion
            ReleaseNotes = String.concat " " release.Notes
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Dependencies = 
                [
                    "MBrace.Store", RequireExactly release.NugetVersion
                ]
            Files =
                [
                    yield! addAssembly @"lib\net45" @"..\bin\MBrace.Azure.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\Microsoft.Data.Edm.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\Microsoft.Data.OData.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\Microsoft.Data.Services.Client.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\Microsoft.WindowsAzure.Configuration.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\Microsoft.WindowsAzure.Storage.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\System.Spatial.dll"
                    yield! addAssembly @"lib\net45" @"..\bin\Newtonsoft.Json.dll"
                ]
        })
        ("nuget/MBrace.nuspec")
)

let writeMBraceIntroScript () =
    let newFile = 
        File.ReadLines("./nuget/mbrace-tutorial.fsx")
        |> Seq.map (fun line -> 
            if line.StartsWith """#load "../packages/MBrace.Runtime""" then 
                sprintf """#load "../packages/MBrace.Runtime.%s/bootstrap.fsx" """ release.NugetVersion
            else line)
        |> Seq.toArray

    File.WriteAllLines("./nuget/mbrace-tutorial.fsx", newFile)

Target "Nuget.Runtime" (fun _ ->
    let nugetPath = ".nuget/NuGet.exe"
    do writeMBraceIntroScript ()
    NuGet (fun p -> 
        { p with   
            Authors = authors
            Project = "MBrace.Runtime"
            Summary = "Standalone distribution of the MBrace framework."
            Description = description
            Version = nugetVersion
            ReleaseNotes = String.concat " " release.Notes
            Tags = tags
            OutputPath = "bin"
            ToolPath = nugetPath
            AccessKey = getBuildParamOrDefault "nugetkey" ""
            Publish = hasBuildParam "nugetkey"
            Files =
                [  
                    yield! addAssembly @"tools" @"..\bin\Newtonsoft.Json.dll"
                    yield! addAssembly @"tools" @"..\bin\Mono.Cecil.dll"
                    yield! addAssembly @"tools" @"..\bin\FsPickler.dll"
                    yield! addAssembly @"tools" @"..\bin\FsPickler.Json.dll"
                    yield! addAssembly @"tools" @"..\bin\Thespian.dll"
                    yield! addAssembly @"tools" @"..\bin\Thespian.Cluster.dll"
                    yield! addAssembly @"tools" @"..\bin\UnionArgParser.dll"
                    yield! addAssembly @"tools" @"..\bin\Unquote.dll"
                    yield! addAssembly @"tools" @"..\bin\Vagrant.dll"
                    yield! addAssembly @"tools" @"..\bin\Vagrant.Cecil.dll"
                    yield! addAssembly @"tools" @"..\bin\MBrace.Core.dll"
                    yield! addAssembly @"tools" @"..\bin\MBrace.Lib.dll"
                    yield! addAssembly @"tools" @"..\bin\MBrace.Utils.dll"
                    yield! addAssembly @"tools" @"..\bin\MBrace.Runtime.Base.dll"
                    yield! addAssembly @"tools" @"..\bin\MBrace.Runtime.Cluster.dll"
                    yield! addAssembly @"tools" @"..\bin\MBrace.Client.dll"
                    yield! addAssembly @"tools" @"..\bin\MBrace.Store.dll"
                    yield! addAssembly @"tools" @"..\bin\mbraced.exe"
                    yield! addAssembly @"tools" @"..\bin\mbrace.worker.exe"
                    yield! addAssembly @"tools" @"..\bin\mbracesvc.exe"
                    yield! addAssembly @"tools" @"..\bin\mbracectl.exe"
                    yield  addFile     @"tools" @"..\lib\fsharp\FSharp.Core.dll"
                    yield  addFile     @"tools" @"..\lib\fsharp\FSharp.Core.xml"
                    yield  addFile     @"tools" @"..\lib\fsharp\FSharp.Core.sigdata"
                    yield  addFile     @"tools" @"..\lib\fsharp\FSharp.Core.optdata"
                    yield  addFile     @"" @"bootstrap.fsx"
                    yield  addFile     @"tools" @"install.ps1"  
                    yield  addFile     @"content" "mbrace-tutorial.fsx"
                ]                   
        })                          
        ("nuget/MBrace.nuspec")
)

// --------------------------------------------------------------------------------------
// documentation

Target "GenerateDocs" (fun _ ->
    executeFSIWithArgs "docs/tools" "generate.fsx" ["--define:RELEASE"] [] |> ignore
)

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    fullclean tempDocsDir
    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override
Target "Default" DoNothing
Target "Release" DoNothing
Target "PrepareRelease" DoNothing
Target "NuGet" DoNothing
Target "Help" (fun _ -> PrintTargets() )

"Clean"
  ==> "RestorePackages"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RunTests"
  ==> "Default"

"Build"
  ==> "PrepareRelease"
  ==> "NuGet.Core"
  ==> "NuGet.Store"
  ==> "NuGet.Client"
  ==> "NuGet.Runtime"
  ==> "NuGet.Azure"
  ==> "Nuget"
  ==> "GenerateDocs"
  ==> "ReleaseDocs"
  ==> "Release"

//// start build
RunTargetOrDefault "Default"