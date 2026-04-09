using System;
using System.Collections.Generic;
using System.Text.Json;
using Roz.Language.Diagnostics;
using Roz.Lsp.Documents;

namespace Roz.Lsp.Protocol;

internal sealed class LspRequestDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly LspSession _session;

    public LspRequestDispatcher(LspSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public DispatchOutcome Dispatch(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Incoming JSON-RPC payload is empty.");
        }

        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize JSON-RPC request.");

        FileLogger.Info($"Dispatch method: {request.Method}");

        return request.Method switch
        {
            "initialize" => HandleInitialize(request),
            "initialized" => HandleInitialized(),
            "shutdown" => HandleShutdown(request),
            "exit" => HandleExit(),
            "textDocument/didOpen" => HandleDidOpen(request),
            "textDocument/didChange" => HandleDidChange(request),
            "textDocument/didClose" => HandleDidClose(request),
            "textDocument/completion" => HandleCompletion(request),
            "textDocument/hover" => HandleHover(request),
            "textDocument/documentSymbol" => HandleDocumentSymbols(request),
            "textDocument/definition" => HandleDefinition(request),
            "NotificationReceived" => HandleNotificationReceived(),
            _ => throw new NotSupportedException($"Method '{request.Method}' is not supported yet.")
        };
    }

    private DispatchOutcome HandleInitialize(JsonRpcRequest request)
    {
        var parameters = request.Params.ValueKind == JsonValueKind.Null
            ? new InitializeParams()
            : request.Params.Deserialize<InitializeParams>(JsonOptions) ?? new InitializeParams();

        var result = _session.Initialize(parameters);

        var response = new JsonRpcResponse<InitializeResult>
        {
            Id = request.Id,
            Result = result
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        FileLogger.Info($"Initialize response JSON: {json}");

        return new DispatchOutcome
        {
            ResponseJson = json,
            ShouldExit = false
        };
    }

    private DispatchOutcome HandleInitialized()
    {
        FileLogger.Info("LSP initialized called");

        return new DispatchOutcome
        {
            ResponseJson = null,
            ShouldExit = false
        };
    }

    private DispatchOutcome HandleShutdown(JsonRpcRequest request)
    {
        var result = _session.Shutdown();

        var response = new JsonRpcResponse<object?>
        {
            Id = request.Id,
            Result = result
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        FileLogger.Info($"Shutdown response JSON: {json}");

        return new DispatchOutcome
        {
            ResponseJson = json,
            ShouldExit = false
        };
    }

    private DispatchOutcome HandleExit()
    {
        var shouldExit = _session.Exit();

        return new DispatchOutcome
        {
            ResponseJson = null,
            ShouldExit = shouldExit
        };
    }

    private DispatchOutcome HandleDidOpen(JsonRpcRequest request)
    {
        var parameters = request.Params.Deserialize<DidOpenTextDocumentParams>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize didOpen params.");

        var result = _session.DidOpen(parameters);

        var outcome = new DispatchOutcome();
        outcome.NotificationJsons.Add(BuildPublishDiagnosticsJson(result));

        return outcome;
    }

    private DispatchOutcome HandleDidChange(JsonRpcRequest request)
    {
        var parameters = request.Params.Deserialize<DidChangeTextDocumentParams>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize didChange params.");

        var result = _session.DidChange(parameters);

        var outcome = new DispatchOutcome();
        outcome.NotificationJsons.Add(BuildPublishDiagnosticsJson(result));

        return outcome;
    }

    private DispatchOutcome HandleDidClose(JsonRpcRequest request)
    {
        var parameters = request.Params.Deserialize<DidCloseTextDocumentParams>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize didClose params.");

        _session.DidClose(parameters);

        var clearDiagnostics = new PublishDiagnosticsParams
        {
            Uri = parameters.TextDocument.Uri,
            Diagnostics = Array.Empty<LspDiagnostic>()
        };

        var outcome = new DispatchOutcome();
        outcome.NotificationJsons.Add(SerializeNotification("textDocument/publishDiagnostics", clearDiagnostics));

        return outcome;
    }

    private DispatchOutcome HandleCompletion(JsonRpcRequest request)
    {
        var parameters = request.Params.Deserialize<CompletionParams>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize completion params.");

        var result = _session.GetCompletion(parameters);

        var response = new JsonRpcResponse<CompletionList>
        {
            Id = request.Id,
            Result = result
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        FileLogger.Info($"Completion response JSON: {json}");

        return new DispatchOutcome
        {
            ResponseJson = json,
            ShouldExit = false
        };
    }

    private DispatchOutcome HandleHover(JsonRpcRequest request)
    {
        var parameters = request.Params.Deserialize<HoverParams>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize hover params.");

        var result = _session.GetHover(parameters);

        var response = new JsonRpcResponse<Hover?>
        {
            Id = request.Id,
            Result = result
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        FileLogger.Info($"Hover response JSON: {json}");

        return new DispatchOutcome
        {
            ResponseJson = json,
            ShouldExit = false
        };
    }

    private DispatchOutcome HandleDocumentSymbols(JsonRpcRequest request)
    {
        var parameters = request.Params.Deserialize<DocumentSymbolParams>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize documentSymbol params.");

        var result = _session.GetDocumentSymbols(parameters);

        var response = new JsonRpcResponse<DocumentSymbol[]>
        {
            Id = request.Id,
            Result = result
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        FileLogger.Info($"DocumentSymbol response JSON: {json}");

        return new DispatchOutcome
        {
            ResponseJson = json,
            ShouldExit = false
        };
    }

    private DispatchOutcome HandleDefinition(JsonRpcRequest request)
    {
        var parameters = request.Params.Deserialize<DefinitionParams>(JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize definition params.");

        var result = _session.GetDefinition(parameters);

        var response = new JsonRpcResponse<Location[]>
        {
            Id = request.Id,
            Result = result
        };

        var json = JsonSerializer.Serialize(response, JsonOptions);
        FileLogger.Info($"Definition response JSON: {json}");

        return new DispatchOutcome
        {
            ResponseJson = json,
            ShouldExit = false
        };
    }

    private DispatchOutcome HandleNotificationReceived()
    {
        FileLogger.Info("NotificationReceived ignored");

        return new DispatchOutcome
        {
            ResponseJson = null,
            ShouldExit = false
        };
    }

    private string BuildPublishDiagnosticsJson(AnalyzedDocument analyzedDocument)
    {
        var diagnostics = new List<LspDiagnostic>();

        foreach (var item in analyzedDocument.Compilation.Diagnostics)
        {
            var diagnostic = (Diagnostic)item;

            diagnostics.Add(new LspDiagnostic
            {
                Range = TextPositionMapper.FromSpan(
                    analyzedDocument.Document.Text,
                    diagnostic.Span.Start,
                    diagnostic.Span.Length),
                Severity = 1,
                Code = diagnostic.Code,
                Source = "RozDsl",
                Message = diagnostic.Message
            });
        }

        var payload = new PublishDiagnosticsParams
        {
            Uri = analyzedDocument.Document.Uri,
            Diagnostics = diagnostics.ToArray()
        };

        return SerializeNotification("textDocument/publishDiagnostics", payload);
    }

    private static string SerializeNotification<T>(string method, T parameters)
    {
        var notification = new JsonRpcNotification<T>
        {
            Method = method,
            Params = parameters
        };

        var json = JsonSerializer.Serialize(notification, JsonOptions);
        FileLogger.Info($"Notification JSON ({method}): {json}");

        return json;
    }
}