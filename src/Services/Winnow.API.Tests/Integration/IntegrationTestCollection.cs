using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Winnow.API.Tests.Integration;

// 1. Define a "Collection" name
// 2. Disable Parallelization for anything in this collection
[CollectionDefinition("Integration Tests", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<WinnowTestApp>
{
    // This class has no code, and is never created. 
    // Its purpose is simply to be the place to apply [CollectionDefinition].
}
