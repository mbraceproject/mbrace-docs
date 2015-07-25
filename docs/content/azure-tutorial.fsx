(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "../../packages/MBrace.Azure.Standalone/MBrace.Azure.fsx"
#r "../../packages/MBrace.Flow/lib/net45/MBrace.Flow.dll"


let config = Unchecked.defaultof<MBrace.Azure.Configuration>

(**

# Using MBrace on Windows Azure 

In this tutorial you will learn how to setup MBrace on Windows Azure and
how to use the MBrace Azure Client API.


## Provisioning Your Cluster 

### Provisioning Your Cluster Using Brisk Engine 

The easiest path to provision an MBrace cluster on Azure using
an Azure Cloud Service is to use [Brisk Engine](http://briskengine.com).
See [Getting Started with MBrace using Brisk Engine](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/azure/brisk-tutorial.md#get-started-with-brisk).

> *IMPORTANT NOTE*: MBrace.Azure is currently optimized for clients using F# 3.1 (Visual Studio 2013, FSharp.Core 4.3.1.0).
>
> If using F# 3.0 (Visual Studio 2012) or F# 4.0 (Visual Studio 2015), 
> you must provision a bespoke Azure cloud service and add a binding redirect 
> for FSharp.Core to [app.config](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/azure/CustomCloudService/MBraceAzureRole/app.config)
> as described further below.  

### Provisioning Your Cluster using an Explicit Azure Cloud Service 



In some cases you may decide to provision explicitly, if the options provided
by Brisk Engine do not yet meet your needs.  

If so, see [Creating a Custom Azure Cloud Service which includes MBrace runtime instances](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/azure/AZURE.md).

For example you may want to:

* use Visual Studio 2012 (F# 3.0, FSharp.Core 4.3.0.0) or Visual Studio 2015 (F# 4.0, FSharp.Core 4.4.0.0) as your client (see below); or

* adjust the size of VM used for worker instances in your cluster; or

* add endpoints to your cloud service (so your MBrace cluster can publish 
  TCP and HTTP endpoints, either public or to your virtual network, 
  for example, you want your MBrace cluster to publish a web server); or

* enable Remote Access to MBrace cluster worker instances; or

* specify the size of local storage available on MBrace cluster worker instances; or

* upload certificates as part of your provisioning process; or

* specify Azure-specific local caching options; or

* include additional web and worker roles in your cloud service; or

* compile and deploy your own version of the MBrace cluster worker instance software. 

At the time of writing these options were not yet via the Brisk Engine provisioning
service. In this case, you must create and deploy your own Azure cloud service.

In order to provision explicitly, as a prerequisite you need 
to have a Windows Azure account, basic knowledge of the Azure computing and
optionally Microsoft Visual Studio (or any environment supporting F#).



### Using MBrace.Azure with F# 3.0 (Visual Studio 2012, FSharp.Core 4.3.0.0) Clients

If your client is using F# 3.0 (Visual Studio 2012, FSharp.Core 4.3.0.0), you must use 
a [Custom Azure Cloud Service](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/azure/AZURE.md), and 
adjust the binding redirect for `FSharp.Core` in [app.config](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/azure/CustomCloudService/MBraceAzureRole/app.config):

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

### Using MBrace.Azure with F# 4.0 (Visual Studio 2015, FSharp.Core 4.4.0.0) Clients

If your client is using F# 4.0 (Visual Studio 2015, FSharp.Core 4.4.0.0), you must also use 
a [Custom Azure Cloud Service](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/azure/AZURE.md), and 
adjust the binding redirect for `FSharp.Core` in [app.config](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/azure/CustomCloudService/MBraceAzureRole/app.config):

    [lang=xml]
    <configuration>
      <runtime>
        <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
          <dependentAssembly>
            <assemblyIdentity name="FSharp.Core" publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
            <bindingRedirect oldVersion="0.0.0.0-4.4.0.0" newVersion="4.4.0.0" />
          </dependentAssembly>
        </assemblyBinding>
      </runtime>
    </configuration>


### How your MBrace client code runs

Typically the MBrace client will run in:

* an F# interactive instance in your Visual Studio session; or

* as part of another cloud service, website or web job; or

* as a client desktop process.

In all cases, the client will need sufficient network access to be able to write to the
Azure storage and Service Bus accounts used by the MBrace runtime/cluster nodes.

Parts of your client code will be transported to the MBrace.Azure service and executed
there. You should be careful that the version dependencies between base
architectures, operating systems and .NET versions are compatible and that
both client and MBrace.Azure service use the same FSharp.Core.

*)

