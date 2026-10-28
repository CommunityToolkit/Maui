using Xunit;
using Xunit.Sdk;
using Xunit.v3;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
[assembly: Parallelization(Mode = ParallelMode.None, MaxThreads = 1)]