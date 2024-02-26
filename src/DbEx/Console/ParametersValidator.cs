// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using DbEx.Migration;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace DbEx.Console
{
    /// <summary>
    /// Validate the Params to ensure format is correct and values are not duplicated.
    /// </summary>
    /// <param name="args">The <see cref="MigrationArgsBase"/> to update.</param>
    public class ParametersValidator(MigrationArgsBase args) : IOptionValidator
    {
        private readonly MigrationArgsBase _args = args.ThrowIfNull(nameof(args));

        /// <summary>
        /// Performs the validation.
        /// </summary>
        /// <param name="option">The <see cref="CommandOption"/>.</param>
        /// <param name="context">The <see cref="ValidationContext"/>.</param>
        /// <returns>The <see cref="ValidationResult"/>.</returns>
        public ValidationResult GetValidationResult(CommandOption option, ValidationContext context)
        {
            option.ThrowIfNull(nameof(option));
            context.ThrowIfNull(nameof(context));

            foreach (var p in option.Values.Where(x => !string.IsNullOrEmpty(x)))
            {
                var pos = p!.IndexOf("=", StringComparison.Ordinal);
                if (pos <= 0)
                    AddParameter(p, null);
                else
                    AddParameter(p[..pos], string.IsNullOrEmpty(p[(pos + 1)..]) ? null : p[(pos + 1)..]);
            }

            return ValidationResult.Success!;
        }

        /// <summary>
        /// Adds or overriddes the parameter.
        /// </summary>
        private void AddParameter(string key, string? value)
        {
            if (!_args.Parameters.TryAdd(key, value))
                _args.Parameters[key] = value;
        }
    }
}