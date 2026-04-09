using System.Collections.Generic;

namespace Roz.Lsp.Protocol;

internal sealed class DispatchOutcome
{
    public string? ResponseJson { get; set; }

    public List<string> NotificationJsons { get; } = new();

    public bool ShouldExit { get; set; }
}