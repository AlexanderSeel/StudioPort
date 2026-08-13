# StudioPort — Implementation Plan

## Mission

StudioPort reproduces a Windows music-production environment across multiple computers from a portable external drive.

The target workflow is:

1. Scan a known-good studio workstation.
2. Build a versioned StudioPort snapshot on an external HDD/SSD.
3. Connect the drive to another Windows PC or laptop.
4. Install the DAW and unavoidable machine-level prerequisites normally.
5. Run StudioPort to compare, restore, link and verify the remaining environment.
6. Clearly report anything requiring a vendor installer, driver, login, activation or hardware device.

StudioPort must never bypass DRM, licensing, activation, hardware identifiers, copy protection, vendor services or driver installation requirements.

The product goal is **reproducibility**, not an unsupported "portable Cubase" hack.

---

## Architectural principles

### 1. A plug-in is not just a `.vst3` file

Every product may consist of:

- plug-in module/bundle
- presets and user presets
- samples/content/libraries
- ProgramData/AppData/Documents data
- registry state
- runtimes
- vendor managers/services
- installer packages
- activation requirements
- DAW-specific state

The data model and restore engine must preserve this distinction.

### 2. Separate portable payload from machine state

Every discovered asset is classified into one of these scopes:

- `PortablePayload` — safe to archive and restore/link.
- `UserState` — user-specific presets/configuration that may be restored.
- `MachineState` — machine-specific configuration; compare or regenerate, do not blindly copy.
- `Prerequisite` — driver/runtime/service/vendor manager that needs installation.
- `LicenseState` — activation requirement only; never clone protected state.
- `Unknown` — insufficient confidence; require review.

### 3. Restore is planned before it is executed

Every restore begins as a dry-run plan. No mutation happens until the plan is valid and conflicts are surfaced.

### 4. Machine differences are first-class

Desktop and laptop can legitimately differ in:

- audio interface / ASIO driver
- MIDI devices and device IDs
- CPU/GPU capabilities
- Windows architecture/version
- available drive space
- external storage filesystem
- developer/symlink permissions
- installed vendor managers
- activation availability

StudioPort therefore stores a `MachineProfile` separately from the portable studio snapshot.

### 5. Vendor-specific knowledge is isolated

Generic scanning/restoring lives in Core. Vendor-specific behavior uses adapters, for example:

- Steinberg / Cubase / Steinberg Library Manager
- iLok / PACE
- Native Instruments / Native Access
- Arturia Software Center
- Waves Central
- Spitfire Audio
- UAD

Adapters may detect requirements and invoke documented vendor tools, but must not manipulate protected license stores.

### 6. Paths are logical, never bound to `E:` or another drive letter

Snapshot manifests use logical roots and relative paths. Runtime resolution uses a StudioPort volume ID plus Windows volume information.

### 7. Verification is part of restore

A restore is incomplete until files, hashes, links, library registrations and prerequisites have been checked.

---

# Product model

## StudioSnapshot

A snapshot represents the portable/reproducible studio state at a point in time.

```text
StudioSnapshot
  FormatVersion
  SnapshotId
  CreatedAt
  SourceMachineId
  Daws[]
  Plugins[]
  Libraries[]
  Presets[]
  Installers[]
  Prerequisites[]
  PathMappings[]
  VendorRequirements[]
  Integrity
```

## MachineProfile

```text
MachineProfile
  MachineId
  MachineName
  Windows
  Architecture
  Cpu
  Memory
  Volumes[]
  AudioDevices[]
  AsioDrivers[]
  MidiDevices[]
  InstalledRuntimes[]
  VendorManagers[]
  Capabilities[]
```

Machine profiles are compared against snapshot requirements; they are not copied wholesale between machines.

## PluginProduct

```text
PluginProduct
  Id
  Vendor
  Product
  Version
  Formats[]
  Modules[]
  DataLocations[]
  PresetLocations[]
  LibraryReferences[]
  RegistryReferences[]
  InstallerReferences[]
  RuntimeRequirements[]
  LicenseRequirement
  Portability
  DiscoveryEvidence[]
```

