(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "../../packages/MBrace.Azure/MBrace.Azure.fsx"
#r "../../packages/MBrace.Flow/lib/net45/MBrace.Flow.dll"


let config = Unchecked.defaultof<MBrace.Azure.Configuration>

(**

# Using MBrace on Azure Cloud Services

In this tutorial you will learn how to setup MBrace on Azure and how to use the MBrace Azure Client API.

First, [create a Custom Azure Cloud Service which includes MBrace runtime instances](https://github.com/mbraceproject/MBrace.StarterKit/blob/master/azure/README.md).

_Customization_: During configuration (and prior to deployment) you may want to:

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

In order to provision explicitly, as a prerequisite you need 
to have an Azure account, basic knowledge of the Azure computing and
an editing environment supporting F#.



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


### How your MBrace client code runs

Typically the MBrace client will run in:

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

