(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "../../packages/MBrace.Thespian/MBrace.Thespian.fsx"
#r "../../packages/MBrace.Flow/lib/net45/MBrace.Flow.dll"

open MBrace.Core
open MBrace.Thespian
open MBrace.Flow

let config = Unchecked.defaultof<MBrace.Thespian.ThespianCluster>

#nowarn "443"

(**

# Programming model

The MBrace programming model is a language-integrated cloud programming DSL for use from F#.
It offers a concise and elegant programming model which extends F# asynchronous workflows 
to the domain of distributed cloud computation.

See the following samples which demonstrate
different aspects of the core programming model:

* [Hello World with MBrace](starterkit/HandsOnTutorial/1-hello-world.html)
* [Introduction to cloud combinators](starterkit/HandsOnTutorial/2-cloud-parallel.html)
* [Introduction to CPU parallelism](starterkit/HandsOnTutorial/3-cloud-parallel-cpu-intensive.html)
* [Introduction to Cloud Flows](starterkit/HandsOnTutorial/4-cloud-parallel-data-flow.html)
* [Running MBrace in the Thread Pool](starterkit/HandsOnTutorial/0-thread-pool.html)
* [Using C# and native components](starterkit/HandsOnTutorial/5-using-csharp-and-native-dlls.html)
* [Using cloud value storage](starterkit/HandsOnTutorial/6-using-cloud-values.html)
* [Using cloud file storage](starterkit/HandsOnTutorial/7-using-cloud-data-files.html)
* [Using cloud queue storage](starterkit/HandsOnTutorial/8-using-cloud-queues.html)
* [Using cloud key/value storage](starterkit/HandsOnTutorial/9-using-cloud-key-value-stores.html)
* [Example: Distributed Image Processing](starterkit/HandsOnTutorial/200-image-processing-example.html)
* [Example: Parallel Web Download](starterkit/HandsOnTutorial/200-cloud-parallel-web-download.html)
* [Example: Word Count](starterkit/HandsOnTutorial/200-wordcount.html)
* [Example: Using CloudFlow with FSharp.Data](starterkit/HandsOnTutorial/200-house-data-analysis.html)
* [Example: Monte Carlo Pi Approximation](starterkit/HandsOnTutorial/200-monte-carlo-pi-approximation.html)
* [Example: k-Means clustering](starterkit/HandsOnTutorial/200-kmeans-clustering.html)
* [Example: Extracting Statistics for a Spelling Corrector](starterkit/HandsOnTutorial/200-norvigs-spelling-corrector.html)
* [Example: Starting a WebServer to Control Your Cluster](starterkit/HandsOnTutorial/200-starting-a-web-server.html)

What follows is a general overview. 

## Cloud Workflows

In MBrace, the unit of computation is a *cloud workflow*:
*)

let myFirstCloudWorkflow = cloud { return 42 }

(**

Cloud workflows generate objects of type `Cloud<'T>`, which denote a delayed computation
that once executed will yield a result of type `'T`. Cloud workflows are language-integrated
and can freely intertwine with native code.

*)

let getDataCenterTime() = cloud {
    let now = System.DateTime.Now
    return now
}

(**

Simple cloud computations can be composed into larger ones using the `let!` keyword:

*)

let first = cloud { return 15 }
let second = cloud { return 27 }

cloud {
    let! x = first
    let! y = second
    return x + y
}

(** 

This creates bindings to the first and second workflows respectively,
to be consumed by the continuations of the main computation.
Once executed, this will sequentually perform the computations in `first`
and `second`, resuming once the latter has completed.

For loops and while loops are possible:

*)

/// Sequentially iterate and loop 
cloud {
    for i in [ 1 .. 100 ] do
        do! Cloud.Logf "Logging entry %d of 100" i

    while true do
        do! Cloud.Sleep 200
        do! Cloud.Log "Logging forever..."
}

(**

MBrace workflows also integrate with exception handling:

*)

cloud {
    try
        let! x = cloud { return 1 / 0 }
        return true

    with :? System.DivideByZeroException -> return false
}

(**

Asynchronous workflows can be embedded into cloud workflows:

*)

let downloadAsync (url : string) = async {
    use webClient = new System.Net.WebClient()
    let! html = webClient.AsyncDownloadString(System.Uri url) 
    return html.Split('\n')
}


cloud {
    let! t1 = downloadAsync "http://www.nessos.gr/" |> Cloud.OfAsync
    let! t2 = downloadAsync "http://www.m-brace.net/" |> Cloud.OfAsync
    return t1.Length + t2.Length
}

(**

## Parallelism Combinators

Cloud workflows as discussed so far enable asynchronous
computation but do not suffice in describing parallelism and distribution.
To control this, MBrace uses a collection of primitive combinators that act on
the distribution/parallelism semantics of execution in cloud workflows.

The previous example could be altered so that downloading happens in parallel:

*)

let downloadCloud url = downloadAsync url |> Cloud.OfAsync

cloud {

    let! results =
        [ "http://www.m-brace.net/"
          "http://www.nessos.gr/" ]
        |> List.map downloadCloud
        |> Cloud.Parallel

    return results |> Array.sumBy(fun r -> r.Length)
}

(**

Here is a second example of `Cloud.Parallel`:

*)

cloud {

    let n = System.Random().Next(50,100) // number of parallel jobs determined at runtime
    let! results = Cloud.Parallel [ for x in 1..n -> cloud { return x * x } ]
    return Array.sum results
}

(**

### Exception handling in workflows

For exception handling, consider the workflow:

*)

cloud {

    try
        let! results =
            [ "http://www.m-brace.net/"
              "http://www.nessos.gr/"
              "http://non.existent.domain/" ]
            |> List.map downloadCloud
            |> Cloud.Parallel

        return results |> Array.sumBy(fun r -> r.Length)

    with :? System.Net.WebException as e ->
        // log and reraise
        do! Cloud.Logf "Encountered error %O" e
        return raise e
}

(**

In this case, one of the child computations will fail on account of
an invalid url, creating an exception. In general, uncaught exceptions
bubble up through `Cloud.Parallel` triggering cancellation of all
outstanding child computations (just like `Async.Parallel`).

The exception handling clause will almost certainly be executed in a different machine than the one
in which it was originally thrown. This is due to the cloud workflow, which allows
exceptions, environments, closures to be passed around worker machines
in a largely transparent manner.

*)

(**
## Non-deterministic parallelism

MBrace provides the `Cloud.Choice` combinator that utilizes
parallelism for non-deterministic algorithms.
Cloud workflows of type `Cloud<'T option>` are said to be non-deterministic
in the sense that their return type indicates either success with `Some` result 
or a negative answer with `None`.

`Cloud.Choice` combines a collection of arbitrary nondeterministic computations
into one in which everything executes in parallel: it either returns `Some` result
whenever a child happens to complete with `Some` result (cancelling all pending jobs)
or `None` when all children have completed with `None`. It can be thought of as
a distributed equivalent to the `Seq.tryPick` function found in the F# core library.

The following example defines a distributed search function based on `Cloud.Choice`:

*)

let tryFind (f : 'T -> bool) (ts : 'T list) = cloud {
    let select t = cloud {
        return
            if f t then Some t
            else None
    }

    return! ts |> List.map select |> Cloud.Choice
}

(**

## Composing distributed workflows

Recursive and higher-order composition of cloud workflows is possible:

*)

/// Sequentially fold along a set of jobs
let rec foldLeftCloud (f : 'State -> 'T -> Cloud<'State>) state ts = cloud {
    match ts with
    | [] -> return state
    | t :: ts' ->
        let! s' = f state t
        return! foldLeftCloud f s' ts'
}

(**

## Distributed Data

Cloud workflows offer a programming model for distributed computation. 
But what happens when it comes to data? 

Small-to-medium-scale data can be transported implicitly as part of a 
cloud computation. This offers a limited (though extremely convenient) form of 
data distribution. However, it will not scale to all needs, particularly computations
involving gigabytes of data.

MBrace offers a range of mechanisms for managing
large-scale data in a more global and massive scale. These provide an essential decoupling 
between distributed computation and distributed data.

See also this [summary of the MBrace abstractions for cloud data](https://github.com/mbraceproject/MBrace.Design/blob/master/DataAbstractions.md).

### Cloud Values

The mbrace programming model offers access to persistable and cacheable distributed data 
entities known as cloud values. Cloud values very much resemble immutable data values found in 
F# but are stored in persisted cloud storage. The following workflow stores the downloaded 
content of a web page and returns a cloud value to it:

*)

let textCell = cloud {
    // download a piece of data
    let! text = downloadCloud "http://www.m-brace.net/"
    // store data to a new CloudValue
    let! cref = CloudValue.New text
    // return the ref
    return cref
}

(**

Dereferencing a cloud value can be done by getting its `.Value` property:

*)

let dereference (data : CloudValue<byte []>) = cloud {
    let v = data.Value
    return v.Length
}

(**

It is possible to define explicitly specify the StorageLevel used for the specific CloudValue instance:

*)

cloud {
    let! text = downloadCloud "http://www.m-mbrace.net/largeFile.txt"
    let! cref = CloudValue.New(text, storageLevel = StorageLevel.MemoryAndDisk)
    return cref
}

(**

This indicates the cloud value should be persisted using both disk storage and in-memory
for dereferencing worker instances.

*)

(**
### Example: Defining a MapReduce workflow

Cloud worklows in conjunctions with parallel combinators can be used
to articulate MapReduce-like workflows. A simplistic version follows:
*)
(***hide***)
module List =
    /// splits a list into two halves
    let split ts = ts,ts

(** *)

let mapReduce (map : 'T -> 'R) (reduce : 'R -> 'R -> 'R)
              (identity : 'R) (inputs : 'T list) =

    let rec aux inputs = cloud {
        match inputs with
        | [] -> return identity
        | [t] -> return map t
        | _ ->
            let left,right = List.split inputs
            let! results = Cloud.Parallel [ aux left;  aux right ]
            return reduce results.[0] results.[1]
    }

    aux inputs

(**

The workflow follows a divide-and-conquer approach, 
recursively partitioning input data until trivial cases are met. 
Recursive calls are passed through `Cloud.Parallel`,
thus achieving the effect of distributed parallelism.

This is a naive conception of mapReduce, as it does not enable data parallelism nor does it take into account cluster granularity.

### Cloud Files

Like cloud values, sequences and vectors, CloudFile is an immutable storage primitive 
that a references a file saved in the global store. 
In other words, it is an interface for storing or accessing binary blobs in the runtime.

*)

cloud {
    // enumerate all files from underlying storage container
    let! files = CloudFile.Enumerate "path/to/container"

    // read a cloud file and return its word count
    let wordCount (f : CloudFileInfo) = cloud {
        let! text = CloudFile.ReadAllText f.Path
        let count =
            text.Split(' ')
            |> Seq.groupBy id
            |> Seq.map (fun (token,instances) -> token, Seq.length instances)
            |> Seq.toArray

        return f.Path, count
    }

    // perform computation in parallel
    let! results = files |> Array.map wordCount |> Cloud.Parallel

    return results
}

(**

### Mutable cloud values (CloudAtom)

The `CloudAtom` primitive is, like `CloudValue`, a reference to data stored in the underlying cloud service.
However, CloudAtoms are mutable. The value of a cloud atom can be updated and, as a result,
its values are never cached. Mutable cloud values can be updated transactionally using the 
`CloudAtom.Transact` methods or forcibly using the `CloudAtom.Force` method.
 
The CloudAtom is a powerful primitive that can be used to create runtime-wide synchronization 
mechanisms like locks, semaphores, etc.

The following demonstrates simple use of the cloud atom:

*)

let race () = cloud {
    let! ca = CloudAtom.New(0)
    let! _ =
        cloud { ca.Force 2 }
            <||>
        cloud { ca.Force 1 }

    return ca.Value
}

(**

The snippet will return a result of either 1 or 2, depending on which update operation was run last.

The following snippet implements an transactionally incrementing function acting on a cloud atom:

*)

let increment (counter : CloudAtom<int>) = cloud {
    do! Cloud.OfAsync <| counter.UpdateAsync(fun c -> c + 1)
}

(**

## Other Primitives

For more information and examples on the programming model, please refer to the 
[API Reference](reference/index.html).

*)