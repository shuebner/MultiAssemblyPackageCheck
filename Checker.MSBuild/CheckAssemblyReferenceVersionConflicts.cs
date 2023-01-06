using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SvSoft.MSBuild.MultiAssemblyPackageCheck;
public sealed class CheckAssemblyReferenceVersionConflicts : Task
{
    const string SubCategory = "PackageCheck";
    const string WarningCode = "PCK001";

    [Required]
    public string PackagePath { get; set; }

    public ITaskItem[] IgnoredAssemblyNames { get; set; }

    public override bool Execute()
    {
        if (!File.Exists(PackagePath))
        {
            Log.LogError($"unable to find package file at {PackagePath}");
            return false;
        }

        byte[] packageContent;
        try
        {
            packageContent = File.ReadAllBytes(PackagePath);
        }
        catch (IOException e)
        {
            Log.LogError($"unable to read package file at {PackagePath}: {e.Message}");
            Log.LogErrorFromException(e);
            return false;
        }

        string[] ignoredAssemblyNames = IgnoredAssemblyNames is not null
            ? Array.ConvertAll(IgnoredAssemblyNames, taskItem => taskItem.ItemSpec)
            : Array.Empty<string>();

        var checker = new AssemblyReferenceVersionConflictChecker();

        IReadOnlyList<ConflictsForTfm> conflicts = checker.GetConflicts(packageContent, ignoredAssemblyNames);

        if (conflicts.Count > 0)
        {
            foreach (var conflict in conflicts)
            {
                Log.LogWarning(
                    subcategory: SubCategory,
                    warningCode: WarningCode,
                    null,
                    PackagePath,
                    0, 0, 0, 0,
                    $"Package {PackagePath} within TFM {conflict.Tfm} contains assemblies with inconsistent assembly dependency versions. This can lead to runtime assembly loading errors.\n{ToString(conflict)}");
            }
        }

        return true;
    }

    static string ToString(ConflictsForTfm conflictsForTfm) =>
        string.Join(Environment.NewLine, conflictsForTfm.Conflicts.Select(ToString));

    static string ToString(Conflict conflict) =>
        $"{conflict.ReferencedAssemblyName}: {ToString(conflict.References)}";

    static string ToString(IReadOnlyList<AssemblyReference> assemblyReferences) =>
        string.Join(
            ", ",
            assemblyReferences
                .GroupBy(r => r.ReferencedAssemblyName.Version)
                .Select(g => $"{g.Key} <- [{string.Join(", ", g.Select(r => r.ReferencingAssemblyName.Name))}]"));
}
