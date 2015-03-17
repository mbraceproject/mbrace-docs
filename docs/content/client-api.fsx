(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.

(**

# MBrace Azure Client API

The following article provides an overview of the MBrace Azure PaaS API,
the collection of types and methods used for interacting with the Azure runtime.

## Installation

These can be accessed by adding the [`MBrace.Azure`](http://www.nuget.org/packages/MBrace.Azure) 
nuget package to projects. Alternatively, they can be consumed from F# interactive 
by installing [`MBrace.Azure.Client`](http://www.nuget.org/packages/MBrace.Azure.Client`) and loading
*)

#load "../../packages/MBrace.Azure.Client/bootstrap.fsx"

open MBrace
open MBrace.Azure
open MBrace.Azure.Client

let config =
    { Configuration.Default with
        ServiceBusConnectionString = "service bus connection string"
        StorageConnectionString = "storage connection string" }

let runtime = Runtime.GetHandle(config)

let rec f n = if n <= 1 then n else f(n-2) + f(n-1)

runtime.Run (cloud { return f 20 })

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

*)