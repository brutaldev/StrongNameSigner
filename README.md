.NET Assembly Strong-Name Signer
=================

Utility software to strong-name sign .NET assemblies, including assemblies you do not have the source code for. If you strong-name sign your own projects you may have noticed that if you reference an unsigned third party assembly you get an error similar to "*Referenced assembly 'A.B.C' does not have a strong name*". If you did not create this assembly, you can use this tool to sign the assembly with your own (or temporarily generated) strong-name key. The tool will also re-write the assembly references to match the new signed versions of the assemblies you create.

* [Download Installer](http://www.brutaldev.com/file.axd?file=StrongNameSigner_Setup.exe)
* [More Information](http://brutaldev.com/post/2013/10/18/NET-Assembly-Strong-Name-Signer)

Screenshots
-----------
![User Interface](http://brutaldev.com/image.axd?picture=StrongNameSigner_UI_1.png)

![Console](http://brutaldev.com/image.axd?picture=StrongNameSigner_Console_1.png)

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