(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "../../packages/MBrace.Azure/MBrace.Azure.fsx"
#r "../../packages/MBrace.Flow/lib/net45/MBrace.Flow.dll"


let config = Unchecked.defaultof<MBrace.Azure.Configuration>

(**

# Using MBrace on Azure 

In this tutorial you will learn how to setup MBrace on Azure.

1. Create an Azure account using the [Azure management portal](https://manage.windowsazure.com/).
2. [Provision a Custom Azure Cloud Service which includes MBrace runtime instances](starterkit/azure/README.html).
3. [Download or clone the starter pack](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/mbrace-versions.md).
   Enter your connection strings in ``AzureCluster.fsx`` and adjust each tutorial script to load this script.

The scripts follow the tutorials in the [Core Programming Model](programming-model.html).

### Using your Azure cluster from compiled code

Reference the ``MBrace.Azure`` package and add the following code, after insertng your connection strings
or acquiring them by other means:

    let myStorageConnectionString = "..."
    let myServiceBusConnectionString = "..."
    let config = Configuration(myStorageConnectionString, myServiceBusConnectionString)
    let cluster = AzureCluster.Connect(config, logger = ConsoleLogger(true), logLevel = LogLevel.Info)


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

