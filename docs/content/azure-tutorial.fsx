(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "../../packages/MBrace.Azure.Client/bootstrap.fsx"


open MBrace
open MBrace.Azure
open MBrace.Azure.Client
open MBrace.Azure.Runtime

let config = Unchecked.defaultof<Configuration>

(**

# Using MBrace on Windows Azure 

In this tutorial you will learn how to setup MBrace on Windows Azure.

## Using Brisk Engine to Provision

The easiest path to provision an MBrace cluster on Azure using
an Azure Cloud Service is to use Brisk Engine.
See the [Brisk Engine Tutorial](brisk-tutorial.html).

## Provisioning Explicitly

In some cases you may decide to provision explicitly.  For example, you may want your cloud service
to have access to endpoint funcitonality not yet available via the Brisk Engine provisioning
service.

As a prerequisite you need to have a Windows Azure account, basic knowledge of the Azure computing and
optionally Microsoft Visual Studio (or any environment supporting F#).


Typically you will host your MBrace runtime/cluster nodes in one of

* explicit virtual machines (VMs); or

* the VMs of an Azure cloud service; or 

* the execution environment of an Azure web job.

Typically the MBrace client will run in:

* an F# interactive instance in your Visual Studio session; or

* as part of another Azure cloud service, website or web job; or

* as a client desktop process.

In all cases, the client will need sufficient network access to be able to write to the
Azure storage and Service Bus accounts used by the MBrace runtime/cluster nodes.

## Using a Cloud Service for the MBrace Runtime

This is the normal option.

See [using an Azure cloud service for the MBrace runtime instances](https://github.com/mbraceproject/MBrace.Demos/blob/master/azure/AZURE.md).



## Using a virtual network 

Any nodes in a MBrace runtime as well as any clients, should be part of the 
same network in order to work properly. 
Because of this restriction in this tutorial you have two choices: you can either 
use one of the virtual machines as a client or you can use
the Windows Azure VPN service to join the virtual network created in Azure 
and access the runtime from a remote client (your on-premises computer).

At this point you should decide if you are going to use a remote client or one of
the virtual machines as a client. If you choose the first 
option you need to create a virtual network, create and upload certificates and finally configure your VPN client. 
The process is described in [here](http://msdn.microsoft.com/en-us/library/azure/dn133792.aspx).

You can skip this step if you want to use a client in one of your virtual machines, as a virtual network will be automatically created
for you during the virtual machine creation.

*)
