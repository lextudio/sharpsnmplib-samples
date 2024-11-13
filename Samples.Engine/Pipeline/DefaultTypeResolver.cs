﻿using System;
using System.Reflection;

namespace Samples.Pipeline
{
    /// <summary>
    /// Default type resolver to return default type.
    /// </summary>
    public class DefaultTypeResolver : ITypeResolver
    {
        /// <inheritdoc />
        public Type Load(string assembly, string name)
        {
            // IMPORTANT: .NET standard 1.3 does not support this scenario so simply return a default type.
            return typeof(NullMessageHandler);
        }

        /// <inheritdoc />
        public Assembly[] GetAssemblies()
        {
            return Array.Empty<Assembly>();
        }
    }
}
