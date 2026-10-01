namespace MvcApp.Infrastructure.Seeding;

public sealed record SeedPackResult(string PackName, int RowsRecorded, int RowsAlreadyPresent);

public sealed record SeedPackRemoval(string PackName, int RowsDeleted, IReadOnlyDictionary<string, int> ByEntityType);

public sealed record SeedPackEntityCount(string EntityType, int Rows);