## Asset classification

Every asset receives:

```text
Scope
Confidence
SourcePath
LogicalTarget
RestoreStrategy
Hash
Size
OwnershipEvidence[]
```

Confidence values:

- `Confirmed`
- `Likely`
- `Possible`
- `Unknown`

Weak correlations must never silently become restore actions.

---

# Storage layout

Recommended external drive layout:

```text
StudioPort/
  .studioport/
    volume.json
    format.json
  snapshots/
    <snapshot-id>/
      snapshot.json
      machine.json
      report.json
  objects/
    sha256/
      ab/
        <hash>
  packages/
    plugins/
    presets/
    libraries/
    installers/
  vendor/
    steinberg/
    native-instruments/
    arturia/
    waves/
    other/
  reports/
  logs/
```

Large content can be referenced instead of duplicated when it already resides on the StudioPort drive.

The drive should have a unique StudioPort ID. The drive letter is only a transient mount point.

NTFS is the preferred filesystem for the StudioPort drive on Windows because it gives the fewest constraints for links, metadata and future deduplication strategies. Other filesystems may be supported with reduced capabilities.

---

# Restore strategies

Each asset chooses an explicit strategy.

## Mirror

Copy to the normal machine-local destination.

Best default for small VST3 binaries and local configuration that vendors expect at standard paths.

## Link

Keep the payload on StudioPort and create a supported filesystem link from a normal location.

Use only where the host/vendor behavior is known to tolerate it.

## ExternalReference

The application/library remains directly on the StudioPort drive and is registered/configured through its supported mechanism.

Ideal for large sample/content libraries.

## Installer

Execute or direct the user to the appropriate installer/vendor manager. Never fake installer side effects.

## Manual

Report an action that StudioPort cannot safely automate.

---

# Phase 0 — Repository foundation

- [ ] Create .NET 10 solution.
- [ ] Add `StudioPort.Core`.
- [ ] Add `StudioPort.Windows`.
- [ ] Add `StudioPort.Scanner`.
- [ ] Add `StudioPort.Storage`.
- [ ] Add `StudioPort.Restore`.
- [ ] Add `StudioPort.Cli`.
- [ ] Add test projects.
- [ ] Enable nullable reference types.
- [ ] Enable deterministic builds.
- [ ] Add analyzers and warnings policy.
- [ ] Add structured logging abstraction.
- [ ] Add central JSON serialization settings.
- [ ] Define snapshot schema versioning from day one.
- [ ] Add architecture and safety documentation.

Acceptance:

- solution builds on Windows with .NET 10
- empty `StudioSnapshot` round-trips through JSON
- manifest schema version is validated when loading

---

# Phase 1 — StudioPort volume and logical path system

This must be implemented before snapshots because physical drive letters are unstable.

- [ ] Define `.studioport/volume.json`.
- [ ] Generate persistent StudioPort volume ID.
- [ ] Detect currently mounted StudioPort volumes.
- [ ] Capture Windows volume GUID where available.
- [ ] Define logical URI/path format such as `studioport://packages/...`.
- [ ] Resolve logical paths to current mount paths.
- [ ] Reject path traversal outside logical roots.
- [ ] Handle drive-letter changes.
- [ ] Detect missing/offline volumes.
- [ ] Detect filesystem capabilities.
- [ ] Detect free-space constraints.

Acceptance:

A snapshot created while the drive is `E:` remains usable if Windows later mounts it as `H:`.

---

# Phase 2 — Machine capability scanner

- [ ] Windows version/build.
- [ ] x64/ARM64 architecture.
- [ ] CPU information.
- [ ] available memory.
- [ ] volume/filesystem information.
- [ ] developer/symlink capability.
- [ ] installed Visual C++/.NET runtimes where discoverable.
- [ ] installed application inventory from safe registry sources.
- [ ] installed vendor managers.
- [ ] detected audio endpoints.
- [ ] detected ASIO drivers via supported Windows/registry discovery.
- [ ] detected MIDI devices where practical.

Do not use APIs with side effects merely to enumerate installed software.

