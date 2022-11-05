using System;
using System.Diagnostics;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Numerge;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Xunit.XunitTasks;
using static Nuke.Common.Tools.VSWhere.VSWhereTasks;

partial class Build : NukeBuild {
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    Target Clean => _ => _
        .Executes(() =>
        {
            Parameters.BuildDirs.ForEach(DeleteDirectory);
            Parameters.BuildDirs.ForEach(EnsureCleanDirectory);
            EnsureCleanDirectory(Parameters.ArtifactsDir);
            EnsureCleanDirectory(Parameters.NugetIntermediateRoot);
            EnsureCleanDirectory(Parameters.NugetRoot);
            EnsureCleanDirectory(Parameters.ZipRoot);
        });

    Target Compile => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetBuild(settings => settings.SetProjectFile(Parameters.MSBuildSolution));
        });

    Target ZipFiles => _ => _
        .After(CreateNugetPackages, Compile)
        .Executes(() =>
        {
            var data = Parameters;
            Zip(data.ZipNuGetArtifacts, data.NugetRoot);
        });

    Target CreateIntermediateNugetPackages => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(settings => settings
                .SetProject(Parameters.MSBuildSolution)
                .SetOutputDirectory(Parameters.NugetIntermediateRoot)
            );
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(CreateIntermediateNugetPackages)
        .Executes(() =>
        {
            var config = MergeConfiguration.LoadFile(RootDirectory / "build" / "numerge.config");
            EnsureCleanDirectory(Parameters.NugetRoot);
            if (!NugetPackageMerger.Merge(Parameters.NugetIntermediateRoot, Parameters.NugetRoot, 
                    config, new NumergeNukeLogger()))
                throw new Exception("Package merge failed");
        });
}