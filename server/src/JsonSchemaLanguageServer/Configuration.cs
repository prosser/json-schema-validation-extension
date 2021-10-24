// <copyright file="Configuration.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class Configuration
    {
        private IReadOnlyList<string>? searchPaths;
        private IReadOnlyList<string>? searchGlobs;

        public int MaxNumberOfProblems { get; set; }

        public string? SchemaSearchPaths
        {
            get => FromArray(this.searchPaths);
            set => this.searchPaths = ToArray(value);
        }

        public string? SchemaSearchGlobs
        {
            get => FromArray(this.searchGlobs);
            set => this.searchGlobs = ToArray(value);
        }

        public string? SchemaSearchUrlPattern { get; set; }

        public Configuration Clone()
        {
            return new Configuration
            {
                MaxNumberOfProblems = this.MaxNumberOfProblems,
                SchemaSearchGlobs = this.SchemaSearchGlobs,
                SchemaSearchPaths = this.SchemaSearchPaths,
                SchemaSearchUrlPattern = this.SchemaSearchUrlPattern,
            };
        }

        public bool Equals(Configuration other)
        {
            return other.MaxNumberOfProblems == this.MaxNumberOfProblems &&
                other.SchemaSearchUrlPattern == this.SchemaSearchUrlPattern &&
                ArrayEqual(this.searchPaths, other.searchPaths) &&
                ArrayEqual(this.searchGlobs, other.searchGlobs);
        }

        public bool TryGetSearchOptions(
            [NotNullWhen(true)] out IReadOnlyList<string>? paths,
            [NotNullWhen(true)] out IReadOnlyList<string>? globs,
            [NotNullWhen(true)] out Regex? urlRegex)
        {
            paths = this.searchPaths;
            globs = this.searchGlobs;

            try
            {
                urlRegex = this.SchemaSearchUrlPattern is null
                    ? null
                    : new Regex(this.SchemaSearchUrlPattern);
            }
            catch (ArgumentException)
            {
                urlRegex = null;
            }

            if (paths is null || globs is null || urlRegex is null)
            {
                return false;
            }

            return true;
        }

        public Configuration WithMaxNumberOfProblems(int value)
        {
            return new Configuration
            {
                MaxNumberOfProblems = value,
                searchGlobs = this.searchGlobs,
                searchPaths = this.searchPaths,
                SchemaSearchUrlPattern = this.SchemaSearchUrlPattern,
            };
        }

        public Configuration WithSchemaSearchPaths(params string[] value)
        {
            return new Configuration
            {
                MaxNumberOfProblems = this.MaxNumberOfProblems,
                searchGlobs = searchGlobs,
                searchPaths = value.Length == 0 ? null : value,
                SchemaSearchUrlPattern = this.SchemaSearchUrlPattern,
            };
        }

        public Configuration WithSchemaSearchGlobs(params string[] value)
        {
            return new Configuration
            {
                MaxNumberOfProblems = this.MaxNumberOfProblems,
                searchGlobs = value.Length == 0 ? null : value,
                searchPaths = this.searchPaths,
                SchemaSearchUrlPattern = this.SchemaSearchUrlPattern,
            };
        }

        public Configuration WithSchemaSearchUrlPattern(string? value)
        {
            return new Configuration
            {
                MaxNumberOfProblems = this.MaxNumberOfProblems,
                searchGlobs = this.searchGlobs,
                searchPaths = this.searchPaths,
                SchemaSearchUrlPattern = value,
            };
        }

        private static string? FromArray(IReadOnlyList<string>? value)
            => value is null ? null : string.Join(',', value);

        private static bool ArrayEqual(IReadOnlyList<string>? left, IReadOnlyList<string>? right)
        {
            if (left is null && right is null)
            {
                return true;
            }

            if (left is not null && right is not null)
            {
                return left.SequenceEqual(right);
            }

            return false;
        }

        private static IReadOnlyList<string>? ToArray(string? value)
            => value?.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
    }
}
