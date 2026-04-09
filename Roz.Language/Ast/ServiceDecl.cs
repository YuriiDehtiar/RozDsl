// Roz.Language/Ast/ServiceDecl.cs
using System;
using System.Collections.Generic;
using Roz.Language.Diagnostics;

namespace Roz.Language.Ast;

/// <summary>
/// AST-вузол: оголошення одного сервісу
/// service <name> { image "..."; replicas N; port A:B; ... }
/// </summary>
public sealed class ServiceDecl
{
    public ServiceDecl(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Service name must be non-empty.", nameof(name));

        Name = name;
        Ports = new List<PortMapping>();
        ReplicasSpan = new TextSpan(0, 0);
    }

    /// <summary>Назва сервісу (ідентифікатор після 'service').</summary>
    public string Name { get; }

    /// <summary>Образ (рядок після 'image'). Може бути null, доки не заповнить парсер/валідація.</summary>
    public string? Image { get; set; }

    /// <summary>Кількість екземплярів (число після 'replicas'). Може бути null, доки не заповнить парсер/валідація.</summary>
    public int? Replicas { get; set; }

    /// <summary>Точний span числа після 'replicas'. Потрібен для semantic diagnostics у LSP.</summary>
    public TextSpan ReplicasSpan { get; set; }

    /// <summary>Список портів (кожен 'port host:container').</summary>
    public List<PortMapping> Ports { get; }
}