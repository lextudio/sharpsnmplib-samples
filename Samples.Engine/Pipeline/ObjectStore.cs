// Object store class.
// Copyright (C) 2009-2010 Lex Li
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Lextm.SharpSnmpLib;

namespace Samples.Pipeline
{
    /// <summary>
    /// SNMP object store, who holds all implemented SNMP objects in the agent.
    /// </summary>
    public class ObjectStore
    {
        /// <summary>The internal list of objects holding the data.</summary>
        protected readonly IList<ISnmpObject> List = new List<ISnmpObject>();
        private readonly IList<Func<Variable, bool>> _missingSetHandlers = new List<Func<Variable, bool>>();

        /// <summary>
        /// Gets the object.
        /// </summary>
        /// <param name="id">The object id.</param>
        /// <returns></returns>
        public virtual ScalarObject GetObject(ObjectIdentifier id)
        {
            return List.Select(o => o.MatchGet(id)).FirstOrDefault(result => result != null);
        }

        /// <summary>
        /// Registers a callback that can materialize missing writable objects for a SET request.
        /// </summary>
        /// <param name="handler">The handler.</param>
        public virtual void RegisterMissingSetHandler(Func<Variable, bool> handler)
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            _missingSetHandlers.Add(handler);
        }

        /// <summary>
        /// Attempts to materialize a missing object for a SET request.
        /// </summary>
        /// <param name="variable">The requested variable.</param>
        /// <param name="createdObject">The created object if successful.</param>
        /// <returns><c>true</c> if the object became available.</returns>
        public virtual bool TryCreateObject(Variable variable, out ScalarObject createdObject)
        {
            createdObject = GetObject(variable.Id);
            if (createdObject != null)
            {
                return true;
            }

            foreach (var handler in _missingSetHandlers)
            {
                if (!handler(variable))
                {
                    continue;
                }

                createdObject = GetObject(variable.Id);
                if (createdObject != null)
                {
                    return true;
                }
            }

            createdObject = null;
            return false;
        }

        /// <summary>
        /// Gets the next object.
        /// </summary>
        /// <param name="id">The object id.</param>
        /// <returns></returns>
        public virtual ScalarObject GetNextObject(ObjectIdentifier id)
        {
            return List
                .Select(o => o.MatchGetNext(id))
                .Where(result => result != null)
                .OrderBy(result => result.Id)
                .FirstOrDefault();
        }

        /// <summary>
        /// Adds the specified <see cref="ISnmpObject"/>.
        /// </summary>
        /// <param name="newObject">The object.</param>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown if an object with the same ID already exists in the store.
        /// </exception>
        public virtual void Add(ISnmpObject newObject)
        {
            if (List.Any(o => o.Id == newObject.Id))
            {
                throw new System.InvalidOperationException($"An object with ID {newObject.Id} already exists in the store.");
            }
            List.Add(newObject);
        }
    }
}
