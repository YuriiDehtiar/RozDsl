// Roz.Language/Validation/SemanticValidator.cs
using System;
using System.Collections.Generic;

using Roz.Language.Ast;
using Roz.Language.Diagnostics;

namespace Roz.Language.Validation;

/// <summary>
/// Семантична (доменна) валідація AST.
/// Важливо: у v1 наші AST-вузли не зберігають позиції вхідного тексту,
/// тому для Diagnostic.Span ставимо TextSpan(0,0).
/// (На "наступному рівні" можна додати Span у вузли AST або окрему таблицю прив’язок.)
/// </summary>
internal sealed class SemanticValidator
{
    private static readonly TextSpan NoSpan = new(0, 0);

    public void Validate(RozDocument document, DiagnosticBag diagnostics)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        if (diagnostics is null) throw new ArgumentNullException(nameof(diagnostics));

        ValidateServiceNamesUnique(document, diagnostics);

        foreach (var svc in document.Services)
            ValidateService(svc, diagnostics);
    }

    private static void ValidateServiceNamesUnique(RozDocument document, DiagnosticBag diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var svc in document.Services)
        {
            // Name завжди має бути не пустий (у конструкторі ServiceDecl є перевірка),
            // але додатково підстрахуємось.
            var name = svc.Name ?? string.Empty;

            if (!seen.Add(name))
            {
                diagnostics.Report(
                    "ROZ200",
                    $"Дубль назви сервісу: '{name}'. Назви service мають бути унікальні.",
                    NoSpan);
            }
        }
    }

    private static void ValidateService(ServiceDecl svc, DiagnosticBag diagnostics)
    {
        // image — обов'язкове
        if (string.IsNullOrWhiteSpace(svc.Image))
        {
            diagnostics.Report(
                "ROZ201",
                $"У сервісі '{svc.Name}' не задано 'image' або значення порожнє.",
                NoSpan);
        }

        // replicas — обов'язкове і > 0
        if (svc.Replicas is null)
        {
            diagnostics.Report(
                "ROZ202",
                $"У сервісі '{svc.Name}' не задано 'replicas'.",
                NoSpan);
        }
        else if (svc.Replicas.Value <= 0)
        {
            diagnostics.Report(
                "ROZ203",
                $"У сервісі '{svc.Name}' значення 'replicas' має бути > 0 (зараз: {svc.Replicas.Value}).",
                svc.ReplicasSpan);
        }

        // ports — кожен порт у діапазоні 1..65535
        foreach (var p in svc.Ports)
        {
            if (p.HostPort < 1 || p.HostPort > 65535)
            {
                diagnostics.Report(
                    "ROZ204",
                    $"У сервісі '{svc.Name}' host port поза діапазоном 1..65535 (зараз: {p.HostPort}).",
                    NoSpan);
            }

            if (p.ContainerPort < 1 || p.ContainerPort > 65535)
            {
                diagnostics.Report(
                    "ROZ205",
                    $"У сервісі '{svc.Name}' container port поза діапазоном 1..65535 (зараз: {p.ContainerPort}).",
                    NoSpan);
            }
        }
    }
}
