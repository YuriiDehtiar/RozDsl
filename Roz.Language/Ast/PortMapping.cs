// Roz.Language/Ast/PortMapping.cs
using System;

namespace Roz.Language.Ast;

/// <summary>
/// AST-вузол: мапінг портів host:container (наприклад 8080:80).
/// </summary>
public sealed class PortMapping
{
    public PortMapping(int hostPort, int containerPort)
    {
        HostPort = hostPort;
        ContainerPort = containerPort;
    }

    public int HostPort { get; }

    public int ContainerPort { get; }

    public override string ToString() => $"{HostPort}:{ContainerPort}";
}
