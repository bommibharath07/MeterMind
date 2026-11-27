using Xunit;

// These acceptance classes share one explicitly isolated PostgreSQL database.
// Serialize schema setup and scenario mutations; independent test projects still run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
