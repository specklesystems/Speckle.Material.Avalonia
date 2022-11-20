using System;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Numerge;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild {
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    protected Target Clean => _ => _
        .Executes(() =>
        {
            Parameters.BuildDirs.ForEach(DeleteDirectory);
            Parameters.BuildDirs.ForEach(EnsureCleanDirectory);
            EnsureCleanDirectory(Parameters.ArtifactsDir);
            EnsureCleanDirectory(Parameters.NugetIntermediateRoot);
            EnsureCleanDirectory(Parameters.NugetRoot);
            EnsureCleanDirectory(Parameters.ZipRoot);
        });

    protected Target Compile => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetBuild(settings => ApplySetting(settings, buildSettings => 
                buildSettings.SetProjectFile(Parameters.MSBuildSolution)));
        });

    protected Target CreateIntermediateNugetPackages => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(settings => ApplySetting(settings, packSettings => 
                packSettings.SetProject(Parameters.MSBuildSolution)
                    .SetOutputDirectory(Parameters.NugetIntermediateRoot))
            );
        });

    protected Target CreateNugetPackage => _ => _
        .DependsOn(CreateIntermediateNugetPackages)
        .Produces(RootDirectory / "Material.Avalonia" / "bin" / "artifacts" / "nuget")
        .Executes(() =>
        {
            var config = MergeConfiguration.LoadFile(RootDirectory / "build" / "numerge.config");
            EnsureCleanDirectory(Parameters.NugetRoot);
            if (!NugetPackageMerger.Merge(Parameters.NugetIntermediateRoot, Parameters.NugetRoot,
                    config, new NumergeNukeLogger()))
                throw new Exception("Package merge failed");
        });

    protected Target ZipFiles => _ => _
        .After(CreateNugetPackage, Compile)
        .Executes(() =>
        {
            var data = Parameters;
            Zip(data.ZipNuGetArtifacts, data.NugetRoot);
        });
}