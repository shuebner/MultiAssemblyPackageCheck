namespace SvSoft.MSBuild.MultiAssemblyPackageCheck;

public class SamplesBasedTests
{
    [Test]
    public void When_package_contains_conflicting_assemblies_Then_returns_conflicts()
    {
        AssemblyReferenceVersionConflictChecker checker = new();
        var packageContent = File.ReadAllBytes(
            Path.Combine(
                "Samples",
                "PackagedProject2.1.0.0.nupkg"));

        var conflicts = checker.GetConflicts(packageContent, Array.Empty<string>());

        ConflictsForTfm tfmConflict = conflicts.Should().ContainSingle().Subject;
        tfmConflict.Tfm.Should().Be("net6.0");
        Conflict conflict = tfmConflict.Conflicts.Should().ContainSingle().Subject;
        conflict.ReferencedAssemblyName.Should().Be("System.ComponentModel.Annotations");
        conflict.References.Should().HaveCount(2);
        conflict.References.Should().ContainSingle(r => r.ReferencingAssemblyName.Name!.Equals("PackagedProject1")).Which
            .ReferencedAssemblyName.Version.Should().BeEquivalentTo(new Version(4, 2, 0, 0));
        conflict.References.Should().ContainSingle(r => r.ReferencingAssemblyName.Name!.Equals("PackagedProject2")).Which
            .ReferencedAssemblyName.Version.Should().BeEquivalentTo(new Version(6, 0, 0, 0));
    }

    [Test]
    public void When_conflicting_assemblies_are_ignored_Then_returns_no_conflicts()
    {
        AssemblyReferenceVersionConflictChecker checker = new();
        var packageContent = File.ReadAllBytes(
            Path.Combine(
                "Samples",
                "PackagedProject2.1.0.0.nupkg"));

        var conflicts = checker.GetConflicts(packageContent, new[] { "System.ComponentModel.Annotations" });

        conflicts.Should().BeEmpty();
    }
}