﻿using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Xunit;

namespace LogOtter.CosmosDb.ContainerMock.IntegrationTests;

[Collection("Integration Tests")]
public sealed class CosmosCreateContainerWithUniqueKeyIncludingId : IAsyncLifetime, IDisposable
{
    private readonly TestCosmos _testCosmos;

    public CosmosCreateContainerWithUniqueKeyIncludingId(IntegrationTestsFixture testFixture)
    {
        _testCosmos = testFixture.CreateTestCosmos();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _testCosmos.CleanupAsync();
    }

    public void Dispose()
    {
        _testCosmos.Dispose();
    }

    [Fact]
    public async Task CreateUniqueKeyViolationIsEquivalent()
    {
        var uniqueKeyPolicy = new UniqueKeyPolicy { UniqueKeys = { new UniqueKey { Paths = { "/id", "/ItemId", "/Type" } } } };

        var (realException, testException) = await _testCosmos.SetupAsyncProducesExceptions("/partitionKey", uniqueKeyPolicy);

        realException.Should().NotBeNull();
        testException.Should().NotBeNull();
        realException!.StatusCode.Should().Be(testException!.StatusCode);
        realException.Should().BeOfType(testException.GetType());
    }
}
