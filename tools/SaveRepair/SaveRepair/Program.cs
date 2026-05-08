using System.Text;
using RocksDbSharp;

var basePath = @"C:\Users\jacob\AppData\Local\R5\Saved\SaveProfiles\76561197980947929";

var dbDirs = new List<string>();
foreach (var version in new[] { "RocksDB", "RocksDB_v2" })
{
    var playersRoot = Path.Combine(basePath, version, "0.10.0", "Players");
    if (Directory.Exists(playersRoot))
        foreach (var dir in Directory.GetDirectories(playersRoot))
            dbDirs.Add(dir);

    var accountsRoot = Path.Combine(basePath, version, "0.10.0", "Accounts");
    if (Directory.Exists(accountsRoot))
        foreach (var dir in Directory.GetDirectories(accountsRoot))
            dbDirs.Add(dir);
}

Console.WriteLine($"Found {dbDirs.Count} databases to scan\n");

foreach (var dbPath in dbDirs)
{
    Console.WriteLine($"=== {dbPath} ===");
    try
    {
        var options = new DbOptions().SetCreateIfMissing(false);
        using var db = RocksDb.OpenReadOnly(options, dbPath, false);
        using var iter = db.NewIterator();
        int count = 0;
        iter.SeekToFirst();
        while (iter.Valid())
        {
            count++;
            iter.Next();
        }
        Console.WriteLine($"  Total keys: {count}");

        // Now dump all keys
        using var iter2 = db.NewIterator();
        iter2.SeekToFirst();
        while (iter2.Valid())
        {
            var key = iter2.StringKey();
            var value = iter2.Value();
            var preview = TryReadValue(value);
            if (preview.Length > 200) preview = preview[..200] + "...";
            Console.WriteLine($"  {key} = {preview}");
            iter2.Next();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  Error: {ex.Message}");
    }
    Console.WriteLine();
}

static string TryReadValue(byte[] bytes)
{
    if (bytes.Length == 0) return "(empty)";
    try
    {
        var str = Encoding.UTF8.GetString(bytes);
        if (str.All(c => !char.IsControl(c) || c == '\n' || c == '\r' || c == '\t'))
            return str;
    }
    catch { }
    if (bytes.Length == 4)
        return $"int32={BitConverter.ToInt32(bytes)} float={BitConverter.ToSingle(bytes)}";
    if (bytes.Length == 8)
        return $"int64={BitConverter.ToInt64(bytes)} double={BitConverter.ToDouble(bytes)}";
    return $"({bytes.Length} bytes) {BitConverter.ToString(bytes.Take(Math.Min(32, bytes.Length)).ToArray())}";
}
