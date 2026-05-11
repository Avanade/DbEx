using DbEx.CodeGen.Config;
using OnRamp.Generators;

namespace DbEx.CodeGen.Generators;

/// <summary>
/// Provides the entity-framework model builder code-generator.
/// </summary>
public class EfModelBuilderGenerator : CodeGeneratorBase<CodeGenConfig, CodeGenConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<CodeGenConfig> SelectGenConfig(CodeGenConfig config) => config.EfModelBuilders.Count == 0 ? [] : [config];
}