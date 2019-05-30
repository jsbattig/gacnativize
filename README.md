# gacnativize
Simple utility that will GAC and NGEN assemblies matching a mask in a given folder.
This tool will keep track of failures on a subfolder named GACNativize.Log under the target folder.
The entries in log file contained in this folder will be used as an exception list. Files in this list
will not be processed.
If any file name is preprended with a - (dash) sign it will be completely skipped and not even reported
in the console.

Usage:

GACNativize -g|-gn|-n <folder> <filemask> install|uninstall [[<winversion>]|<winversion>[<.net version>]]
GACNativize -retry <folder> install|uninstall [[<winversion>]|<winversion>[<.net version>]]

-g Performs    GAC install only
-gn Performs   GAC and NGEN of matching assemblies
-n Performs    NGEN install only

<folder>       Root folder where to look for assembly files
<filemask>     The mask to match within the specified folder
<winversion>   A string specifying the Windows SDK version. Default: v10.0A
<.net version> .NET Framework version for NGEN tool. Default: 4.7.1
