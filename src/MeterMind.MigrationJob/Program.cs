using MeterMind.Persistence;
using Microsoft.EntityFrameworkCore;
await using var db = new DesignFactory().CreateDbContext(args);
await db.Database.MigrateAsync();
Console.WriteLine("Database migrations completed.");
