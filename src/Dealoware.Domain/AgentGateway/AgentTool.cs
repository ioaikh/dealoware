namespace Dealoware.Domain.AgentGateway;

using Dealoware.Domain.FieldAcl;

/// <summary>
/// Represents a platform tool that agents can invoke via the gateway.
/// Each tool declares the FieldClasses it may access.
/// Stage C #67: Tools must be explicitly registered on the allowlist.
/// </summary>
public sealed class AgentTool
{
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<FieldClassDeclaration> FieldDeclarations { get; }

    private AgentTool(string name, string description, IReadOnlyList<FieldClassDeclaration> fieldDeclarations)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        FieldDeclarations = fieldDeclarations ?? throw new ArgumentNullException(nameof(fieldDeclarations));
    }

    public static AgentTool Create(string name, string description, params FieldClassDeclaration[] declarations)
    {
        return new AgentTool(name, description, declarations.ToList().AsReadOnly());
    }

    /// <summary>
    /// Checks if this tool declares access to the given FieldClass with the specified action.
    /// Returns false if undeclared — caller must deny.
    /// </summary>
    public bool DeclaresAccess(FieldClass fieldClass, FieldAction action)
    {
        foreach (var declaration in FieldDeclarations)
        {
            if (declaration.FieldClass == fieldClass && declaration.AllowsAction(action))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets all FieldClasses this tool declares for any action.
    /// Used for validating tool responses don't contain undeclared fields.
    /// </summary>
    public IEnumerable<FieldClass> GetDeclaredFieldClasses()
    {
        return FieldDeclarations.Select(d => d.FieldClass).Distinct();
    }

    /// <summary>
    /// Gets all FieldClasses declared for a specific action.
    /// </summary>
    public IEnumerable<FieldClass> GetDeclaredFieldClassesForAction(FieldAction action)
    {
        return FieldDeclarations
            .Where(d => d.AllowsAction(action))
            .Select(d => d.FieldClass);
    }
}
