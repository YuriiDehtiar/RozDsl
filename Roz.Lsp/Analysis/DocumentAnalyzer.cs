using Roz.Language;

namespace Roz.Lsp.Analysis;

internal sealed class DocumentAnalyzer
{
    private readonly RozCompiler _compiler = new();

    public CompilationResult Analyze(string text)
    {
        return _compiler.CompileText(text ?? string.Empty);
    }
}