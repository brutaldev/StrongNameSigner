.NET Assembly Strong-Name Signer
=================
[![Build status](https://ci.appveyor.com/api/projects/status/www2a5bfbrwn8piu)](https://ci.appveyor.com/project/brutaldev/strongnamesigner)

Utility software to strong-name sign .NET assemblies, including assemblies you do not have the source code for. If you strong-name sign your own projects you may have noticed that if you reference an unsigned third party assembly you get an error similar to "*Referenced assembly 'A.B.C' does not have a strong name*". If you did not create this assembly, you can use this tool to sign the assembly with your own (or temporarily generated) strong-name key. The tool will also re-write the assembly references (as well as any InternalsVisibleTo references) to match the new signed versions of the assemblies you create.

* [Download Installer](http://brutaldev.com/download/StrongNameSigner_Setup.exe)
* [NuGet Package](https://www.nuget.org/packages/Brutal.Dev.StrongNameSigner/)
* [More Information](http://brutaldev.com/post/2013/10/18/NET-Assembly-Strong-Name-Signer)

Screenshots
-----------
![User Interface](https://raw.github.com/brutaldev/StrongNameSigner/master/screenshots/StrongNameSigner_UI.png)

![Console](https://raw.github.com/brutaldev/StrongNameSigner/master/screenshots/StrongNameSigner_Console.png)

![Help](https://raw.github.com/brutaldev/StrongNameSigner/master/screenshots/StrongNameSigner_Help.png)

Build Process
-------------
You can call the console version in your Visual Studio project files to sign assemblies before it gets built. This will change the references to your assemblies to strong-name signed ones allowing to sign your own projects and reference unsigned assemblies. All assemblies that are found (including signed ones) will have their references corrected if they were using any files that now have public key tokens.

For example, if you want to strong-name sign and fix references to all the NuGet packages that your project uses, you can add this to you Visual Studio project file:

```xml
<Target Name="BeforeBuild">
  <Exec ContinueOnError="false"
        Command="&quot;C:\Program Files\BrutalDev\.NET Assembly Strong-Name Signer\StrongNameSigner.Console.exe&quot; -in &quot;..\packages&quot;" />
</Target>
```

If you are making use of the [NuGet package](https://www.nuget.org/packages/Brutal.Dev.StrongNameSigner/), you can make the call from the `packages` directory like this instead (replace with you own `packages` path):

```xml
<Target Name="BeforeBuild">
  <Exec ContinueOnError="false"
        Command="&quot;..\packages\Brutal.Dev.StrongNameSigner.1.5.1\tools\StrongNameSigner.Console.exe&quot; -in &quot;..\packages&quot;" />
</Target>
```

Often different packages have dependencies that are not signed, but those assemblies are in different directories. To correctly resolve references to dependant assemblies, all required assemblies and the dependencies they reference need to be processed at the same time.
Elmah is a good example of this. Additional Elmah libraries reference Elmah core, but do not include it in the package, they are installed separately. In order to fix the references to Elmah core, it needs to be able to cross check all signed files so you should sign all of them together.

To add multiple directories to process at the same time (similar to how the UI can process a number of assemblies at once in the grid) just pipe **|** delimit your input directory list.

```xml
<Target Name="BeforeBuild">
  <Exec ContinueOnError="false"
        Command="&quot;..\packages\Brutal.Dev.StrongNameSigner.1.5.1\tools\StrongNameSigner.Console.exe&quot; -in &quot;..\packages\elmah.corelibrary.1.2.2|..\packages\Elmah.MVC.2.1.1&quot;" />
</Target>
```

This way the core library can be signed and the MVC library can have it's reference to the new signed version updated since they will be processed together and each file can be verified against each other after signing.
As a rule of thumb, always include all libraries that will be affected by any signing in a single call.

Note that any files that are already strong-name signed will not be modified unless they reference a previously unsigned assembly. If you are using NuGet package restore then this works on build servers as well.

Another alternative is to simply call the `StrongNameSigner.Console.exe` with relevant argument as a pre-build step.

`"C:\Program Files\BrutalDev\.NET Assembly Strong-Name Signer\StrongNameSigner.Console.exe" -in "..\packages"`

Dealing With Dependencies
-------------------------

To avoid a complicated explanation on how this works, just include ALL assemblies that reference each other whether they are signed or not. As of version 1.4.8, all included file paths are probed for references so they can be fixed without having to copy them into the signed assembly directory.

When dependant assemblies cannot be found and references weren't fixed correctly, the following type of error will occur during a build.

```
The type 'XYZ' is defined in an assembly that is not referenced. You must add a reference to assembly 'SomeAssembly, Version=1.2.34.5, Culture=neutral, PublicKeyToken=null'.
```

For example, ServiceStack's PostgreSQL NuGet package is not signed but other dependant assemblies are. Furthermore, these dependant assembly versions don't match what is referenced in `ServiceStack.OrmLite.PostgreSQL`. To correct the reference versions as well as ensuring the correct signed assemblies are referenced, simply include all the files that need to be processed in a single command to the strong-name signer.

```xml
<Target Name="BeforeBuild">
  <Exec ContinueOnError="false"
        Command="&quot;..\packages\Brutal.Dev.StrongNameSigner.1.5.1\tools\StrongNameSigner.Console.exe&quot; -in &quot;..\packages\ServiceStack.OrmLite.PostgreSQL.4.0.40\lib\net40|..\packages\ServiceStack.Text.Signed.4.0.40\lib\net40|..\packages\ServiceStack.OrmLite.Signed.4.0.40&quot;" />
</Target>
```

Even though `ServiceStack.OrmLite.PostgreSQL.dll` references the unsigned `ServiceStack.Text` v4.0.39 and the unsigned `ServiceStack.OrmLite.Signed` v4.0.40, using the command above will force it to use the included signed versions as references as well as correcting the reference versions to match.

API Usage
---------
Reference **Brutal.Dev.StrongNameSigner.dll** in your project or include it in a PowerShell script.

```csharp
var newInfo = Brutal.Dev.StrongNameSigner.SigningHelper.SignAssembly(@"C:\MyAssembly.dll");
```

Build
-----

To build the project you need to have the following third party software installed.
 - [Sandcastle Help File Builder](https://shfb.codeplex.com/) (to build API documentation in Release mode)
 - [Inno Setup](http://www.jrsoftware.org/isdl.php) (to compile the installer)

License
-------

Copyright (c) Werner van Deventer (werner@brutaldev.com).  All rights reserved.

Licensed under the Apache License, Version 2.0 (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied. See the License for the specific language governing permissions
and limitations under the License.
