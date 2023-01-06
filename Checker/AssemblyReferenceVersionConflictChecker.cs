using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SvSoft.MSBuild.MultiAssemblyPackageCheck;
public sealed class AssemblyReferenceVersionConflictChecker
{
    record TfmAssemblies(string Tfm, Assembly[] Assemblies);

    public IReadOnlyList<ConflictsForTfm> GetConflicts(byte[] packageContent, IEnumerable<string> ignoredAssemblyNames)
    {
        IReadOnlyList<TfmAssemblies> assembliesByTfm;
        using (ZipArchive packageZip = new(new MemoryStream(packageContent)))
        {
            assembliesByTfm = packageZip.Entries
                .Where(IsInLibFolder)
                .Where(IsDll)
                .GroupBy(GetTfm)
                .Select(g => new TfmAssemblies(g.Key, g.Select(ReflectionOnlyLoad).ToArray()))
                .ToArray();
        }

        IEnumerable<ConflictsForTfm> conflictsByTfm = from tfmAssemblies in assembliesByTfm
                                                      let conflicts = GetConflicts(tfmAssemblies.Assemblies)
                                                      where conflicts.Any()
                                                      select new ConflictsForTfm(tfmAssemblies.Tfm, conflicts);

        return conflictsByTfm.ToArray();

        IReadOnlyList<Conflict> GetConflicts(IEnumerable<Assembly> assemblies)
        {
            IEnumerable<AssemblyReference> assemblyReferences = assemblies
                .SelectMany(GetAssemblyReferences);

            ISet<string> namesWithMoreThanOneVersion = assemblyReferences
                .Select(r => r.ReferencedAssemblyName)
                .GroupBy(n => n.Name)
                .Select(group => (
                    Name: group.Key,
                    DistinctVersions: group.GroupBy(n => n.Version).Select(g => g.First()).ToArray()))
                .Where(nameAndDistinctVersions => nameAndDistinctVersions.DistinctVersions.Length > 1)
                .Select(nameAndDistinctVersions => nameAndDistinctVersions.Name)
                .ToImmutableHashSet();

            var conflictingAssemblyReferences = assemblyReferences
                .Where(r => namesWithMoreThanOneVersion.Contains(r.ReferencedAssemblyName.Name))
                .GroupBy(r => r.ReferencedAssemblyName.Name)
                .Select(g => new Conflict(g.Key, g.ToArray()))
                .ToArray();

            return conflictingAssemblyReferences;

            IEnumerable<AssemblyReference> GetAssemblyReferences(Assembly a)
            {
                return a.GetReferencedAssemblies()
                    .Where(a => !ignoredAssemblyNames.Any(n => n.Equals(a.Name)))
                    .Select(referencedAssembly => new AssemblyReference(
                        ReferencedAssemblyName: referencedAssembly,
                        ReferencingAssemblyName: a.GetName()));
            }
        }
    }

    static bool IsInLibFolder(ZipArchiveEntry entry) =>
        entry.FullName.StartsWith("lib", StringComparison.Ordinal);

    static bool IsDll(ZipArchiveEntry entry) =>
        entry.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

    // note that the zip format always uses '/' separator
    // see https://pkware.cachefly.net/webdocs/APPNOTE/APPNOTE-6.3.10.TXT section 4.4.17.1
    static string GetTfm(ZipArchiveEntry entry) =>
        entry.FullName.Split('/')[1];

    // The runtime assembly resolution is the same for each checker instance and immutable,
    // so we need to only set it up once.
    static readonly string[] RuntimeAssemblyPaths = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
    static readonly MetadataAssemblyResolver AssemblyResolver = new PathAssemblyResolver(RuntimeAssemblyPaths);

    // This checker could potentially run in parallel when the build checks multiple packages at once.
    // Each instance need its own LoadContext so that the parallel checks do not collide.
    readonly MetadataLoadContext LoadContext = new(AssemblyResolver);

    Assembly ReflectionOnlyLoad(ZipArchiveEntry dllEntry)
    {
        using var stream = dllEntry.Open();
        MemoryStream ms = new();
        stream.CopyTo(ms);
        var assembly = LoadContext.LoadFromByteArray(ms.ToArray());
        return assembly;
    }
}

public sealed record ConflictsForTfm(
    string Tfm,
    IReadOnlyList<Conflict> Conflicts);

public sealed record Conflict(
    string ReferencedAssemblyName,
    IReadOnlyList<AssemblyReference> References);

public sealed record AssemblyReference(
    AssemblyName ReferencedAssemblyName,
    AssemblyName ReferencingAssemblyName);
