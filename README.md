.NET Assembly Strong-Name Signer
=================

Utility software to strong-name sign .NET assemblies, including assemblies you do not have the source code for. If you strong-name sign your own projects you may have noticed that if you reference an unsigned third party assembly you get an error similar to "*Referenced assembly 'A.B.C' does not have a strong name*". If you did not create this assembly, you can use this tool to sign the assembly with your own (or temporarily generated) strong-name key. The tool will also re-write the assembly references (as well as any InternalsVisibleTo references) to match the new signed versions of the assemblies you create.

* [Download Installer](https://brutaldev.com/download/StrongNameSigner_Setup.exe)
* [NuGet Package](https://www.nuget.org/packages/Brutal.Dev.StrongNameSigner/)
* [More Information](https://brutaldev.com/post/2013/10/18/NET-Assembly-Strong-Name-Signer)

Screenshots
-----------
![User Interface](https://raw.github.com/brutaldev/StrongNameSigner/master/screenshots/StrongNameSigner_UI.png)

![Console](https://raw.github.com/brutaldev/StrongNameSigner/master/screenshots/StrongNameSigner_Console.png)

![Help](https://raw.github.com/brutaldev/StrongNameSigner/master/screenshots/StrongNameSigner_Help.png)

Build Process
-------------
By default all unsigned referenced assemblies in your project can automatically be signed just by installing the [NuGet package](https://www.nuget.org/packages/Brutal.Dev.StrongNameSigner/).
This will change the references to your assemblies to strong-name signed ones allowing you to sign your own projects and reference unsigned assemblies. All assemblies that are found (including signed ones) will have their references corrected if they were using any files that now have public key tokens.
In version 3.x, BAML resources are also updated with the correct references if assemblies are signed.

If you need to be more specific about what to sign you can call the console version in your Visual Studio project files to sign assemblies before it gets built.

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
        Command="&quot;..\packages\Brutal.Dev.StrongNameSigner.3.6.0\build\StrongNameSigner.Console.exe&quot; -in &quot;..\packages&quot;" />
</Target>
```

You can also provide specify a value for the `$(StrongNameSignerDirectory)` variable to avoid having to update your project files when a new version is released.
```xml
<Target Name="BeforeBuild">
  <Exec ContinueOnError="false"
        Command="&quot;$(StrongNameSignerDirectory)StrongNameSigner.Console.exe&quot; -in &quot;..\packages&quot;" />
</Target>
```

Often different packages have dependencies that are not signed, but those assemblies are in different directories. To correctly resolve references to dependent assemblies, all required assemblies and the dependencies they reference need to be processed at the same time.
Elmah is a good example of this. Additional Elmah libraries reference Elmah core, but do not include it in the package, they are installed separately. In order to fix the references to Elmah core, it needs to be able to cross check all signed files so you should sign all of them together.

To add multiple directories to process at the same time (similar to how the UI can process a number of assemblies at once in the grid) just pipe **|** delimit your input directory list.

```xml
<Target Name="BeforeBuild">
  <Exec ContinueOnError="false"
        Command="&quot;..\packages\Brutal.Dev.StrongNameSigner.3.6.0\build\StrongNameSigner.Console.exe&quot; -in &quot;..\packages\elmah.corelibrary.1.2.2|..\packages\Elmah.MVC.2.1.2&quot;" />
</Target>
```

This way the core library can be signed and the MVC library can have it's reference to the new signed version updated since they will be processed together and each file can be verified against each other after signing.
As a rule of thumb, always include all libraries that will be affected by any signing in a single call.

You can also use wildcards for each of your input directories. The above example could also be written using a wildcard that will match all directories and versions.

```xml
<Target Name="BeforeBuild">
  <Exec ContinueOnError="false"
        Command="&quot;..\packages\Brutal.Dev.StrongNameSigner.3.6.0\build\StrongNameSigner.Console.exe&quot; -in &quot;..\packages\elmah.*&quot;" />
</Target>
```

Wildcards can also be complex and placed anywhere in the path. This is useful if you only want a subset of directories as well as only certain framework specific lib directories to be signed.

```xml
<Target Name="BeforeBuild">
  <Exec ContinueOnError="false"
        Command="&quot;..\packages\Brutal.Dev.StrongNameSigner.3.6.0\build\StrongNameSigner.Console.exe&quot; -in &quot;..\packages\Microsoft.*.Security*\*\net45&quot;" />
</Target>
```

Note that any files that are already strong-name signed will not be modified unless they reference a previously unsigned assembly. If you are using NuGet package restore then this works on build servers as well.

Another alternative is to simply call the `StrongNameSigner.Console.exe` with relevant argument as a pre-build step.

`"C:\Program Files\BrutalDev\.NET Assembly Strong-Name Signer\StrongNameSigner.Console.exe" -in "..\packages"`

Dealing With Dependencies
-------------------------

To avoid a complicated explanation on how this works, just include ALL assemblies that reference each other whether they are signed or not. As of version 1.4.8, all included file paths are probed for references so they can be fixed without having to copy them into the signed assembly directory.

When dependent assemblies cannot be found and references weren't fixed correctly, the following type of error will occur during a build.

```
The type 'XYZ' is defined in an assembly that is not referenced. You must add a reference to assembly 'SomeAssembly, Version=1.2.34.5, Culture=neutral, PublicKeyToken=null'.
```

For example, ServiceStack's PostgreSQL NuGet package is not signed but other dependent assemblies are. Furthermore, these dependent assembly versions don't match what is referenced in `ServiceStack.OrmLite.PostgreSQL`. To correct the reference versions as well as ensuring the correct signed assemblies are referenced, simply include all the files that need to be processed in a single command to the strong-name signer.

```xml
<Target Name="BeforeBuild">
  <Exec ContinueOnError="false"
        Command="&quot;..\packages\Brutal.Dev.StrongNameSigner.3.6.0\build\StrongNameSigner.Console.exe&quot; -in &quot;..\packages\ServiceStack.OrmLite.PostgreSQL.4.0.40\lib\net40|..\packages\ServiceStack.Text.Signed.4.0.40\lib\net40|..\packages\ServiceStack.OrmLite.Signed.4.0.40&quot;" />
</Target>
```

Even though `ServiceStack.OrmLite.PostgreSQL.dll` references the unsigned `ServiceStack.Text` v4.0.39 and the unsigned `ServiceStack.OrmLite.Signed` v4.0.40, using the command above will force it to use the included signed versions as references as well as correcting the reference versions to match.

API Usage
---------
Reference **Brutal.Dev.StrongNameSigner.dll** in your project or include it in a PowerShell script.

```csharp
using var newInfo = Brutal.Dev.StrongNameSigner.SigningHelper.SignAssembly(@"C:\MyAssembly.dll");
```

Build
-----

To build the project you need to have the following third party software installed.
 - [Sandcastle Help File Builder](https://github.com/EWSoftware/SHFB/) (to build API documentation in Release mode)
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
