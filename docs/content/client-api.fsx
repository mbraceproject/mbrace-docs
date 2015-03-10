(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin/"
#I "../../src/MBrace.Client/"
#I "../../src/MBrace.Azure/"

#load "bootstrap.fsx"

#r "FsPickler.dll"
#r "MBrace.Azure.dll"

open System

open Nessos.MBrace
open Nessos.MBrace.Lib
open Nessos.MBrace.Client
open Nessos.MBrace.Runtime.Logging

open Nessos.FsPickler

(**

# MBrace Client API

The following article provides an overview of the MBrace client API,
the collection of types and methods used for interacting with the MBrace runtime.
These include

  1. The cloud workflow [programming model](progamming-model.html).

  2. An interface for managing and interacting with the MBrace runtime, that can 
     be roughly divided in the following categories:

      * Runtime administration functionality, that includes cluster management operations, 
        health monitoring and real-time elastic node management.

      * Cloud process management functionality, that includes submission of computations, 
        process monitoring, debugging and storage access.

  3. The MBrace shell, which enables interactive, on-the-fly declaration, 
     deployment and debugging of cloud computation through the F# REPL.

  4. A collection of command line tools for server-side deployments.

  5. A library of combinators implementing common parallelism workflows like MapReduce 
     or nondeterministic algorithms and a multitude of sample implementations.

## Installation

These can be accessed by adding the [`MBrace.Client`](http://www.nuget.org/packages/MBrace.Client) 
nuget package to projects. Alternatively, they can be consumed from F# interactive 
by installing [`MBrace.Runtime`](http://www.nuget.org/packages/MBrace.Runtime) and loading
*)
#load "../packages/MBrace.Runtime/bootstrap.fsx"

open Nessos.MBrace
open Nessos.MBrace.Client

let runtime = MBrace.InitLocal(totalNodes = 3)

runtime.Run <@ cloud { return 1 + 1 } @>

(**

MBrace is compatible with Visual Studio 2012 and 2013.
If using F# 3.0/Visual Studio 2012, a binding redirect for `FSharp.Core`
needs to be set up

    [lang=xml]
    <configuration>
      <runtime>
        <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
          <dependentAssembly>
            <assemblyIdentity name="FSharp.Core" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
            <bindingRedirect oldVersion="0.0.0.0-4.3.1.0" newVersion="4.3.0.0" />
          </dependentAssembly>
        </assemblyBinding>
      </runtime>
    </configuration>

## The Cloud Attribute

A primary function of the MBrace client is statically traversing cloud workflows for library dependencies, 
extracting metadata to be used for debugging purposes, 
as well as detecting and emitting warnings for potentially invalid patterns. 
This static traversal is in part achieved through the help of F# code quotations. 
Quotations are an excellent resource for metadata and as such, they are utilized by MBrace.

Cloud computations in MBrace are initialized like so:
*)

let proc = runtime.CreateProcess <@ cloud { return 1 + 1 } @>

(**

where runtime denotes a client object to a running MBrace cluster. 
A peculiar feature of this syntax is that cloud blocks are delimited by `<@` and `@>` symbols. 
These are part of the F# language feature known as [code quotations](http://msdn.microsoft.com/en-us/library/dd233212.aspx). 
Any F# expression enclosed in these symbols will yield a typed syntax tree 
of the given expression, also known as its quotation. Evaluating
*)

let expr = <@ 1 + 1 @>

(**

gives a syntax tree which, if evaluated, will yield a result of type `int`.

Even though workflows need to be quoted for execution, it does not mean 
that all referenced workfows have to be expression trees;
only the top-level expression need be so.

*)

[<Cloud>]
let test () = cloud { return 1 + 1 }

runtime.Run <@ test () @>

(**
Despite this, all nested calls to cloud workflows should be affixed with a `[<Cloud>]` attribute. 
Failing to do so will result in warning messages being raised. 
The Cloud attribute is just an abbreviation of the 
[`ReflectedDefinitionAttribute`](http://msdn.microsoft.com/en-us/library/ee353643.aspx) 
provided by F#. Adding this to a declaration makes the F# compiler generate a reflected quotation to the tagged code.

It should be noted that the Cloud attribute is not needed for F# declarations that are not cloud workflows. 
For example, the following code is perfectly valid:

*)

let f x = x + 1

runtime.Run <@ cloud { return f 41 } @>

(**

## Store Providers

Every MBrace runtime requires a storage backend in order for it to function. 
This enables distributed storage primitives like cloud refs and cloud files 
and is used by internally the runtime for logging purposes. 
The MBrace runtime does not provide its own distributed storage implementation, 
rather it relies on pluggable storage providers which can be user defined.

*)

open Nessos.MBrace.Store

let fsStore = FileSystemStore.Create(@"\\FileServer\path\to\mbrace")
let sqlStore = SqlServerStore.Create(connectionString = "connectionString")

open Nessos.MBrace.Azure

let azureStore = AzureStore.Create(accountName = "name", accountKey = "key")

(**

The default store implementation of a client session can be specified by setting

*)

MBraceSettings.DefaultStore <- azureStore

(**

User-defined storage providers can be created by implementing the 
[`ICloudStore`](reference/nessos-mbrace-store-icloudstore.html) interface.

## Managing the MBrace Runtime

The MBrace runtime is a cluster of connected computers capable of 
orchestrating the execution of cloud workflows. 
Every computer participating in an MBrace runtime is called an MBrace node. 
In this section we offer an overview of how the MBrace client stack can be used to initialize, 
manage and monitor remote MBrace runtimes.

### MBraceNode

An [`MBraceNode`](reference/nessos-mbrace-client-mbracenode.html) represents any 
physical machine that runs the MBrace daemon, the server-side component of the framework. 
Every MBrace daemon accepts connections from a predetermined tcp port on the host. 
MBrace nodes are identifiable by the uri format

    [lang=ascii]
    mbrace://hostname:port/

The MBrace client can connect to a remote node by calling

*)

let node  = Node.Connect("mbrace://hostname:2675")
let node' = Node.Connect("hostname", 2675) // equivalent to the above

(** A node in the local machine can be initialized by calling *)

let node'' = Node.Spawn(hostname = "10.0.0.12", primaryPort = 2675)

(**

This will initialize an object of type MBraceNode. 
This object acts as a handle to the remote node. 
It can be used to perform a variety of operations like

*)

node.Ping() // ping the node, returning the number of milliseconds taken

(** or *)

node.IsLocal

(**

which is a property indicating whether the node is part of an existing MBrace cluster.

Every MBrace daemon writes to a log of its own. 
MBrace node logs can accessed remotely from the client either in the form of a dump

*)

node.ShowSystemLogs()

(**

or in a structured format that can be used to perform local queries:

*)

node.GetSystemLogs()
|> Seq.filter (fun entry -> DateTime.Now - entry.Date < TimeSpan.FromDays 1.)

(**

### MBraceRuntime

An MBrace runtime can be booted once access to a collection of at least 3 nodes, 
all running within the same subnet, has been established. This can be done like so:

*)

let nodes = [ node ; node' ; node'' ]

let runtime = MBraceRuntime.Boot(nodes, store = azureStore)

(**

This will initialize an MBrace cluster that connects to a Windows Azure store endpoint. 
Once boot is completed, a handle of type [`MBraceRuntime`](reference/nessos-mbrace-client-mbracenode.html) 
will be returned. If no store is specified explicitly, the MBraceSettings.DefaultStore will be used. 
To connect to an already booted MBrace runtime, one needs simply write

*)

let runtime = MBraceRuntime.Connect("mbrace://host:port/")

(**

wherein the supplied uri can point to any of the constituent worker nodes.

The client stack provides a facility for instantaneously spawning local runtimes:

*)

let runtime = MBraceRuntime.InitLocal(totalNodes = 4, background = true)

(**

This will initiate a runtime of four local nodes that execute in the background. 
The feature is particularly useful for quick deployments of distributed code under development.
The MBraceRuntime object serves as the entry point for any kind of client interactions with the cluster. 
For instance, the property

*)

runtime.Nodes

(**

returns the list of all nodes that constitute the cluster. In the MBrace shell, calling

*)

runtime.ShowInfo()

(**

prints a detailed description of the cluster to the terminal.

    [lang=ascii]
    {m}brace runtime information (active)

    Host           Port  Role
    ----           ----  ----
    grothendieck  38857  Master
    grothendieck  38873  Alt Master  Local (Pid 3616)  mbrace://grothendieck:38873/
    grothendieck  38865  Alt Master  Local (Pid 4952)  mbrace://grothendieck:38865/

The state of the runtime can be reset or stopped at any point by calling the following methods:

*)

runtime.Shutdown() // stops the runtime
runtime.Reboot() // resets the state of the runtime
runtime.Kill() // violently kills all node processes in the runtime

(**

## Managing Cloud Processes

A cloud process is a currently executing or completed cloud computation 
in the context of a specific MBrace runtime. 
In any given runtime, cloud processes can be initialized, monitored for progress, or cancelled; 
completed cloud processes can be queried for their results and 
symbolic stack traces can be fetched for failed executions.

Cloud processes form a fundamental organizational unit for the MBrace runtime: 
conceptually, if one is to think of MBrace as an operating system for the cloud, 
then cloud processes form its units of distributed execution; 
every cloud process spawns its own set of scheduler and workers; 
the MBrace runtime enforces a regime of process isolation, 
which means that each cloud process will run in a distinct 
instance of the CLR in the context of each worker node.

Given a runtime object, a cloud process can be initialized like so:

*)

let proc = runtime.CreateProcess <@ cloud { return 1 + 1 } @>

(**

This will submit the workflow to the runtime for execution and 
return with a process handle of type 
[`Process<int>`](http://nessos.github.io/MBrace/reference/nessos-mbrace-client-process-1.html). 
Various useful properties can be used to query the status 
of the cloud computation at any time. For instance,

*)

proc.Result // Pending, Value, user Exception or system Fault
proc.ProcessId // the cloud process id
proc.InitTime // returns a System.DateTime on execution start
proc.ExecutionTime // returns a System.TimeSpan on execution time
proc.GetLogs() // get user logs for cloud process

(**

If running in the MBrace shell, typing the command

*)

proc.ShowInfo()

(** 

will print information like the following

    [lang=ascii]
    Name       Process Id  Status   #Workers  #Tasks  Start Time         Result Type
    ----       ----------  ------   --------  ------  ----------         -----------
    mapReduce        6674  Running         2       2  30/7/2013 4:08:21  (string * int) []

Similar to `CreateProcess` is the `Run` method:

*)

let result = runtime.Run <@ cloud { return 1 + 1 } @>


(**

This is a blocking version of `CreateProcess` that is equivalent to the statement below:

*)

let proc = runtime.CreateProcess <@ cloud { return 1 + 1 } @> in
proc.AwaitResult()

(**

A list of all executing cloud processes in a given runtime can be obtained as follows:

*)

let procs = runtime.GetAllProcesses()

(**

If running in the MBrace Shell, process information can be printed to the buffer like so:

*)

runtime.ShowProcessInfo()

(**

Given a cloud process id, one can receive the corresponding handle object like so:

*)

let proc = runtime.GetProcess 1131 :?> Process<int>
let proc' = runtime.GetProcess<(string * int) []> 119

(**

Finally, an executing cloud process can be cancelled with the following method

*)

proc.Kill()

(**

## The MBrace Daemon

As mentioned earlier, the MBrace daemon is the server-side application 
that contains a machine-wide instance of the MBrace framework. 
It is initialized by running the `mbraced.exe` executable, 
which can be found in the `tools` folder of the `MBrace.Runtime` nuget package. 
For instance, the command
    
    [lang=bash]
    $ mbraced.exe --hostname 127.0.0.1 --primary-port 2675 --detach

will instantiate a background mbraced process that listens on the loopback interface at port 2675.

### Configuring the MBrace Daemon

The MBrace daemon comes with a range of configuration options. 
These parameters can either be read from the mbraced configuration file, 
or passed as command line arguments, in that evaluation order. 
Command line parameters override those provided by the configuration file.

As is common in .NET applications, mbraced comes with an xml configuration file, 
namely mbraced.exe.config found in the same location as the executable. 
Configuration for mbraced is written in the `AppSettings` section 
of the xml document that follows a key-value schema:

    [lang=xml]
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
        <appSettings>
            <add key="hostname" value="grothendieck.nessos"/>
            <add key="primary port" value="2675"/>
            <add key="worker port range" value="30000, 30042"/>
            <add key="working directory" value="/var/mbraced/"/>
            <add key="log file" value="mbrace-log.txt"/>
            <!-- specify loglevel: info 0, warning 1, error 2-->
            <add key="log level" value="0"/>
            <!-- permitted operation modes; None: 0, Slave: 1, Master: 2, All: 3 -->
            <add key="permissions" value="3" />
            <!-- executable name of mbrace child process -->
            <add key="mbrace processdomain executable" value="mbrace.worker.exe"/>
        </appSettings>
    </configuration>

The full range of command line parameters for mbraced can be viewed by typing

    [lang=bash]
    $ mbraced.exe --help

We now give a brief description of the configuration parameters offered by the daemon:

  * Hostname: the ip address or host name that the daemon listens to. The hostname must be resolvable in the context of
    the entire MBrace cluster. Each instance of mbraced can only have one hostname specified.

  * Primary Port: the tcp port that the local cluster supervisor listens to.

  * Worker Port Range: a range or collection of tcp ports that can be assigned to 
    worker processes spawned by the local cluster supervisor.

  * Working Directory: the local directory in which all local caching is performed. 
    Write permissions are required for the daemon process.

  * Log File: specifies the path to the log file. If relative, 
    it is resolved with respect to the working directory.

  * Log Level: specifies the log level: 0 for info, 1 for warnings, 2 for errors.

  * ProcessDomain Executable: the location of the worker process executable. 
    Relative paths evaluated with respect to the main `mbraced.exe` path.

### Deploying the MBrace Daemon

Once the configuration file for `mbraced` has been set up as desired, 
deploying an instance from the command line is as simple as typing

    [lang=bash]
    $ mbraced --detach

The MBrace framework also comes with the `mbracectl` command line tool 
that can be used to track deployment state. Initiating a session can be done like so:

    [lang=bash]
    $ mbracectl start

This will initialize a background instance with settings read from the mbraced configuration file. 
Session state can be queried by entering
    
    [lang=bash]
    $ mbracectl status

Finally, a session can be ended by typing
    
    [lang=bash]
    $ mbracectl stop

`mbracectl` can also be used to initiate multiple instances on the local machine

    [lang=bash]
    $ mbracectl start --nodes 16 --spawn-window

that can even be booted in a local cluster

    [lang=bash]
    $ mbracectl start --nodes 3 --boot

The MBrace installer also comes bundled with a windows service. 
Initiating `mbraced` as a service will spawn a background instance 
with settings read from the xml configuration file.

*)