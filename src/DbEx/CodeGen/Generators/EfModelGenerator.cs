using DbEx.CodeGen.Config;
using OnRamp.Generators;

namespace DbEx.CodeGen.Generators;

/// <summary>
/// Provides the entity-framework model code-generator.
/// </summary>
public class EfModelGenerator : CodeGeneratorBase<CodeGenConfig, TableConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<TableConfig> SelectGenConfig(CodeGenConfig config) => config.EfModels;
}