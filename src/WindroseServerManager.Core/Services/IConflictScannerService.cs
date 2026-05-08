using WindroseServerManager.Core.Models;

namespace WindroseServerManager.Core.Services;

public interface IConflictScannerService
{
    /// <summary>
    /// Scans for conflicts between enabled pak mods and active Windrose+ multipliers.
    /// Returns an empty list if no conflicts are detected.
    /// </summary>
    IReadOnlyList<ConflictResult> ScanForConflicts();
}
