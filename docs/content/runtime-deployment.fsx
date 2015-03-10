(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../bin"

#r "Thespian.dll"
#r "Vagrant.dll"
#r "MBrace.Core.dll"
#r "MBrace.Utils.dll"
#r "MBrace.Lib.dll"
#r "MBrace.Store.dll"
#r "MBrace.Runtime.Base.dll"
#r "MBrace.Client.dll"

open Nessos.MBrace
open Nessos.MBrace.Store
open Nessos.MBrace.Client

MBraceSettings.MBracedExecutablePath <- System.IO.Path.Combine(__SOURCE_DIRECTORY__, "../../bin/mbraced.exe")

(**

# Deploying the MBrace Runtime 

In this tutorial you will learn how to install and setup MBrace.

Typically you need some machines to run the MBrace daemons/nodes and a client
that manages the runtime (usually the client runs on a F# interactive instance). Any nodes in a MBrace runtime,
as well as any clients, should be part of the same network in order to work properly. 


## <a name="installservice"></a>Installing the MBrace Runtime Service

In order to get a MBrace node running on a machine you need to download 
the [MBrace.Runtime](http://nuget.org/packages/MBrace.Runtime) package.
Any dependencies and executables are located in the `tools` subdirectory. Alternatively you can 
use the [Install-MBrace.ps1](http://github.com/mbraceproject/MBrace/raw/master/nuget/installer/Install-MBrace.ps1) powershell script that helps you install 
MBrace by downloading `.NET 4.5` if it's needed, downloading the
runtime nuget package, adding firewall exceptions for the MBrace executables and finally 
installing the MBrace runtime as a Windows Service.

Open a PowerShell prompt as administrator, download and run `Install-MBrace.ps1` script.
Before running the installation script you probably need to enable script execution in PowerShell;
you can do this using the [Set-ExecutionPolicy](http://technet.microsoft.com/en-us/library/ee176961.aspx) cmdlet.

    [lang=batchfile]
    PS C:\mbrace > help .\Install-MBrace.ps1
    
    NAME
        C:\mbrace\Install-MBrace.ps1
    
    SYNOPSIS
        Installation script for the MBrace runtime.
    
    SYNTAX
        C:\mbrace\Install-MBrace.ps1 [-AddToPath] [-Service] [[-Directory] <String>] [<CommonParameters>]

    PARAMETERS
        -AddToPath [<SwitchParameter>]
            Add the MBrace.Runtime/tools directory to PATH environment variable. This parameter defaults to false.

        -Service [<SwitchParameter>]
            Install MBrace Windows service. This parameter defaults to true.

        -Directory <String>
            Installation directory. This parameter defaults to the Program Files directory.

    DESCRIPTION
        This script implements the following workflow:
    	    * Install .NET 4.5 if it's needed.
    	    * Download the NuGet standalone and the latest MBrace.Runtime package.
    	    * Add firewall exceptions for the mbraced and mbrace.worker executables.
    	    * Install and starts the MBrace Windows service.
    	Note that administrator rights are required.


    RELATED LINKS
        http://github.com/mbraceproject/MBrace
    	http://nessos.github.io/MBrace
    	http://www.m-brace.net/

            
    PS C:\mbrace > Set-ExecutionPolicy Unrestricted
    PS C:\mbrace > .\Install-MBrace.ps1
    * Checking for admin permissions...
    * Checking if .NET 4.5 installed...
    * Downloading NuGet...
    * Downloading MBrace.Runtime...
    * Adding firewall rules...
    * Installing MBrace service...
    * Done...


At this point you should have the `MBrace Runtime` service running. You can confirm this by using the `Get-Service` cmdlet or the Services tool.

    [lang=batchfile]
    c:\mbrace > Get-Service MBrace
            
    Status   Name               DisplayName
    ------   ----               -----------
    Running  MBrace             MBrace Runtime


## <a name="configureservice"></a>Configuring MBrace
Now you need to make any configurations to the MBrace daemon by changing the `mbraced.exe.config` file.
Normally you don't need to change this file and you can skip this step.
Below you can see the options you can configure.

* `hostname` This is the hostname used by the daemon and the MBrace API. It can be either the hostname of the host machine or
an IP address. You can also specify an IP range in CIDR format; in this case the hostname of the daemon will be
the address of the first interface that belongs to the given range. Leave this setting blank to use the machine's hostname.
* `primary port` The TCP port used by the daemon.
* `worker ports` Available port for use by mbrace workers.
* `worker port range` Available port range for use by mbrace workers.
* `permissions` Permitted operation modes for this node; None: 0, Slave: 1, Master: 2, All: 3.
* `working directory` Specifies the working directory. This is the place where assemblies and the cache will be stored.
Use `temp` for the system temp folder.
* `log file` Specifies a log file for the daemon. This can either be an absolute path or a relative path to the `working directory`.
* `log level` Specify logs verbosity. Use 2 to display only Errors, 1 to also display Warnings and 0 for full verbosity.
* `mbrace processdomain executable` Executable name for `mbrace.worker.exe`.

You can see and example of the appSettings section of a `mbraced.exe.config` file.

    [lang=xml]
    <appSettings>
        <!-- hostname that uniquely and globally identifies the host machine of the daemon. This can be an IP address. -->
        <add key="hostname" value="10.0.1.0/27" /> 
        <!-- Primary TCP port -->
        <add key="primary port" value="2675" />
        <!-- available ports for mbrace workers -->
        <add key="worker port range" value="30000, 30020" />
        <!-- permitted operation modes; None: 0, Slave: 1, Master: 2, All: 3 -->
        <add key="permissions" value="3" />
        <!-- the working directory of the node; paths relative to executable; use "temp" for system temp folder -->
        <add key="working directory" value="temp" />
        <!-- logfile; paths relative to declared working directory -->
        <add key="log file" value="mbrace-log.txt" />
        <!-- specify loglevel: info 0, warning 1, error 2-->
        <add key="log level" value="0" />
        <!-- executable name of mbrace child process -->
        <add key="mbrace processdomain executable" value="mbrace.worker.exe" />
    </appSettings>


Finally you need to restart the service in order to load the new configuration.
    
    [lang=batchfile]
    c:\mbrace > Restart-Service MBrace

You can repeat this process to install MBrace on multiple machines.

## <a name="bootruntime"></a>Booting the MBrace Runtime

At this point your machines are running and the MBrace nodes are idle.
At your client machine, you need to install the F# interactive (or optionally Visual Studio or any environment supporting F#)
as well as the [MBrace.Runtime](http://www.nuget.org/packages/MBrace.Runtime) package from nuget.

In your solution open the `mbrace-tutorial.fsx`. In the _initialize a runtime of remote nodes_ section
change the hostnames and ports. Depending on your configuration use either IP addresses
*)

let nodes =
    [
        MBraceNode.Connect("10.0.1.4", 2675)
        MBraceNode.Connect("10.0.1.5", 2675)
        MBraceNode.Connect("10.0.1.6", 2675)
    ]

(**
or hostnames
*)
let nodes = [1..3] |> List.map (fun i -> MBraceNode.Connect(sprintf "machine%d" i, 2675))

(**
Now ping the nodes to ensure connectivity
*)

nodes |> List.iter (fun n -> printfn "Node %A : %s" n <| try n.Ping().ToString() with _ -> "failed")

(**
At this point, before actually booting a runtime you need to configure which store
will be used by the runtime. By default each node will use its local filesystem, 
but in distributed scenarios you need to use storage common to all participating nodes like
the _Windows Azure Blob Storage_, _SQL Server_, or any implementation of the `ICloudStore` interface.
In this example we are going to use the `FileSystemStore` that comes with MBrace
with a UNC path that is accesible to all nodes. _Note:_ The FileSystemStore should be used only for
testing purposes.

*)

open Nessos.MBrace.Store

let fileSystemStore = FileSystemStore.Create @"\\shared\mbrace"

MBraceSettings.DefaultStore <- fileSystemStore

(**
Finally boot the runtime and send your first cloud computation.
*)

let runtime = MBrace.Boot nodes

runtime.Run <@ cloud { return 42 } @>
