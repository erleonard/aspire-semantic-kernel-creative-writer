using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;
using System.Numerics;

namespace ChatApp.WebApi.Model;

internal sealed class ProductDataModel
{
    [VectorStoreRecordKey]
    public string Key { get; set; }

    [VectorStoreRecordData]
    [TextSearchResultName]
    public string Name { get; set; }

    [VectorStoreRecordData]
    [TextSearchResultValue]
    public string Content { get; set; }

    [VectorStoreRecordVector(3072)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}