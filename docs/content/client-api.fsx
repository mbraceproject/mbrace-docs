(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "../../packages/MBrace.Azure.Client/bootstrap.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client

let config = Unchecked.defaultof<Configuration>

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

  3. The MBrace integration into F# Interactive, which enables interactive, on-the-fly declaration, 
     deployment and debugging of cloud computation through the F# REPL.

  4. A collection of command line tools for server-side deployments.

  5. A library of combinators implementing common parallelism workflows like MapReduce 
     or nondeterministic algorithms and a multitude of sample implementations.

## Installation

For MBrace.Local, use the MBrace.SampleRuntime in the samples.

For MBrace.Azure, these can be accessed by adding 
the [`MBrace.Azure`](http://www.nuget.org/packages/MBrace.Azure) 
nuget package to projects. Alternatively, they can be consumed from F# interactive 
by installing [`MBrace.Azure.Client`](http://www.nuget.org/packages/MBrace.Azure.Client) and loading
*)
#load "../../packages/MBrace.Azure.Client/bootstrap.fsx"


//let runtime = MBrace.SampleRuntime.InitLocal(totalNodes = 3)
let runtime = MBrace.Azure.Client.Runtime.GetHandle(config)

runtime.Run (cloud { return 1 + 1 })

(**

MBrace is compatible with Visual Studio 2012 and above.
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

## Job Creation 

A primary function of the MBrace client is statically traversing cloud workflows for library dependencies, 
extracting metadata to be used for debugging purposes, 
as well as detecting and emitting warnings for potentially invalid patterns. 

Cloud computations in MBrace are initialized like so:
*)

let job1 = runtime.CreateProcess (cloud { return 1 + 1 })

(**

where runtime denotes a client object to a running MBrace cluster.  

You can also call functions both in and outside cloud computations:
*)

let test () = cloud { return 1 + 1 }

runtime.Run (test ())

let f x = x + 1

runtime.Run (cloud { return f 41 })


(**

## Store Providers

Every MBrace runtime requires a storage backend in order for it to function. 
This enables distributed storage primitives like cloud refs and cloud files 
and is used by internally the runtime for logging purposes. 
The MBrace runtime does not provide its own distributed storage implementation, 
rather it relies on pluggable storage providers which can be user defined.

User-defined storage providers can be created by implementing the 
`MBrace.Store.ICloudFileStore` interface.

## Managing the MBrace Runtime

The MBrace runtime is a cluster of connected computers capable of 
orchestrating the execution of cloud workflows. 
Every computer participating in an MBrace runtime is called an MBrace node. 
In this section we offer an overview of how the MBrace client stack can be used to initialize, 
manage and monitor remote MBrace runtimes.

### WorkerRef

`WorkerRef` represents any physical machine that runs the MBrace daemon, the server-side component of the framework. 
Every MBrace daemon accepts connections from a predetermined tcp port on the host. 
MBrace nodes are identifiable by the uri format

    [lang=ascii]
    mbrace://hostname:port/



*)

let workers = runtime.GetWorkers() |> Seq.toArray

let worker = workers.[0]

worker.HeartbeatTime // indicate when the worker last recorded its existence

(**

Every MBrace daemon writes to a log of its own. 
MBrace node logs can accessed remotely from the client either in the form of a dump

*)

runtime.ShowLogs()

(**

returns the list of all nodes that constitute the cluster. In the MBrace shell, calling

*)

runtime.ShowProcesses()

(**

prints a detailed description of the cluster to the terminal.


The state of the runtime can be reset or stopped at any point by calling the following methods:

*)

runtime.Reset() // resets the runtime

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

let job2 = runtime.CreateProcess (cloud { return 1 + 1 })

(**

This will submit the workflow to the runtime for execution and 
return with a process handle of type 
[`Process<int>`](http://nessos.github.io/MBrace/reference/nessos-mbrace-client-process-1.html). 
Various useful properties can be used to query the status 
of the cloud computation at any time. For instance,

*)

job2.AwaitResult() // Pending, Value, user Exception or system Fault
job2.Id // the cloud process id
job2.InitializationTime // returns a System.DateTime on execution start
job2.ExecutionTime // returns a System.TimeSpan on execution time
job2.GetLogs() // get user logs for cloud process

(**

If running in the MBrace shell, typing the command

*)

job2.ShowInfo()

(** 

will print information like the following

    [lang=ascii]
    Name       Process Id  Status   #Workers  #Tasks  Start Time         Result Type
    ----       ----------  ------   --------  ------  ----------         -----------
    mapReduce        6674  Running         2       2  30/7/2013 4:08:21  (string * int) []

Similar to `CreateProcess` is the `Run` method:

*)

let result = runtime.Run (cloud { return 1 + 1 })


(**

This is a blocking version of `CreateProcess` that is equivalent to the statement below:

*)

let job3 = runtime.CreateProcess (cloud { return 1 + 1 }) 
job3.AwaitResult()

(**

If running in the MBrace Shell, process information can be printed to the buffer like so:

*)

runtime.ShowProcesses()


(**

Given a cloud process id, one can receive the corresponding handle object like so:

*)

let job4 = runtime.GetProcess("1131")

(**

Finally, an executing cloud process can be cancelled with the following method

*)

job4.Kill()

(**

## The MBrace Daemon (for MBrace.SampleRuntime sample)

As mentioned earlier, the MBrace daemon is the server-side application 
that contains a machine-wide instance of the MBrace framework. 
It is initialized by running the `mbraced.exe` executable, 
which can be found in the `tools` folder of the `MBrace.Runtime` nuget package. 
For instance, the command
    
    [lang=bash]
    $ mbraced.exe --hostname 127.0.0.1 --primary-port 2675 --detach

will instantiate a background mbraced process that listens on the loopback interface at port 2675.

### Configuring the MBrace Daemon (for MBrace.SampleRuntime sample)

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

### Deploying the MBrace Daemon (for MBrace.SampleRuntime sample)

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