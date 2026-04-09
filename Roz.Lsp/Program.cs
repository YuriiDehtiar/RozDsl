using System;
using System.IO;
using Roz.Lsp.Analysis;
using Roz.Lsp.Documents;
using Roz.Lsp.Protocol;
using Roz.Lsp.Transport;

namespace Roz.Lsp;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            FileLogger.Info("Roz.Lsp starting");

            var store = new DocumentStore();
            var analyzer = new DocumentAnalyzer();
            var pipeline = new DocumentPipeline(store, analyzer);
            var session = new LspSession(pipeline);
            var dispatcher = new LspRequestDispatcher(session);

            if (args.Length > 0 && string.Equals(args[0], "--selftest", StringComparison.OrdinalIgnoreCase))
            {
                RunSelfTest(dispatcher);
            }
            else
            {
                RunStdioLoop(dispatcher);
            }

            FileLogger.Info("Roz.Lsp finished");
            return 0;
        }
        catch (Exception ex)
        {
            FileLogger.Error(ex, "Fatal error during startup");
            return 1;
        }
    }

    private static void RunSelfTest(LspRequestDispatcher dispatcher)
    {
        FileLogger.Info("Running self-test mode");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"rootUri":"file:///d:/temp"}}
""",
            "initialize");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","method":"initialized","params":{}}
""",
            "initialized");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","method":"textDocument/didOpen","params":{"textDocument":{"uri":"file:///d:/temp/test.roz","languageId":"roz","version":1,"text":"service web {\n  image \"nginx:latest\"\n  replicas 2\n  port 8080:80\n}\n"}}}
""",
            "didOpen");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","id":30,"method":"textDocument/completion","params":{"textDocument":{"uri":"file:///d:/temp/test.roz"},"position":{"line":0,"character":0}}}
""",
            "completion-top-level");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","id":3,"method":"textDocument/completion","params":{"textDocument":{"uri":"file:///d:/temp/test.roz"},"position":{"line":3,"character":2}}}
""",
            "completion-inside-service");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","id":40,"method":"textDocument/hover","params":{"textDocument":{"uri":"file:///d:/temp/test.roz"},"position":{"line":2,"character":3}}}
""",
            "hover-replicas");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","id":50,"method":"textDocument/documentSymbol","params":{"textDocument":{"uri":"file:///d:/temp/test.roz"}}}
""",
            "documentSymbol");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","id":60,"method":"textDocument/definition","params":{"textDocument":{"uri":"file:///d:/temp/test.roz"},"position":{"line":0,"character":9}}}
""",
            "definition-service-name");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","method":"textDocument/didChange","params":{"textDocument":{"uri":"file:///d:/temp/test.roz","version":2},"contentChanges":[{"text":"service web {\n  image \"nginx:latest\"\n  replicas 0\n}\n"}]}}
""",
            "didChange");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","method":"textDocument/didClose","params":{"textDocument":{"uri":"file:///d:/temp/test.roz"}}}
""",
            "didClose");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","id":2,"method":"shutdown","params":null}
""",
            "shutdown");

        ProcessSelfTestMessage(
            dispatcher,
            """
{"jsonrpc":"2.0","method":"exit","params":null}
""",
            "exit");
    }

    private static void ProcessSelfTestMessage(
        LspRequestDispatcher dispatcher,
        string requestJson,
        string label)
    {
        var writer = new LspMessageWriter();
        var reader = new LspMessageReader();

        using var inputStream = new MemoryStream();
        using var outputStream = new MemoryStream();

        writer.Write(inputStream, requestJson);
        inputStream.Position = 0;

        var incomingJson = reader.Read(inputStream);
        FileLogger.Info($"Self-test incoming {label}: {incomingJson}");

        var outcome = dispatcher.Dispatch(incomingJson!);

        if (!string.IsNullOrWhiteSpace(outcome.ResponseJson))
        {
            writer.Write(outputStream, outcome.ResponseJson);
            outputStream.Position = 0;

            var outgoingResponse = reader.Read(outputStream);
            FileLogger.Info($"Self-test outgoing response {label}: {outgoingResponse}");
        }

        foreach (var notificationJson in outcome.NotificationJsons)
        {
            outputStream.SetLength(0);
            outputStream.Position = 0;

            writer.Write(outputStream, notificationJson);
            outputStream.Position = 0;

            var outgoingNotification = reader.Read(outputStream);
            FileLogger.Info($"Self-test outgoing notification {label}: {outgoingNotification}");
        }

        FileLogger.Info($"Self-test ShouldExit {label}: {outcome.ShouldExit}");
    }

    private static void RunStdioLoop(LspRequestDispatcher dispatcher)
    {
        FileLogger.Info("Running stdio loop mode");

        var reader = new LspMessageReader();
        var writer = new LspMessageWriter();

        using var input = Console.OpenStandardInput();
        using var output = Console.OpenStandardOutput();

        while (true)
        {
            var incomingJson = reader.Read(input);

            if (incomingJson is null)
            {
                FileLogger.Info("STDIO input closed");
                break;
            }

            FileLogger.Info($"STDIO incoming payload: {incomingJson}");

            var outcome = dispatcher.Dispatch(incomingJson);

            if (!string.IsNullOrWhiteSpace(outcome.ResponseJson))
            {
                writer.Write(output, outcome.ResponseJson);
                FileLogger.Info($"STDIO outgoing response: {outcome.ResponseJson}");
            }

            foreach (var notificationJson in outcome.NotificationJsons)
            {
                writer.Write(output, notificationJson);
                FileLogger.Info($"STDIO outgoing notification: {notificationJson}");
            }

            if (outcome.ShouldExit)
            {
                FileLogger.Info("STDIO loop exit requested");
                break;
            }
        }
    }
}