Acceptance:

`studioport machine scan` emits a stable machine profile without changing machine state.

---

# Phase 3 — VST3 discovery

Scan standardized Windows VST3 locations and explicitly configured additional roots.

Expected standard roots include:

- user common VST3 location
- `%ProgramFiles%\Common Files\VST3`
- applicable x86 common VST3 location
- DAW/application-local VST3 roots where discoverable

Requirements:

- [ ] recurse safely
- [ ] support single-file legacy modules
- [ ] support modern directory/bundle form
- [ ] identify symlinks/reparse points
- [ ] prevent recursive link loops
- [ ] preserve package boundary
- [ ] SHA-256 hash files
- [ ] calculate deterministic package hash
- [ ] capture Windows file version information
- [ ] capture PE architecture
- [ ] inspect digital signature/publisher
- [ ] inspect `moduleinfo.json` when present
- [ ] avoid loading untrusted plug-in DLLs inside the main StudioPort process

### Critical security rule

Metadata discovery must not instantiate arbitrary VST plug-ins inside the main CLI/UI process. If VST runtime interrogation is added later, perform it in a separate disposable scanner process with timeout/crash isolation.

Acceptance:

`studioport plugins scan` produces a deterministic inventory and cannot be crashed permanently by a malformed plug-in package.

---

# Phase 4 — Product correlation engine

A file is not yet a product. Correlate plug-in modules with installed products and data.

Evidence sources:

- exact vendor/product metadata
- digital-signature publisher
- file version product/company names
- directory ownership
- Windows uninstall registry records
- MSI product information where safely available
- installer filename metadata
- known vendor conventions
- optional user confirmation

Scoring must produce `Confirmed`, `Likely`, `Possible` or `Unknown`.

- [ ] persist evidence, not just the final score
- [ ] allow user override
- [ ] remember overrides in a local rules file
- [ ] never auto-copy broad vendor folders based only on vendor-name similarity

---

# Phase 5 — Related data/preset discovery

Search candidate roots including:

- `%APPDATA%`
- `%LOCALAPPDATA%`
- `%PROGRAMDATA%`
- Documents
- Public Documents
- Program Files/Common Files
- vendor-declared locations

Classify discovered files into:

- factory data
- user presets
- shared presets
- samples
- impulse responses
- configuration
- cache/temp
- unknown

Caches, logs, crash dumps and other reproducible transient data should normally be excluded.

Acceptance:

A scan report explains *why* StudioPort believes a data folder belongs to a plug-in and whether it will be included.

---

# Phase 6 — Snapshot engine

Command target:

```text
studioport snapshot create <target>
```

- [ ] scan machine/plugins
- [ ] resolve selected assets
- [ ] calculate sizes before copying
- [ ] estimate required free space
- [ ] content-addressed object storage by SHA-256
- [ ] avoid duplicate payload storage
- [ ] write snapshot into staging area first
- [ ] verify hashes after copy
- [ ] atomically publish completed snapshot manifest
- [ ] never advertise an incomplete snapshot as valid
- [ ] support cancellation and resume
- [ ] produce human-readable report

### Snapshot consistency

The manifest must distinguish between:

- payload copied into object store
- files referenced in-place on StudioPort
- target-machine prerequisites
- files deliberately excluded
- unresolved/unknown assets

---

# Phase 7 — Compare/readiness engine

Commands:

```text
studioport compare <snapshot>
studioport ready <snapshot>
```

Compare:

- plug-in identity/version/hash
- presets
- libraries/content
- DAW version
- required runtimes
- vendor managers
- activation requirements
- audio/MIDI prerequisites
- available storage
- broken links

Statuses:

- `Ready`
- `ReadyWithWarnings`
- `Missing`
- `VersionMismatch`
- `ActivationRequired`
- `InstallerRequired`
- `DriverRequired`
- `Conflict`
- `Unknown`

This engine should become the core UX: before touching a second machine, StudioPort can tell the user exactly what will and will not work.

---

# Phase 8 — Restore planner and transaction engine

Commands:

