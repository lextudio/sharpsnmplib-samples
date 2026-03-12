// ifAdminStatusclass.
// Copyright (C) 2013 Lex Li
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

/*
 * Created by SharpDevelop.
 * User: Lex
 * Date: 3/3/2013
 * Time: 11:15 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Net.NetworkInformation;
using System.Collections.Concurrent;
using System;
using Lextm.SharpSnmpLib;
using Samples.Pipeline;

namespace Samples.Objects
{
    /// <summary>
    /// ifAdminStatus object.
    /// </summary>
    internal sealed class IfAdminStatus : ScalarObject
    {
        private static readonly ConcurrentDictionary<string, int> Values = new(StringComparer.Ordinal);
        private readonly NetworkInterface _networkInterface;
        private readonly string _interfaceKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="IfAdminStatus"/> class.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="networkInterface">The network interface.</param>
        public IfAdminStatus(int index, NetworkInterface networkInterface)
            : base("1.3.6.1.2.1.2.2.1.7.{0}", index.ToString())
        {
            _networkInterface = networkInterface;
            _interfaceKey = string.IsNullOrWhiteSpace(networkInterface.Id)
                ? index.ToString()
                : networkInterface.Id;
        }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        /// <exception cref="AccessFailureException"></exception>
        public override ISnmpData Data
        {
            get { return new Integer32(Values.GetOrAdd(_interfaceKey, _ => GetDefaultAdminStatus(_networkInterface.OperationalStatus))); }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.TypeCode != SnmpType.Integer32)
                {
                    throw new ArgumentException("Invalid data type.", nameof(value));
                }

                var requested = ((Integer32)value).ToInt32();
                if (requested is < 1 or > 3)
                {
                    throw new ArgumentException(nameof(ErrorCode.WrongValue));
                }

                Values[_interfaceKey] = requested;
            }
        }

        private static int GetDefaultAdminStatus(OperationalStatus status)
        {
            return status switch
            {
                OperationalStatus.Up => 1,
                OperationalStatus.Testing => 3,
                _ => 2,
            };
        }
    }
}
