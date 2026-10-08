using Xunit;

// The database tests share the real dev MySQL: some add and remove throw-away users, roles and questions while others
// count or list them. Running the test classes one after another keeps those counts stable.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
