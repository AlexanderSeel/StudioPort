namespace StudioPort.Core.Models;

public sealed record StudioSnapshot
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; init; } = CurrentFormatVersion;
    public required Guid SnapshotId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required Guid SourceMachineId { get; init; }
    public IReadOnlyList<DawProduct> Daws { get; init; } = [];
    public IReadOnlyList<PluginProduct> Plugins { get; init; } = [];
    public IReadOnlyList<LibraryAsset> Libraries { get; init; } = [];
    public IReadOnlyList<PresetAsset> Presets { get; init; } = [];
    public IReadOnlyList<Prerequisite> Prerequisites { get; init; } = [];
    public IReadOnlyList<PathMapping> PathMappings { get; init; } = [];
}

public sealed record DawProduct(string Name, string Version, string? InstallPath = null);

public sealed record PluginProduct
{
    public required string Id { get; init; }
    public required string Vendor { get; init; }
    public required string Product { get; init; }
    public required string Version { get; init; }
    public IReadOnlyList<PluginModule> Modules { get; init; } = [];
    public IReadOnlyList<AssetReference> DataLocations { get; init; } = [];
    public IReadOnlyList<AssetReference> PresetLocations { get; init; } = [];
    public IReadOnlyList<Prerequisite> RuntimeRequirements { get; init; } = [];
    public LicenseRequirement? LicenseRequirement { get; init; }
    public PortabilityClass Portability { get; init; } = PortabilityClass.Unknown;
    public DiscoveryConfidence Confidence { get; init; } = DiscoveryConfidence.Unknown;
}

public sealed record PluginModule(
    string Format,
    string Architecture,
    LogicalPath Path,
    string Sha256,
    long Size);

public sealed record LibraryAsset(string Id, string Name, LogicalPath Path, long Size);
public sealed record PresetAsset(string Id, string Name, LogicalPath Path, long Size);
public sealed record PathMapping(string LogicalRoot, LogicalPath Target);
public sealed record Prerequisite(string Id, string Name, string? Version, PrerequisiteKind Kind);
public sealed record LicenseRequirement(string Provider, bool ActivationRequired, bool MachineBound, bool HardwareDongleSupported);
public sealed record AssetReference(LogicalPath Path, AssetScope Scope, DiscoveryConfidence Confidence);

public readonly record struct LogicalPath
{
    public LogicalPath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Replace('\\', '/').Trim('/');
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public enum AssetScope
{
    PortablePayload,
    UserState,
    MachineState,
    Prerequisite,
    LicenseState,
    Unknown
}

public enum PortabilityClass
{
    Portable,
    PortableWithData,
    RestoreRequired,
    InstallerRequired,
    ActivationRequired,
    Unknown
}

public enum DiscoveryConfidence
{
    Confirmed,
    Likely,
    Possible,
    Unknown
}

public enum PrerequisiteKind
{
    Runtime,
    Driver,
    Service,
    VendorManager,
    Daw,
    Other
}
