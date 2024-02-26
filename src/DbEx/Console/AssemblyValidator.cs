// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using CoreEx;
using DbEx.Migration;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DbEx.Console
{
    /// <summary>
    /// Validates the assembly name(s).
    /// </summary>
    /// <param name="args">The <see cref="MigrationArgsBase"/> to update.</param>
    public class AssemblyValidator(MigrationArgsBase args) : IOptionValidator
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

            var list = new List<Assembly>();
            foreach (var name in option.Values.Where(x => !string.IsNullOrEmpty(x)))
            {
                try
                {
                    // Load from the specified file on the file system or by using its long form name. 
                    _args.AddAssembly(File.Exists(name) ? Assembly.LoadFrom(name!) : Assembly.Load(name!));
                }
                catch (Exception ex)
                {
                    return new ValidationResult($"The specified assembly '{name}' is invalid: {ex.Message}");
                }
            }

            return ValidationResult.Success!;
        }
    }
}