```text
studioport restore <snapshot> --dry-run
studioport restore <snapshot>
```

Every restore operation is represented before execution:

```text
RestoreOperation
  Id
  Type
  Source
  Target
  RequiresElevation
  Precondition
  ConflictPolicy
  RollbackData
```

Operation types:

- CopyFile/CopyTree
- CreateLink
- RegisterLibrary
- WriteAllowedRegistryValue
- RunInstaller
- RunVendorAdapter
- ManualAction

Requirements:

- [ ] dry run is the default planning primitive
- [ ] deterministic operation ordering
- [ ] explicit elevation boundaries
- [ ] backup overwritten user data
- [ ] transaction journal
- [ ] rollback where technically safe
- [ ] no permanent partial-success state without a report
- [ ] idempotent re-run where possible

---

# Phase 9 — Safe filesystem link support

VST3 supports standard locations and hosts may follow links from those locations; link creation on Windows nevertheless depends on privileges/system policy.

- [ ] detect symlink capability
- [ ] support file/directory symbolic links where appropriate
- [ ] evaluate junction support separately
- [ ] never assume link creation is available
- [ ] verify target after creating link
- [ ] detect broken or circular links
- [ ] provide Mirror fallback

Do not make Link mode the universal default. Hybrid/Mirror should be safer for most plug-in binaries.

---

# Phase 10 — Installer repository and provenance

Store installers as first-class immutable artifacts.

```text
InstallerArtifact
  Id
  Vendor
  Product
  Version
  FileName
  Sha256
  SignaturePublisher
  Source
  AddedAt
  SilentInstallSupport
  Arguments
```

- [ ] manually add installer
- [ ] associate installer with product
- [ ] verify signature/hash before execution
- [ ] never silently replace an installer with another version
- [ ] record install result
- [ ] support vendor-manager requirement instead of direct installer when appropriate

Installer provenance is essential for recreating old projects years later.

---

# Phase 11 — Install Capture mode

Generic retrospective discovery is inherently uncertain. Capture mode creates high-confidence package knowledge.

```text
studioport capture install
```

Workflow:

1. Capture relevant pre-install state.
2. User installs the product normally.
3. Capture post-install state.
4. Diff changes.
5. Correlate changes to the newly installed product.
6. Let user review uncertain changes.
7. Save reusable product recipe.

Capture categories:

- files/directories
- selected registry areas
- uninstall inventory
- services/drivers added (report, do not replicate manually)
- environment variables
- vendor-manager registrations

### Capture safety

Do not snapshot the whole registry or copy secrets. Maintain an exclusion policy for credentials, browser data, license tokens and unrelated user data.

---

# Phase 12 — License and activation awareness

Represent requirements without cloning protected state.

```text
LicenseRequirement
  Provider
  ActivationRequired
  HardwareDongleSupported
  VendorManagerRequired
  MachineBound
  Notes
```

Potential providers/adapters include Steinberg Activation Manager, PACE/iLok and vendor-specific managers.

Never:

- copy license tokens/databases
- reproduce hardware IDs
- patch binaries
- bypass activation
- export credentials

Output should state the exact action still required by the user.

---

# Phase 13 — Cubase adapter

Cubase itself is normally installed on each machine. StudioPort manages reproducible user/studio state around it.

Discover installed Cubase versions dynamically.

Candidate portable/user state:

- project templates
- track presets
- plug-in presets
- key commands
- logical editor presets
- scripts
- user-defined content
- selected MediaBay-related user data when safe

Candidate machine-specific state that must not be blindly cloned:

- ASIO device selection
- audio-port/device IDs
- MIDI hardware IDs
- monitor/display geometry
- caches
- crash state
- hardware-specific settings

Implement explicit per-setting classification rather than copying the entire Cubase preferences directory.

---

# Phase 14 — Steinberg content adapter

- [ ] detect Steinberg Library Manager
- [ ] discover known VST Sound content locations
- [ ] model library containers separately from plug-ins
- [ ] prefer supported Steinberg registration/relocation mechanisms
- [ ] verify registration after restore

