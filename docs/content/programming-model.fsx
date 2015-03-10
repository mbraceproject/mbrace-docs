(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/"
#I "../../src/MBrace.Client/"

#load "bootstrap.fsx"

#r "FsPickler.dll"

open Nessos.MBrace
open Nessos.MBrace.Lib
open Nessos.MBrace.Client

open Nessos.FsPickler

(**

# Programming model

The MBrace programming model is based on F# 
[computation expressions](http://msdn.microsoft.com/en-us/library/dd233182.aspx),
a feature that allows user-defined, language-integrated DSLs.
A notable application of computation expressions in F# is
[asynchronous workflows](http://msdn.microsoft.com/en-us/library/dd233250.aspx),
a core library implementation that offers a concise and elegant asynchronous programming model.
MBrace draws heavy inspiration from asynchronous workflows and extends it to the domain of
distributed computation.

## Cloud workflows

In MBrace, the unit of computation is a *cloud workflow:*
*)

let myFirstCloudWorkflow = cloud { return 42 }

(**

Cloud workflows generate objects of type `Cloud<'T>`, which denote a delayed computation
that once executed will yield a result of type `'T`. Cloud workflows are language-integrated
and can freely intertwine with native code.

*)

let mySecondCloudWorkflow = cloud {
    let now = System.DateTime.Now
    printfn "Current time: %O" now
    return ()
}

(**

Note that the specific example introduces a side-effect to the computation.
Due to the distributed nature of cloud workflows, it is unclear where this might
take place once executed.

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

Recursion and higher-order computations are possible:

*)

let rec foldl f s ts = cloud {
    match ts with
    | [] -> return s
    | t :: ts' ->
        let! s' = f s t
        return! foldl f s' ts'
}

(**

and so are for loops and while loops.

*)

cloud {
    for i in [| 1 .. 100 |] do
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

let downloadLines (url : string) = async {
    use http = new System.Net.WebClient()
    let! html = http.AsyncDownloadString(System.Uri url) 
    return html.Split('\n')
}

cloud {
    let download u = Cloud.OfAsync(downloadLines u)
    let! t1 = download "http://www.nessos.gr/"
    let! t2 = download "http://www.m-brace.net/"
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

cloud {
    let download = Cloud.OfAsync << downloadLines
    let! t1,t2 = download "http://www.m-brace.net" <||> download "http://www.nessos.gr/"
    return t1.Length + t2.Length
}

(**

The `<||>` operator defines the *binary parallel combinator*, which combines two workflows
into one where both will executed in fork/join parallelism.
It should be noted that parallelism in this case means that each of the workflows
will be scheduled for execution in remote and potentially separete worker machines
in the MBrace cluster.

The `Cloud.Parallel` combinator is the variadic version of `<||>`,
which can be used for running arbitrarily many cloud workflows in parallel:

*) 

cloud {
    let sqr x = cloud { return x * x }

    let N = System.Random().Next(50,100) // number of parallel jobs determined at runtime
    let! results = Cloud.Parallel(List.map sqr [1.. N])
    return Array.sum results
}

(**

A point of interest here is exception handling. Consider the workflow

*)

cloud {
    let download = Cloud.OfAsync << downloadLines

    try
        let! results =
            [
                "http://www.m-brace.net/"
                "http://www.nessos.gr/"
                "http://non.existent.domain/" ]
            |> List.map download
            |> Cloud.Parallel

        return results |> Array.sumBy(fun r -> r.Length)

    with :? System.Net.WebException as e ->
        // log and reraise
        do! Cloud.Logf "Encountered error %O" e
        return raise e
}

(**

Clearly, one of the child computations will fail on account of
an invalid url, creating an exception. In general, uncaught exceptions
bubble up through `Cloud.Parallel` triggering cancellation of all
outstanding child computations (just like `Async.Parallel`).

The interesting bit here is that the exception handling clause
will almost certainly be executed in a different machine than the one
in which it was originally thrown. This is due to the interpreted
nature of the monadic skeleton of cloud workflows, which allows
exceptions, environments, closures to be passed around worker machines
in a seemingly transparent manner.

### Example: Defining a MapReduce workflow

Cloud worklows in conjunctions with parallel combinators can be used
to articulate MapReduce-like workflows. A simplistic version follows:
*)
(***hide***)
module List =
    /// splits a list into two halves
    let split ts = ts,ts

let download = downloadLines >> Cloud.OfAsync
(** *)

let mapReduce (map : 'T -> 'R) (reduce : 'R -> 'R -> 'R)
                (identity : 'R) (inputs : 'T list) =

    let rec aux inputs = cloud {
        match inputs with
        | [] -> return identity
        | [t] -> return map t
        | _ ->
            let left,right = List.split inputs
            let! r, r' = aux left <||> aux right
            return reduce r r'
    }

    aux inputs

(**

The workflow follows a divide-and-conquer approach, 
recursively partitioning input data until trivial cases are met. 
Recursive calls are passed through the `<||>` operator,
thus achieving the effect of distributed parallelism.

It should be noted that this is indeed a naive conception of mapReduce,
as it does enable data parallelism nor does it take into account cluster granularity. 
For a more in-depth exposition of mapReduce, please refer to the MBrace [manual](mbrace-manual.pdf).

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

## Distributed Data

Cloud workflows offer a programming model for distributed computation. 
But what happens when it comes to big data? While the distributable 
execution environments of mbrace do offer a limited form of data distribution, 
their scope is inherently local and almost certainly do not scale to the demands 
of modern big data applications.  MBrace offers a plethora of mechanisms for managing
data in a more global and massive scale. These provide an essential decoupling 
between distributed computation and distributed data.

### Cloud Refs

The mbrace programming model offers access to persistable and distributed data 
entities known as cloud refs. Cloud refs very much resemble references found in 
the ML family of languages but are "monadic" in nature. In other words, their 
declaration entails a scheduling decision by the runtime. 
The following workflow stores the downloaded content of a web page and returns a cloud ref to it:

*)

let getTextRef () = cloud {
    // download a piece of data
    let! text = download "http://www.m-brace.net/"
    // store data to a new CloudRef
    let! cref = CloudRef.New text
    // return the ref
    return cref
}

(**

Dereferencing a cloud ref can be done by getting its `.Value` property:

*)

let dereference (data : ICloudRef<byte []>) = cloud {
    return data.Value
}

(**

### Example: Data Sharding

An interesting aspect of cloud refs is the ability to define large, distributed data structures. 
For example, one could define a distributed binary tree like so:

*)

type CloudTree<'T> = 
    | Empty
    | Leaf of 'T
    | Branch of TreeRef<'T> * TreeRef<'T>

and TreeRef<'T> = ICloudRef<CloudTree<'T>>

(**

The cloud tree gives rise to a number of naturally distributable combinators. For example

*)

let rec map (f : 'T -> 'S) (ttree : TreeRef<'T>) = cloud { 
    match ttree.Value with
    | Empty -> return! CloudRef.New Empty
    | Leaf t -> return! CloudRef.New <| Leaf (f t)
    | Branch(l,r) ->
        let! l',r' = map f l <||> map f r 
        return! CloudRef.New <| Branch(l', r')
}

(** and *)

let rec reduce (id : 'R) (reduceF : 'R -> 'R -> 'R) (rtree : TreeRef<'R>) = cloud { 
    match rtree.Value with
    | Empty -> return id
    | Leaf r -> return r
    | Branch(l,r) ->
        let! r,r' = reduce id reduceF l <||> reduce id reduceF r 
        return reduceF r r'
}

(**

The above functions enable distributed MapReduce workflows that are 
driven by the structural properties of the cloud tree.
The use of cloud refs accounts for data parallelism in a way not achievable
by the previous MapReduce example.

### Cloud Sequences

While cloud refs are useful for storing relatively small chunks of data, 
they might not scale well when it comes to large collections of objects.
Evaluating a cloud ref that points to a big array may place unnecessary 
memory strain on the runtime. For that reason, mbrace offers the `CloudSeq` 
primitive, a construct similar to the CloudRef that offers access to a collection 
of values with on-demand fetching semantics. The CloudSeq implements the .NET 
`IEnumerable` interface and is immutable, just like `CloudRef`.

*)

let getLines (url : string) =
    cloud {
        let! lines = download url
        let! cseq = CloudSeq.New lines
        return cseq :> seq<string>
    }

(**

### Cloud Files

Like Cloud refs and Cloud sequences, CloudFile is an immutable storage primitive 
that a references a file saved in the global store. 
In other words, it is an interface for storing or accessing binary blobs in the runtime.

*)

cloud {
    // enumerate all files from underlying storage container
    let! files = CloudFile.Enumerate "path/to/container"

    // read a cloud file and return its word count
    let wordCount (f : ICloudFile) = cloud {
        let! text = CloudFile.ReadAllText f
        let count =
            text.Split(' ')
            |> Seq.groupBy id
            |> Seq.map (fun (token,instances) -> token, Seq.length instances)
            |> Seq.toArray

        return f.Name, count
    }

    // perform computation in parallel
    let! results = files |> Seq.map wordCount |> Cloud.Parallel

    // persist results to a new Cloud file
    let serializer = FsPickler.CreateXml()
    let! file = CloudFile.New(fun fs -> async { return serializer.Serialize(fs, results) })
    return file
}

(**

### Mutable Cloud Refs

The `MutableCloudRef` primitive is, similarly to the CloudRef, 
a reference to data saved in the underlying storage provider. 
However,

  * MutableCloudRefs are mutable. The value of a mutable cloud ref can be updated and, as a result,
    its values are never cached. Mutable cloud refs can be updated conditionally using the 
    `MutableCloudRef.Set` methods or forcibly using the `MutableCloudRef.Force` method.

  * MutableCloudRefs can be deallocated manually. This can be done using the `MutableCloudRef.Free` method.
    The MutableCloudRef is a powerful primitive that can be used to create runtime-wide synchronization 
    mechanisms like locks, semaphores, etc.

The following demonstrates simple use of the mutable cloud ref:

*)

let race () = cloud {
    let! mr = MutableCloudRef.New(0)
    let! _ =
        MutableCloudRef.Force(mr,1)
            <||>
        MutableCloudRef.Force(mr,2)

    return! MutableCloudRef.Read(mr)
}

(**

The snippet will return a result of either 1 or 2, depending on which update operation was run last.

The following snippet implements an optimistic incrementing function acting on a mutable cloud ref:

*)

let increment (counter : IMutableCloudRef<int>) = cloud {
    let rec spin () = cloud {
        let! v = MutableCloudRef.Read counter
        let! ok = MutableCloudRef.Set(counter, v + 1)
        if ok then return ()
        else
            return! spin ()
    }

    return! spin ()
}

(**  The same effect can be achieved using the included MutableCloudRef.SpinSet method *)
[<Cloud>]
let increment' counter = MutableCloudRef.SpinSet(counter, fun x -> x + 1)

(**

## Misc Primitives

In this section we describe some of the other 
[primitives](reference/nessos-mbrace-cloud.html) offered by MBrace.

  * `Cloud.GetWorkerCount` : Returns the current number of cluster nodes as reported by the runtime.

  * `Cloud.GetProcessId` : Gets the runtime-assigned cloud process id for the currently executing workflow.

  * `Cloud.GetTaskid` : Gets the runtime-assigned id for the currently executing task. 
    Tasks are units of computation executed by worker nodes.

  * `Cloud.ToLocal` : Dynamically converts a cloud workflow so that it is wholly executed within
    the current worker node using thread parallelism semantics. Useful in divide-and-conquer workflows
    in order to minimize communication after a certain depth.

  * `Cloud.Log` : appends an entry to the cloud process log; this operation introduces runtime communication.

  * `Cloud.Trace` : Dynamically wraps a cloud computation so that trace information is included with
    the user logs. Workflows carrying the `NoTraceInfo` attribute are excluded.


For more information and examples on the programming model, please refer to the 
[MBrace manual](mbrace-manual.pdf).

*)