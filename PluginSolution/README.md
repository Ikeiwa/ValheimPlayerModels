# Plugin Solution

## Configuration

A handful of configuration properties have been defined to facilitate local dev:
- `GameDir`
    - Description:  The path to the root directory of your Valheim installation.
    - Default: none; the empty string
- `TargetInstallDir`
    - Description: The path to the directory _above_ the BepInEx installation, into which the built
      assembly will be copied for testing.
    - Default: The value of `GameDir`
- `PublicizeLocally`:
    - Description: Boolean `true` or `false`, indicating if we should perform publicization of the
      assemblies locally, as `Valheim.GameLibs` might be out-of-date. If `true`, requires `GameDir`
      to be set, in order to find the assemblies to publicize.
    - Default: `false`

### Example:

An example `Directory.Build.props`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <GameDir>C:\Program Files (x86)\Steam\steamapps\common\Valheim</GameDir>
    <!-- If using something like the "r2modman", with a profile called "Usual", might use something like: -->
    <!--
    <TargetInstallDir>$(USERPROFILE)\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\Usual</TargetInstallDir>
    -->
    <PublicizeLocally>true</PublicizeLocally>
  </PropertyGroup>
</Project>
```