Large Steinberg content should normally remain on the external StudioPort drive.

---

# Phase 15 — Other vendor adapters

Prioritize based on installed products found on the source workstation.

Each adapter can implement:

```text
IVendorAdapter
  DetectProducts
  DiscoverData
  DiscoverLibraries
  DiscoverPrerequisites
  BuildRestoreActions
  Verify
```

Adapters must be optional and isolated so failure in one vendor implementation cannot block generic scanning.

---

# Phase 16 — Audio and MIDI environment mapping

This is essential for a usable DAW clone but cannot be treated like normal files.

- [ ] inventory ASIO drivers
- [ ] inventory audio endpoints
- [ ] inventory MIDI input/output endpoints
- [ ] record source-machine logical roles, e.g. `MainAudioInterface`, `MasterKeyboard`
- [ ] allow target-machine mapping to different physical devices
- [ ] detect missing driver/device
- [ ] keep device mapping outside portable plug-in payload

Example:

```text
Source role: MainAudioInterface
Desktop: RME Fireface USB
Laptop: Steinberg UR22C
```

StudioPort should allow different hardware while preserving the logical studio role.

---

# Phase 17 — Absolute path repair and library relocation

Projects and plug-ins often retain absolute content paths.

- [ ] maintain `PathMapping` rules
- [ ] detect references to old drive letters where safely inspectable
- [ ] prefer supported vendor relocation APIs/tools
- [ ] provide mapping suggestions rather than blind binary search/replace
- [ ] support stable optional mount point/drive-letter assignment as a compatibility strategy

Never binary-patch proprietary project/database files without a documented safe format.

---

# Phase 18 — Project readiness

Future command:

```text
studioport project ready <project>
```

Goal: answer "Can this project open correctly on this machine?"

Where safe/documented, determine or let the user associate:

- required plug-ins
- expected versions
- required sample libraries/content
- DAW version
- external files

If `.cpr` inspection is not safely/documentedly possible, support explicit project dependency manifests generated from the running studio environment instead of reverse-engineering proprietary formats.

---

# Phase 19 — Backup, history and rollback

Snapshots should be immutable.

- [ ] retain multiple snapshots
- [ ] incremental/deduplicated storage
- [ ] compare snapshots over time
- [ ] garbage collection only for objects unreferenced by every retained snapshot
- [ ] never delete unknown user files automatically
- [ ] restore journal
- [ ] rollback backups for replaced configuration

A studio snapshot is valuable disaster-recovery data; destructive cleanup must be conservative.

---

# Phase 20 — Security model

StudioPort handles executable plug-ins and installers. Treat the external drive as potentially untrusted input.

- [ ] hash every payload
- [ ] validate manifest/schema
- [ ] canonicalize paths and reject traversal
- [ ] do not follow arbitrary reparse points during archive creation
- [ ] verify signatures when available
- [ ] isolate plug-in runtime probing
- [ ] require explicit consent before installer execution
- [ ] minimize administrator execution scope
- [ ] never log credentials/tokens/license secrets
- [ ] tamper-evident snapshot integrity metadata

Later consider optional snapshot signing.

---

# Phase 21 — Performance and external-drive behavior

A real studio may contain terabytes of samples.

- [ ] streamed hashing/copying
- [ ] bounded parallelism
- [ ] cancellation/resume
- [ ] progress by bytes, not only file count
- [ ] deduplication
- [ ] detect slow/removable/offline storage
- [ ] optional local cache for frequently used small assets
- [ ] avoid rescanning unchanged object payloads

Do not put latency-sensitive binaries on a slow HDD by default merely because they can be linked there.

---

# Phase 22 — CLI UX

Initial commands:

```text
studioport volume init <path>
studioport machine scan
studioport plugins scan
studioport snapshot create <path>
studioport snapshot list <path>
studioport compare <snapshot>
studioport ready <snapshot>
studioport restore <snapshot> --dry-run
studioport restore <snapshot>
studioport verify <snapshot>
```

All mutating commands support machine-readable JSON result output in addition to human-readable console output.

