﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/DbEx

using DbEx.Migration;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using YamlDotNet.Core.Tokens;

namespace DbEx.Console
{
    /// <summary>
    /// Validate the Params to ensure format is correct and values are not duplicated.
    /// </summary>
    public class ParametersValidator : IOptionValidator
    {
        private readonly MigrationArgsBase _args;

        /// <summary>
        /// Initilizes a new instance of the <see cref="ParametersValidator"/> class.
        /// </summary>
        /// <param name="args">The <see cref="MigrationArgsBase"/> to update.</param>
        public ParametersValidator(MigrationArgsBase args) => _args = args ?? throw new ArgumentNullException(nameof(args));

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

            foreach (var p in option.Values.Where(x => !string.IsNullOrEmpty(x)))
            {
                var pos = p!.IndexOf("=", StringComparison.InvariantCultureIgnoreCase);
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