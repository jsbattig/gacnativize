# gacnativize
Simple utility that will GAC and NGEN assemblies matching a mask in a given folder.
This tool will keep track of failures on a subfolder named GACNativize.Log under the target folder.
The entries in log file contained in this folder will be used as an exception list. Files in this list
will not be processed.
If any file name is preprended with a - (dash) sign it will be completely skipped and not even reported
in the console.

Usage: GACNativize <parameters>

--main-command      ret`ry|g|gn|n
--source-path       Root folder where to look for assembly. Default = .\
--operation-mode    install|uninstall. Default = install
--file-mask         Specifies the mask to match assemblies in folder. Default = *.dll
--win-version       Windows SDK version. Default = v10.0A
--net-version       .NET target version for GAC operation. Default = 4.7.1
--framework-version .NET framework tooling version. Default = v4.0.30319

--main-command values reference:
retry  Retries failed operations logged in Exceptions.log file
g      Performs GAC install only
gn     Performs GAC and NGEN of matching assemblies
n      Performs NGEN install only
