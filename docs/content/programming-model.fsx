(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "../../packages/MBrace.Azure.Standalone/MBrace.Azure.fsx"
#r "../../packages/MBrace.Flow/lib/net45/MBrace.Flow.dll"

open MBrace.Core
open MBrace.Store
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Flow

let config = Unchecked.defaultof<Configuration>

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

/// Sequentially fold along a set of jobs
let rec foldLeftCloud f state ts = cloud {
    match ts with
    | [] -> return state
    | t :: ts' ->
        let! s' = f state t
        return! foldLeftCloud f s' ts'
}

(**

and so are for loops and while loops.

*)

/// Sequentially iterate and loop 
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

Clearly, one of the child computations will fail on account of
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

## Distributed Data

Cloud workflows offer a programming model for distributed computation. 
But what happens when it comes to data? 

Small-to-medium-scale data can be transported implicitly as part of a 
cloud computation. This offers a limited (though extremely convenient) form of 
data distribution. However, it will not scale to all needs, particularly computations
involving gigabyts or petabytes of data.

MBrace offers a range of mechanisms for managing
large-scale data in a more global and massive scale. These provide an essential decoupling 
between distributed computation and distributed data.

See also this [summary of the MBrace abstractions for cloud data](https://github.com/mbraceproject/MBrace.Design/blob/master/DataAbstractions.md).

### Cloud Values

The mbrace programming model offers access to persistable and distributed data 
entities known as cloud cells. Cloud cells very much resemble immutable data values found in 
F# but are stored in persisted cloud storage. The following workflow stores the downloaded 
content of a web page and returns a cloud cell to it:

*)

let getTextCell () = cloud {
    // download a piece of data
    let! text = downloadCloud "http://www.m-brace.net/"
    // store data to a new CloudValue
    let! cref = CloudValue.New text
    // return the ref
    return cref
}

(**

Dereferencing a cloud cell can be done by getting its `.Value` property:

*)

let dereference (data : CloudValue<byte []>) = cloud {
    let! v = data.Value
    return v
}

(** 
Cloud cells are stored in named locations and can be resurrected (parsed) from
this locations using `CloudValue.Parse`, supplying a type.

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
For a more in-depth exposition of mapReduce, please refer to the MBrace [manual](mbrace-manual.pdf).

*)

(**

### Example: Distributed Binary Trees

Cloud values can be used to define distributed data structures. 
For example, one could define a distributed binary tree like so:

*)

type CloudTree<'T> = 
    | Empty
    | Leaf of 'T
    | Branch of TreeRef<'T> * TreeRef<'T>

and TreeRef<'T> = CloudValue<CloudTree<'T>>

(**

The cloud tree gives rise to a number of naturally distributable combinators. For example

*)

let rec map (f : 'T -> 'S) (ttree : TreeRef<'T>) = cloud { 
    let! value = ttree.Value
    match value with
    | Empty -> return! CloudValue.New Empty
    | Leaf t -> return! CloudValue.New <| Leaf (f t)
    | Branch(l,r) ->
        let! l',r' = map f l <||> map f r 
        return! CloudValue.New <| Branch(l', r')
}

(** and *)

let rec reduce (id : 'R) (reduceF : 'R -> 'R -> 'R) (rtree : TreeRef<'R>) = cloud { 
    let! value = rtree.Value
    match value with
    | Empty -> return id
    | Leaf r -> return r
    | Branch(l,r) ->
        let! r,r' = reduce id reduceF l <||> reduce id reduceF r 
        return reduceF r r'
}

(**

The above functions enable distributed workflows that are 
driven by the structural properties of the cloud tree.

*)

(**
### Cloud Sequences and Cloud Vectors

While cloud values are useful for storing relatively small chunks of data, 
they might not scale well when it comes to large collections of objects.
Evaluating a cloud cell that points to a big array may place unnecessary 
memory strain on the runtime. For that reason, mbrace offers the `CloudSequence` 
primitive, a construct similar to the CloudValue that offers access to a collection 
of values with on-demand fetching semantics. The CloudSequence type
is immutable and by default may be cached on local worker nodes, 
just like `CloudValue`.

*)

/// Download the page and store it as a new cloud sequence of lines
let storePagesInCloudSequence (url : string) =
    cloud {
        let! lines = downloadCloud url
        let! cseq = CloudSequence.New lines
        return cseq
    }


/// Download the pages and store them in cloud sequences.
let storePagesInCloudSequences (urls : string list) =
    urls 
    |> List.map storePagesInCloudSequence 
    |> Cloud.Parallel

(**

A collection of cloud sequences can then be usesd as input into a cloud flow:

*)

cloud { 
    let! sequences = storePagesInCloudSequences [ "http://google.com"; "http://bing.com" ]

    return 
        sequences 
        |> CloudFlow.OfCloudSequences 
        |> CloudFlow.map (fun x -> x)
}
(**

### Cloud Files

Like cloud values, sequences and vectors, CloudFile is an immutable storage primitive 
that a references a file saved in the global store. 
In other words, it is an interface for storing or accessing binary blobs in the runtime.

*)

open Nessos.FsPickler

cloud {
    // enumerate all files from underlying storage container
    let! files = CloudFile.Enumerate "path/to/container"

    // read a cloud file and return its word count
    let wordCount (f : CloudFile) = cloud {
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

    // persist results to a new Cloud file in XML format
    let serializer = FsPickler.CreateXml()
    let! file = CloudFile.Create("path/file.xml",fun fs -> async { return serializer.Serialize(fs, results) })
    return file
}

(**

### Mutable cloud values (CloudAtom)

The `CloudAtom` primitive is, like `CloudValue`, a reference to data saved in the underlying storage provider. 
However,

  * CloudAtoms are mutable. The value of a cloud atom can be updated and, as a result,
    its values are never cached. Mutable cloud cells can be updated transactionally using the 
    `CloudAtom.Transact` methods or forcibly using the `CloudAtom.Force` method.

  * CloudAtoms can be deallocated manually. This can be done using the `CloudAtom.Free` method.
 
The CloudAtom is a powerful primitive that can be used to create runtime-wide synchronization 
mechanisms like locks, semaphores, etc.

The following demonstrates simple use of the cloud atom:

*)

let race () = cloud {
    let! mr = CloudAtom.New(0)
    let! _ =
        CloudAtom.Force(mr,1)
            <||>
        CloudAtom.Force(mr,2)

    return! CloudAtom.Read(mr)
}

(**

The snippet will return a result of either 1 or 2, depending on which update operation was run last.

The following snippet implements an transactionally incrementing function acting on a cloud atom:

*)

let increment (counter : ICloudAtom<int>) = cloud {
    let! v = CloudAtom.Read counter
    let! ok = CloudAtom.Transact(counter, (fun v -> true, v + 1))
    return ok
}

(**

## Other Primitives

Some other primitives offered by MBrace are:

  * `Cloud.GetWorkerCount` : Returns the current number of cluster nodes as reported by the runtime.

  * `Cloud.GetProcessId` : Gets the runtime-assigned cloud process id for the currently executing workflow.

  * `Cloud.Log` : appends an entry to the cloud process log; this operation introduces runtime communication.


For more information and examples on the programming model, please refer to the 
[API Reference](reference/index.html).

*)