using System;
using Nuke.Common;
using Nuke.Common.IO;

partial class Build : NukeBuild
{
    const string Debug = "Debug";
    const string Release = "Release";

    int DegreeOfParallelism =
        System.Environment.ProcessorCount < 5
            ? System.Environment.ProcessorCount * 2
            : 10;

    AbsolutePath SourceDirectory => RootDirectory / "src";

    AbsolutePath AllSolutionFile => SourceDirectory / "All.sln";

    AbsolutePath SgSolutionFile => SourceDirectory / "StrawberryShake" / "SourceGenerator" / "StrawberryShake.SourceGenerator.sln";

    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath TestResultDirectory => OutputDirectory / "test-results";
    AbsolutePath CoverageReportDirectory => OutputDirectory / "coberage-reports";
    AbsolutePath PackageDirectory => OutputDirectory / "packages";

    string ChangelogFile => RootDirectory / "CHANGELOG.md";

    AbsolutePath StrawberryShakeNuSpec => SourceDirectory / "StrawberryShake" / "CodeGeneration" / "src" / "MSBuild" / "StrawberryShake.nuspec";
    AbsolutePath StarWarsTemplateNuSpec => RootDirectory / "templates" / "StarWars" / "HotChocolate.Templates.StarWars.nuspec";

    AbsolutePath StarWarsProj => RootDirectory / "templates" / "StarWars" / "content" / "StarWars" / "StarWars.csproj";
    AbsolutePath EmptyServerTemplateNuSpec => RootDirectory / "templates" / "Server" / "HotChocolate.Templates.Server.nuspec";

    AbsolutePath EmptyServerProj => RootDirectory / "templates" / "Server" / "content" / "HotChocolate.Server.Template.csproj";
}
