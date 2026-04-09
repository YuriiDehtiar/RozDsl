using System.Text.Json;

namespace Roz.Lsp.Protocol;

internal sealed class JsonRpcRequest
{
    public string Jsonrpc { get; set; } = string.Empty;

    public JsonElement Id { get; set; }

    public string Method { get; set; } = string.Empty;

    public JsonElement Params { get; set; }
}

internal sealed class JsonRpcResponse<T>
{
    public string Jsonrpc { get; set; } = "2.0";

    public JsonElement Id { get; set; }

    public T Result { get; set; } = default!;
}

internal sealed class JsonRpcNotification<T>
{
    public string Jsonrpc { get; set; } = "2.0";

    public string Method { get; set; } = string.Empty;

    public T Params { get; set; } = default!;
}