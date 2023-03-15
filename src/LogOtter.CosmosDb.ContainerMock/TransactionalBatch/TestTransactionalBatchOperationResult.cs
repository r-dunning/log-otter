﻿using System.Net;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb.ContainerMock.TransactionalBatch;

internal class TestTransactionalBatchOperationResult : TransactionalBatchOperationResult
{
    private readonly ResponseMessage _responseMessage;

    public override HttpStatusCode StatusCode => _responseMessage.StatusCode;
    public override Stream ResourceStream => _responseMessage.Content;
    public override string ETag => _responseMessage.Headers.ETag;

    public TestTransactionalBatchOperationResult(ResponseMessage responseMessage)
    {
        _responseMessage = responseMessage;
    }
}

internal class TestTransactionalBatchOperationResult<T> : TransactionalBatchOperationResult<T>
{
    private readonly string _eTag;
    private readonly Stream _resourceStream;
    private readonly HttpStatusCode _statusCode;

    public sealed override T Resource { get; set; }

    public override HttpStatusCode StatusCode => _statusCode;

    public override string ETag => _eTag;

    public override Stream ResourceStream => _resourceStream;

    public TestTransactionalBatchOperationResult(TransactionalBatchOperationResult result, T resource)
    {
        Resource = resource;
        _statusCode = result.StatusCode;
        _eTag = result.ETag;
        _resourceStream = result.ResourceStream;
    }
}
