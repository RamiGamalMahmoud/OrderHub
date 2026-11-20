using Xunit;

// Database-backed tests share one PostgreSQL schema and truncate it between
// tests, so test classes must not run concurrently inside a process.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
