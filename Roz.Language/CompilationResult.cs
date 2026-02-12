// Roz.Language/CompilationResult.cs
using System;
using System.Collections.Generic;

using Roz.Language.Diagnostics;

namespace Roz.Language;

/// <summary>
/// Result of compiling a .roz file:
/// - Diagnostics: errors/warnings collected during lexing/parsing/validation/codegen
/// - Json: generated output (null if errors occurred or generation not executed)
/// </summary>
public sealed class CompilationResult
{
    public CompilationResult(string? json, IReadOnlyList<Diagnostic> diagnostics)
    {
        Json = json;
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public string? Json { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    public bool HasErrors => Diagnostics.Count > 0;
}
