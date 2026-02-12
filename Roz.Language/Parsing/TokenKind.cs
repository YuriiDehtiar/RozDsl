// Roz.Language/Parsing/TokenKind.cs
namespace Roz.Language.Parsing;

/// <summary>
/// All token kinds supported by the .roz language (v1).
/// Lexer produces these tokens; Parser consumes them.
/// </summary>
public enum TokenKind
{
    // --- special / control -------------------------------------------------
    EndOfFile,
    BadToken,

    // --- trivia ------------------------------------------------------------
    Whitespace,
    NewLine,
    LineComment, // e.g. // comment (optional to emit; lexer may also skip)

    // --- literals / identifiers -------------------------------------------
    Identifier,
    Number,
    String,

    // --- keywords ----------------------------------------------------------
    ServiceKeyword,   // service
    ImageKeyword,     // image
    ReplicasKeyword,  // replicas
    PortKeyword,      // port

    // --- punctuation -------------------------------------------------------
    OpenBrace,   // {
    CloseBrace,  // }
    Colon        // :
}