Exit codes must distinguish success, warnings, incomplete prerequisites and hard failure so the CLI can later be scripted.

---

# Phase 23 — Desktop UI

Implement only after core/CLI semantics are stable.

Recommended Windows UI: WPF on .NET 10.

Screens:

- Dashboard
- Source Machine
- Plug-ins
- Libraries
- Presets
- Installers
- Snapshots
- Machine Compare
- Restore Plan
- Vendor/Activation Requirements
- Audio/MIDI Mapping
- Verification
- Settings

The UI must call application services used by the CLI; no restore logic belongs only in UI code.

---

# Phase 24 — Format expansion

V1 focuses on VST3/Cubase/Windows.

Architecture should permit later scanners for:

- VST2 where legally/technically appropriate for existing installations
- CLAP
- standalone instruments/effects
- DAW extensions

Do not add these before the VST3 workflow is reliable.

---

# MVP milestone

The first useful release should provide a trustworthy core loop, not superficial vendor breadth.

## MVP implementation block A — Foundation

- [ ] solution/projects/tests
- [ ] core models and enums
- [ ] JSON manifest versioning
- [ ] StudioPort volume identity
- [ ] logical path resolver
- [ ] hashing/integrity primitives

## MVP implementation block B — Discovery

- [ ] machine basics
- [ ] VST3 standard-location scan
- [ ] package boundary detection
- [ ] hashes/version/architecture/publisher
- [ ] safe reparse-point behavior
- [ ] uninstall-registry product inventory
- [ ] initial correlation engine

## MVP implementation block C — Snapshot

- [ ] size planning
- [ ] content-addressed storage
- [ ] staging + atomic snapshot publish
- [ ] deterministic manifest
- [ ] snapshot verification

## MVP implementation block D — Compare/restore

- [ ] target scan
- [ ] compare report
- [ ] dry-run restore plan
- [ ] Mirror restore
- [ ] supported Link restore
- [ ] transaction journal
- [ ] post-restore verification

## MVP acceptance scenario

On workstation A:

```text
studioport volume init E:\StudioPort
studioport plugins scan
studioport snapshot create E:\StudioPort
```

On workstation B after normal DAW/prerequisite installation:

```text
studioport compare E:\StudioPort
studioport restore E:\StudioPort --dry-run
studioport restore E:\StudioPort
studioport verify E:\StudioPort
```

The result must accurately distinguish:

- restored plug-ins
- already matching plug-ins
- version/hash conflicts
- missing installers/runtimes
- activation requirements
- unsupported/unknown products

No licensing state is cloned and no uncertain dependency is silently copied.

---

# Testing strategy

## Unit tests

- schema serialization/version validation
- path normalization/traversal rejection
- logical volume resolution
- package hashing
- deterministic manifests
- VST3 package classification
- correlation scoring
- restore-plan conflict detection
- snapshot diff

## Integration tests

Use temporary fake filesystem/registry abstractions wherever possible.

Scenarios:

- single-file `.vst3`
- bundle/directory `.vst3`
- symlinked package
- circular/broken link
- duplicate binaries
- same product, different version
- corrupted payload
- StudioPort drive letter changed
- external drive missing mid-operation
- insufficient free space
- partial snapshot staging cleanup
- restore interrupted and resumed
- rollback after failed restore step

Tests must never mutate the developer's real plug-in folders or production registry state.

---

# Immediate implementation order

1. Repository/solution foundation.
2. Core domain model and snapshot schema.
3. StudioPort volume identity + logical path resolution.
4. SHA-256 package hashing.
5. VST3 filesystem scanner with safe link handling.
6. Windows installed-product scanner using uninstall registry locations.
7. Correlation/reporting.
8. Snapshot object store.
9. Compare/readiness engine.
10. Dry-run restore planner.
11. Mirror restore and verification.
12. Link restore after capability checks.
13. Cubase and Steinberg adapters.
14. Install Capture.
15. Additional vendor adapters discovered from real-world StudioPort scans.

The implementation should finish coherent blocks from this list rather than adding isolated features from later phases.
