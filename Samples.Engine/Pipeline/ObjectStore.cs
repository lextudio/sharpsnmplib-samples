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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        /// Gets the next object.
        /// </summary>
        /// <param name="id">The object id.</param>
        /// <returns></returns>
        public virtual ScalarObject GetNextObject(ObjectIdentifier id)
        {
            return List.Select(o => o.MatchGetNext(id)).FirstOrDefault(result => result != null);
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

        public void LoadData(string dataFile)
        {
            if (!File.Exists(dataFile))
            {
                Console.WriteLine($"Warning: Data file '{dataFile}' not found. Skipping data loading.");
                return;
            }

            try
            {
                var lines = File.ReadAllLines(dataFile);
                var loadedCount = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Split('|');
                    if (parts.Length != 3)
                        continue;

                    var oidString = parts[0].Trim();
                    var typeString = parts[1].Trim();
                    var valueString = parts[2].Trim();

                    try
                    {
                        var oid = new ObjectIdentifier(oidString);
                        var data = ParseSnmpData(typeString, valueString);

                        if (data != null)
                        {
                            SetObjectData(oid, data);
                            loadedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to parse line '{line}': {ex.Message}");
                    }
                }

                Console.WriteLine($"Successfully loaded {loadedCount} SNMP values from '{dataFile}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data file '{dataFile}': {ex.Message}");
            }
        }

        private ISnmpData ParseSnmpData(string typeString, string valueString)
        {
            switch (typeString)
            {
                case "2": // Integer32
                    if (int.TryParse(valueString, out var intValue))
                        return new Integer32(intValue);
                    break;

                case "4": // OctetString (ASCII)
                    return new OctetString(valueString);

                case "4x": // OctetString (Hex)
                    try
                    {
                        var bytes = new byte[valueString.Length / 2];
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bytes[i] = Convert.ToByte(valueString.Substring(i * 2, 2), 16);
                        }
                        return new OctetString(bytes);
                    }
                    catch
                    {
                        return new OctetString(valueString);
                    }

                case "6": // ObjectIdentifier
                    try
                    {
                        return new ObjectIdentifier(valueString);
                    }
                    catch
                    {
                        return new ObjectIdentifier("0.0");
                    }

                case "64x": // IpAddress (Hex)
                    try
                    {
                        var bytes = new byte[valueString.Length / 2];
                        for (int i = 0; i < bytes.Length; i++)
                        {
                            bytes[i] = Convert.ToByte(valueString.Substring(i * 2, 2), 16);
                        }
                        return new IP(bytes);
                    }
                    catch
                    {
                        return new IP("127.0.0.1");
                    }

                case "65": // Counter32
                    if (uint.TryParse(valueString, out var counterValue))
                        return new Counter32(counterValue);
                    break;

                case "66": // Gauge32
                    if (uint.TryParse(valueString, out var gaugeValue))
                        return new Gauge32(gaugeValue);
                    break;

                case "67": // TimeTicks
                    if (uint.TryParse(valueString, out var ticksValue))
                        return new TimeTicks(ticksValue);
                    break;

                case "70": // Counter64
                    if (ulong.TryParse(valueString, out var counter64Value))
                        return new Counter64(counter64Value);
                    break;

                default:
                    Console.WriteLine($"Warning: Unknown SNMP type '{typeString}' for value '{valueString}'");
                    break;
            }

            return null;
        }

        private void SetObjectData(ObjectIdentifier oid, ISnmpData data)
        {
            // Find the object that matches this OID
            var obj = GetObject(oid);
            if (obj != null)
            {
                try
                {
                    obj.CheckAccess = false;
                    obj.Data = data;
                    obj.CheckAccess = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to set data for OID {oid}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Warning: No object found for OID {oid}. Cannot set data.");
            }
        }
    }
}
