.NET Assembly Strong-Name Signer
=================
[![Build status](https://ci.appveyor.com/api/projects/status/www2a5bfbrwn8piu)](https://ci.appveyor.com/project/brutaldev/strongnamesigner)

Utility software to strong-name sign .NET assemblies, including assemblies you do not have the source code for. If you strong-name sign your own projects you may have noticed that if you reference an unsigned third party assembly you get an error similar to "*Referenced assembly 'A.B.C' does not have a strong name*". If you did not create this assembly, you can use this tool to sign the assembly with your own (or temporarily generated) strong-name key. The tool will also re-write the assembly references (as well as any InternalsVisibleTo references) to match the new signed versions of the assemblies you create.

* [Download Installer](http://www.brutaldev.com/file.axd?file=StrongNameSigner_Setup.exe)
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

```
<Target Name="BeforeBuild">
  <Exec ContinueOnError="false"
        Command="&quot;C:\Program Files\BrutalDev\.NET Assembly Strong-Name Signer\StrongNameSigner.Console.exe&quot; -in &quot;..\packages&quot;" />
</Target>
```

Note that any files that are already strong-name signed will not be modified unless they reference a previously unsigned assembly. If you are using NuGet package restore then this works on build servers as well.

Another alternative is to simply call the `StrongNameSigner.Console.exe` with relevant argument as a pre-build step.

`"C:\Program Files\BrutalDev\.NET Assembly Strong-Name Signer\StrongNameSigner.Console.exe" -in "..\packages"`

API Usage
---------
Reference **Brutal.Dev.StrongNameSigner.dll** in your project or include it in a PowerShell script.

```
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
