using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Brutal.Dev.StrongNameSigner;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Brutal.Dev.StrongNameSigner")]
[assembly: AssemblyDescription("Simple API to sign .NET assemblies with a strong-name key and fix assembly references.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("https://brutaldev.com")]
[assembly: AssemblyProduct("Brutal.Dev.StrongNameSigner")]
[assembly: AssemblyCopyright("Copyright © 2013-2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("64d853c0-07b2-4891-8f9a-352c00669028")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.9.0.0")]
[assembly: AssemblyFileVersion("2.9.0.0")]

// These assemblies are used by Cecil, and reading assemblies with symbols without these DLL's present
// will cause an error ("No Symbols Found"). So to ensure that these are actually referenced by 
// StrongNameSigner and copied along to the output directory as well as the UnitTests when running 
// them, we use this "hack".
[assembly: ForceAssemblyReference(typeof(Mono.Cecil.Pdb.NativePdbReader))]
[assembly: ForceAssemblyReference(typeof(Mono.Cecil.Mdb.MdbReader))]
[assembly: ForceAssemblyReference(typeof(Mono.Cecil.Rocks.TypeDefinitionRocks))]
