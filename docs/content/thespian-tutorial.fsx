(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "../../packages/MBrace.Thespian/MBrace.Thespian.fsx"


(**

# Using MBrace with a 'Thespian' Locally Simulated Cluster

You can use the MBrace with a locally simulated cloud fabric
called "MBrace.Thespian".  This utilizes the multi-core capabillities of your
machine and is independent of any particular cloud provider, but supports the
same programming model as MBrace.Azure and other MBrace implementations.  This lets
you learn the MBrace cloud programming model in a provider-independent way.

## Provisioning and Using Your 'Thespian' Locally Simulated Cluster 

To provision your cluster, the easiest way is to [download or clone the starter pack for MBrace.Thespian](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/mbrace-versions.md).
Build the solution to get the required NuGet packages.  

Then follow the tutorials in the [Core Programming Model](programming-model.html) to learn MBrace programming with your cluster.

## Initializing Thespian Manually

Your cluster workers will be created automaticaly when using the scripts in the starter pack. You can also
initialize manually (e.g. from an application) as follows:
*)

open MBrace.Thespian
let cluster = 
    ThespianCluster.InitOnCurrentMachine(workerCount = 4, 
                                         logger = ConsoleLogger(), 
                                         logLevel = LogLevel.Info)

(**
You can create a multi-machine cluster using instances of ``ThespianWorker`` and ``InitOnWorker``. This
is not covered in this tutorial.

*)
