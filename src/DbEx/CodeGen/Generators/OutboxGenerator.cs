using DbEx.CodeGen.Config;
using OnRamp.Generators;

namespace DbEx.CodeGen.Generators;

/// <summary>
/// Provides the outbox code-generator.
/// </summary>
public class OutboxGenerator : CodeGeneratorBase<CodeGenConfig, CodeGenConfig>
{
    /// <inheritdoc/>
    protected override IEnumerable<CodeGenConfig> SelectGenConfig(CodeGenConfig config) => config.Outbox.HasValue && config.Outbox.Value ? [config] : [];
}