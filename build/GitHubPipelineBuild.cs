using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Xunit.XunitTasks;
using static Nuke.Common.Tools.VSWhere.VSWhereTasks;

/// <summary>
/// Representing targets for github pipeline for main repository
/// </summary>
[GitHubActions("main", GitHubActionsImage.UbuntuLatest, AutoGenerate = true, Submodules = GitHubActionsSubmodules.Recursive,
    InvokedTargets = new[] { nameof(Compile), nameof(PublishNugetPackage) },
    ImportSecrets = new[] { nameof(NuGetKey) }, EnableGitHubToken = true,
    On = new[] {
        GitHubActionsTrigger.Push,
        GitHubActionsTrigger.PullRequest
    })]
partial class Build {
    [Parameter] [Secret] readonly string NuGetKey;

    /// <summary>
    /// Publishing artifacts for main Material.Avalonia repo
    /// </summary>
    Target PublishNugetPackage => _ => _
        .Unlisted()
        .DependsOn(CreateNugetPackage)
        .OnlyWhenStatic(() => IsServerBuild)
        .OnlyWhenDynamic(
            () => Parameters.IsMainRepo,
            () => Parameters.IsReleasable)
        .Executes(() =>
        {
            GitHubActions.Instance.Token.NotNullOrEmpty("Github token should be not null or empty to publish packages");
            DotNetNuGetPush(settings => settings
                .SetSource("https://nuget.pkg.github.com/AvaloniaCommunity/index.json")
                .SetApiKey(GitHubActions.Instance.Token)
                .EnableSkipDuplicate());

            NuGetKey.NotNullOrEmpty("NuGet api key should be not null or empty to publish packages");
            DotNetNuGetPush(settings => settings
                .SetSource("https://api.nuget.org/v3/index.json")
                .SetApiKey(NuGetKey)
                .EnableSkipDuplicate());
        });
}