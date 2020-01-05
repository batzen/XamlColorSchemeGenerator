# XamlColorSchemeGenerator

[![Build status](https://img.shields.io/appveyor/ci/batzen/XamlColorSchemeGenerator.svg?style=flat-square)](https://ci.appveyor.com/project/batzen/XamlColorSchemeGenerator)
[![Release](https://img.shields.io/github/release/batzen/XamlColorSchemeGenerator.svg?style=flat-square)](https://github.com/batzen/XamlColorSchemeGenerator/releases/latest)
[![Issues](https://img.shields.io/github/issues/batzen/XamlColorSchemeGenerator.svg?style=flat-square)](https://github.com/batzen/XamlColorSchemeGenerator/issues)
[![Downloads](https://img.shields.io/nuget/dt/XamlColorSchemeGenerator.svg?style=flat-square)](http://www.nuget.org/packages/XamlColorSchemeGenerator/)
[![Nuget](https://img.shields.io/nuget/vpre/XamlColorSchemeGenerator.svg?style=flat-square)](http://nuget.org/packages/XamlColorSchemeGenerator)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://github.com/batzen/XamlColorSchemeGenerator/blob/master/License.txt)

Generates color scheme xaml files while replacing certain parts of a template file.

For an example on how this tool works see the [generator input](src/GeneratorParameters.json) and [template](src/Theme.Template.xaml) files.

## Using the tool

### Usage with commandline parameters

`XamlColorSchemeGenerator` accepts the following commandline parameters:

- `-g "Path_To_Your_GeneratorParameters.json"`
- `-g "Path_To_Your_Theme.Template.xaml"`
- `-o "Path_To_Your_Output_Folder"`
- `-v` = enables verbose console output

### Usage without commandline parameters

Just set the working directory to a directory containing `GeneratorParameters.json` and `Theme.Template.xaml` and call `XamlColorSchemeGenerator.exe`.
The tool then also uses the current working dir as the output folder.

### Usage during build

```xml
    <ItemGroup>
      <PackageReference Include="XamlColorSchemeGenerator" version="3.*" privateAssets="All" />
    </ItemGroup>

    <Target Name="GenerateXamlFilesInner">
      <PropertyGroup>
        <XamlColorSchemeGeneratorVersion Condition="'%(PackageReference.Identity)' == 'XamlColorSchemeGenerator'">%(PackageReference.Version)</XamlColorSchemeGeneratorVersion>
        <XamlColorSchemeGeneratorPath>$(NuGetPackageRoot)/xamlcolorschemegenerator/$(XamlColorSchemeGeneratorVersion)/tools/XamlColorSchemeGenerator.exe</XamlColorSchemeGeneratorPath>
      </PropertyGroup>
      <!-- Generate theme files -->
      <Exec Command="&quot;$(XamlColorSchemeGeneratorPath)&quot;" WorkingDirectory="$(MSBuildProjectDirectory)/Themes/Themes" />
    </Target>

    <!-- Key to generating the xaml files at the right point in time is to do this before DispatchToInnerBuilds -->
    <Target Name="GenerateXamlFiles" BeforeTargets="DispatchToInnerBuilds;BeforeBuild">
      <!--TargetFramework=once is critical here, as it will not execute task from same project with same properties multiple times. 
        We need to unify TargetFramework between empty, net45, netcoreapp3.0 etc.-->
      <MSBuild Projects="$(MSBuildProjectFile)" Targets="GenerateXamlFilesInner" Properties="TargetFramework=once" />
    </Target>
```
