(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "../../packages/MBrace.Azure/MBrace.Azure.fsx"
#r "../../packages/MBrace.Flow/lib/net45/MBrace.Flow.dll"


let config = Unchecked.defaultof<MBrace.Azure.Configuration>

(**

# Using MBrace on Azure 

In this tutorial you will learn how to setup MBrace on Azure.

First, [create a Custom Azure Cloud Service which includes MBrace runtime instances](starterkit/azure/README.html).

Next, [download the appropriate starter kit and walk through the hands-on tutorial scripts](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/mbrace-versions.md).




### How your MBrace client code runs

Typically your MBrace client will run in:

* an F# interactive instance in your editor; or

* as part of another cloud service, website or web job; or

* as a client desktop process.

In all cases, the client will need sufficient network access to be able to write to the
Azure storage and Service Bus accounts used by the MBrace runtime/cluster nodes.

Parts of your client code will be transported to the MBrace.Azure service and executed
there. You should be careful that the version dependencies between base
architectures, operating systems and .NET versions are compatible and that
both client and MBrace.Azure service use the same FSharp.Core.

*)

