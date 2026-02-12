// Roz.Language/Ast/RozDocument.cs
using System;
using System.Collections.Generic;

namespace Roz.Language.Ast;

/// <summary>
/// Кореневий вузол AST для всього файлу .roz.
/// Містить список оголошених сервісів.
/// </summary>
public sealed class RozDocument
{
    public RozDocument(IReadOnlyList<ServiceDecl> services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>Усі service-блоки з файлу.</summary>
    public IReadOnlyList<ServiceDecl> Services { get; }
}
