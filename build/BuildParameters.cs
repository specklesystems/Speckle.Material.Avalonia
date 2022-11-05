using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Nuke.Common;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;
using Serilog;
partial class Build {
    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    BuildParameters Parameters { get; set; }

    protected override void OnBuildInitialized() {
        Parameters = new BuildParameters(this);
        Log.Information("Building version {0} of Material.Avalonia ({1}) using version {2} of Nuke.",
            Parameters.Version,
            Parameters.Configuration,
            typeof(NukeBuild).Assembly.GetName().Version.ToString());

        if (Parameters.IsLocalBuild) {
            Log.Information("Repository Name: " + Parameters.RepositoryName);
            Log.Information("Repository Branch: " + Parameters.RepositoryBranch);
        }
        Log.Information("Configuration: " + Parameters.Configuration);
        Log.Information("IsLocalBuild: " + Parameters.IsLocalBuild);
        Log.Information("IsRunningOnUnix: " + Parameters.IsRunningOnUnix);
        Log.Information("IsRunningOnWindows: " + Parameters.IsRunningOnWindows);
        Log.Information("IsRunningOnAzure:" + Parameters.IsRunningOnAzure);
        Log.Information("IsPullRequest: " + Parameters.IsPullRequest);
        Log.Information("IsMainRepo: " + Parameters.IsMainRepo);
        Log.Information("IsMasterBranch: " + Parameters.IsMasterBranch);
        Log.Information("IsReleaseBranch: " + Parameters.IsReleaseBranch);
        Log.Information("IsReleasable: " + Parameters.IsReleasable);
        Log.Information("IsNuGetRelease: " + Parameters.IsNuGetRelease);

        void ExecWait(string preamble, string command, string args) {
            Console.WriteLine(preamble);
            Process.Start(new ProcessStartInfo(command, args) { UseShellExecute = false }).WaitForExit();
        }

        ExecWait("dotnet version:", "dotnet", "--info");
        ExecWait("dotnet workloads:", "dotnet", "workload list");
        Log.Information("Processor count: " + Environment.ProcessorCount);
        Log.Information("Available RAM: " + GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 0x100000 + "MB");
    }

    public class BuildParameters {
        public string Configuration { get; }
        public string MainRepo { get; }
        public string MasterBranch { get; }
        public string RepositoryName { get; }
        public string RepositoryBranch { get; }
        public string ReleaseConfiguration { get; }
        public string ReleaseBranchPrefix { get; }
        public string MSBuildSolution { get; }
        public bool IsLocalBuild { get; }
        public bool IsRunningOnUnix { get; }
        public bool IsRunningOnWindows { get; }
        public bool IsRunningOnAzure { get; }
        public bool IsPullRequest { get; }
        public bool IsMainRepo { get; }
        public bool IsMasterBranch { get; }
        public bool IsReleaseBranch { get; }
        public bool IsReleasable { get; }
        public bool IsNuGetRelease { get; }
        public bool PublishTestResults { get; }
        public string Version { get; }
        public AbsolutePath ArtifactsDir { get; }
        public AbsolutePath NugetIntermediateRoot { get; }
        public AbsolutePath NugetRoot { get; }
        public AbsolutePath ZipRoot { get; }
        public string DirSuffix { get; }
        public List<AbsolutePath> BuildDirs { get; }
        public string FileZipSuffix { get; }
        public AbsolutePath ZipNuGetArtifacts { get; }


        public BuildParameters(Build b) {
            // ARGUMENTS
            Configuration = b.Configuration ?? "Release";

            // CONFIGURATION
            MainRepo = "https://github.com/AvaloniaCommunity/Material.Avalonia";
            MasterBranch = "refs/heads/master";
            ReleaseBranchPrefix = "refs/heads/release/";
            ReleaseConfiguration = "Release";
            MSBuildSolution = RootDirectory / "Material.Avalonia.sln";

            // PARAMETERS
            IsLocalBuild = NukeBuild.IsLocalBuild;
            IsRunningOnUnix = Environment.OSVersion.Platform == PlatformID.Unix ||
                              Environment.OSVersion.Platform == PlatformID.MacOSX;
            IsRunningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsRunningOnAzure = Host is AzurePipelines ||
                               Environment.GetEnvironmentVariable("LOGNAME") == "vsts";

            if (IsRunningOnAzure) {
                RepositoryName = AzurePipelines.Instance.RepositoryUri;
                RepositoryBranch = AzurePipelines.Instance.SourceBranch;
                IsPullRequest = AzurePipelines.Instance.PullRequestId.HasValue;
                IsMainRepo = StringComparer.OrdinalIgnoreCase.Equals(MainRepo, AzurePipelines.Instance.RepositoryUri);
            }
            IsMainRepo =
                StringComparer.OrdinalIgnoreCase.Equals(MainRepo,
                    RepositoryName);
            IsMasterBranch = StringComparer.OrdinalIgnoreCase.Equals(MasterBranch,
                RepositoryBranch);
            IsReleaseBranch = RepositoryBranch?.StartsWith(ReleaseBranchPrefix, StringComparison.OrdinalIgnoreCase) ==
                              true;

            IsReleasable = StringComparer.OrdinalIgnoreCase.Equals(ReleaseConfiguration, Configuration);
            IsNuGetRelease = IsMainRepo && IsReleasable && IsReleaseBranch;

            // VERSION
            // Version = b.ForceNugetVersion ?? GetVersion();

            if (IsRunningOnAzure) {
                if (!IsNuGetRelease) {
                    // Use AssemblyVersion with Build as version
                    Version += "-cibuild" + int.Parse(Environment.GetEnvironmentVariable("BUILD_BUILDID")).ToString("0000000") + "-beta";
                }

                PublishTestResults = true;
            }

            // DIRECTORIES
            ArtifactsDir = RootDirectory / "Material.Avalonia" / "bin" / "artifacts";
            NugetRoot = ArtifactsDir / "nuget";
            NugetIntermediateRoot = RootDirectory / "Material.Avalonia" / "bin" / "build-intermediate" / "nuget";
            ZipRoot = ArtifactsDir / "zip";
            BuildDirs = RootDirectory.GlobDirectories(RootDirectory, "**bin").Concat(RootDirectory.GlobDirectories("**obj")).ToList();
            DirSuffix = Configuration;
            FileZipSuffix = Version + ".zip";
            ZipNuGetArtifacts = ZipRoot / ("Material.Avalonia-NuGet-" + FileZipSuffix);
        }
    }
}