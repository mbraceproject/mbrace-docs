(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "../../packages/MBrace.Azure/MBrace.Azure.fsx"
#r "../../packages/MBrace.Flow/lib/net45/MBrace.Flow.dll"

let cluster = Unchecked.defaultof<MBrace.Runtime.MBraceClient>

(**

This page generates F# snippets used for index.html in mbrace.io.
Generated html must be copied manually before publishing.

*)

open MBrace.Core
open MBrace.Flow

let numberOfDuplicates =
    CloudFlow.OfCloudFileByLine ["container/data0.csv" ; "container/data1.csv"]
    |> CloudFlow.map (fun line -> line.Split(','))
    |> CloudFlow.map (fun tokens -> int tokens.[0], Array.map int tokens.[1 ..])
    |> CloudFlow.groupBy (fun (id,_) -> id)
    |> CloudFlow.filter (fun (_,values) -> Seq.length values > 1)
    |> CloudFlow.length
    |> cluster.Run