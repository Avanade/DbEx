using OnRamp.Scripts;

namespace DbEx.CodeGen;

/// <summary>
/// Represents the code generator for database-related code generation, such as for models, contexts, and related code artefacts.
/// </summary>
internal class DbCodeGenerator(CodeGeneratorArgs args, CodeGenScript scripts) : CodeGenerator(args, scripts)
{
    private DatabaseMigrationBase? _migrator;

    /// <summary>
    /// Gets or sets the requisite <see cref="DatabaseMigrationBase"/>.
    /// </summary>
    public DatabaseMigrationBase Migrator { get => _migrator ?? throw new InvalidOperationException("Migrator is not set."); set => _migrator = value; }
}