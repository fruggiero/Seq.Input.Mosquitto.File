using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;

class Build : NukeBuild
{
    [NerdbankGitVersioning]
    readonly NerdbankGitVersioning NerdbankVersioning;

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    static AbsolutePath ArtifactsFolder => RootDirectory / "Artifacts";
    
    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            ArtifactsFolder.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore(_ => _
                .EnableNoCache()
            );
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetPublish(_ => _
                .SetConfiguration(Configuration.Release)
                .SetOutput(".obj/publish")
                .SetProperty("Version", NerdbankVersioning.NuGetPackageVersion)
            );

            DotNetTasks.DotNetPack(_ => _
                .SetConfiguration(Configuration.Release)
                .SetOutputDirectory(ArtifactsFolder)
                .EnableNoBuild()
                .SetProperty("Version", NerdbankVersioning.NuGetPackageVersion)
            );
        });
}
