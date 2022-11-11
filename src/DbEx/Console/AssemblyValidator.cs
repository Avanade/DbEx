// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

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
    public class AssemblyValidator : IOptionValidator
    {
        private readonly MigratorConsoleArgsBase _args;

        /// <summary>
        /// Initilizes a new instance of the <see cref="AssemblyValidator"/> class.
        /// </summary>
        /// <param name="args">The <see cref="MigratorConsoleArgs"/> to update.</param>
        public AssemblyValidator(MigratorConsoleArgsBase args) => _args = args ?? throw new ArgumentNullException(nameof(args));

        /// <summary>
        /// Performs the validation.
        /// </summary>
        /// <param name="option">The <see cref="CommandOption"/>.</param>
        /// <param name="context">The <see cref="ValidationContext"/>.</param>
        /// <returns>The <see cref="ValidationResult"/>.</returns>
        public ValidationResult GetValidationResult(CommandOption option, ValidationContext context)
        {
            if (option == null)
                throw new ArgumentNullException(nameof(option));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var list = new List<Assembly>();
            foreach (var name in option.Values.Where(x => !string.IsNullOrEmpty(x)))
            {
                try
                {
                    // Load from the specified file on the file system or by using its long form name. 
                    list.Add(File.Exists(name) ? Assembly.LoadFrom(name!) : Assembly.Load(name!));
                }
                catch (Exception ex)
                {
                    return new ValidationResult($"The specified assembly '{name}' is invalid: {ex.Message}");
                }
            }

            _args.Assemblies.InsertRange(0, list.ToArray());
            return ValidationResult.Success!;
        }
    }
}