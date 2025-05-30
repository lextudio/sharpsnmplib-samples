﻿// Initially generated by MibSourceGenerator 1.0.0.0
// IMPORTANT: This file can be modified, but won't be updated by the generator again once created.
// Original file name is C:\Users\lextm\source\repos\sharpmibsuite\sharpsnmplib-samples\Samples\CSharpCore\snmpd\Mibs\SNMPv2-MIB.txt
using System;
using System.Collections.Generic;
using System.Globalization;
using Lextm.SharpSnmpLib;
using Samples.Pipeline;
using System.Linq;
using System.Net.NetworkInformation;
// using Lextm.SharpSnmpPro.Mib; // TODO: Uncomment if syntax validation is required.

namespace SNMPv2_MIB
{
    partial class sysDescr
    {
#if NET471_OR_GREATER
        private readonly OctetString _data =
            new OctetString(string.Format(CultureInfo.InvariantCulture, "#SNMP Agent on {0}", Environment.OSVersion));
#else
        private readonly OctetString _data =
            new OctetString("#SNMP Agent on .NET Standard");
#endif

        void OnCreate()
        {
            // Initialization is done via field initializer
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }
    partial class sysObjectID
    {
        private readonly ObjectIdentifier _data = new ObjectIdentifier("1.3.6.1");

        void OnCreate()
        {
            // Initialization is done via field initializer
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }
    partial class sysUpTime
    {
        void OnCreate()
        {
            // No initialization needed - dynamic calculation in getter
        }

        public override ISnmpData Data
        {
            get { return new TimeTicks((uint)Environment.TickCount / 10); }
            set { throw new AccessFailureException(); }
        }
    }
    partial class sysContact
    {
#if NET471
        private OctetString _data = new OctetString(Environment.UserName);
#else
        private OctetString _data = new OctetString("UNKNOWN");
#endif

        void OnCreate()
        {
            // Initialization is done via field initializer
        }

        public override ISnmpData Data
        {
            get
            {
                return _data;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // TODO: should we allow Null?
                if (value.TypeCode != SnmpType.OctetString)
                {
                    throw new ArgumentException("Invalid data type.", nameof(value));
                }
                if (((OctetString)value).ToString().Length > 255) //respect DisplayString syntax length limitation
                {
                    throw new ArgumentException(nameof(ErrorCode.WrongLength));
                }

                _data = (OctetString)value;
            }
        }
    }
    partial class sysName
    {
#if NET471
        private OctetString _data = new OctetString(Environment.MachineName);
#else
        private OctetString _data = new OctetString("UNKNOWN");
#endif

        void OnCreate()
        {
            // Initialization is done via field initializer
        }

        public override ISnmpData Data
        {
            get
            {
                return _data;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.TypeCode != SnmpType.OctetString)
                {
                    throw new ArgumentException("Invalid data type.", nameof(value));
                }

                _data = (OctetString)value;
            }
        }
    }
    partial class sysLocation
    {
        private OctetString _data = OctetString.Empty;

        void OnCreate()
        {
            // Initialization is done via field initializer
        }

        public override ISnmpData Data
        {
            get
            {
                return _data;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.TypeCode != SnmpType.OctetString)
                {
                    throw new ArgumentException("Invalid data type.", nameof(value));
                }

                _data = (OctetString)value;
            }
        }
    }
    partial class sysServices
    {
        private readonly Integer32 _data = new Integer32(72);

        void OnCreate()
        {
            // Initialization is done via field initializer
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }
    partial class sysORLastChange
    {
        private readonly TimeTicks _data = new TimeTicks(0);

        void OnCreate()
        {
            // Initialization is done via field initializer
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }
    partial class sysORTable
    {
        void OnCreate()
        {
            _elements.Add(new sysORIndex("1") { _data = new Integer32(1) });
            _elements.Add(new sysORIndex("2") { _data = new Integer32(2) });
            _elements.Add(new sysORID("1") { _data = new ObjectIdentifier("1.3") });
            _elements.Add(new sysORID("2") { _data = new ObjectIdentifier("1.4") });
            _elements.Add(new sysORDescr("1") { _data = new OctetString("Test1") });
            _elements.Add(new sysORDescr("2") { _data = new OctetString("Test2") });
            _elements.Add(new sysORUpTime("1") { _data = new TimeTicks(1) });
            _elements.Add(new sysORUpTime("2") { _data = new TimeTicks(2) });
        }
    }
    partial class sysORIndex
    {
        internal Integer32 _data = new Integer32(0);

        void OnCreate()
        {
            // Initialization is done in the table setup
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }
    partial class sysORID
    {
        internal ObjectIdentifier _data = new ObjectIdentifier(".0.0");

        void OnCreate()
        {
            // Initialization is done in the table setup
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }
    partial class sysORDescr
    {
        internal OctetString _data = OctetString.Empty;

        void OnCreate()
        {
            // Initialization is done in the table setup
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }
    partial class sysORUpTime
    {
        internal TimeTicks _data = new TimeTicks(0);

        void OnCreate()
        {
            // Initialization is done in the table setup
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInPkts
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInBadVersions
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInBadCommunityNames
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInBadCommunityUses
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInASNParseErrs
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpEnableAuthenTraps
    {
        private ISnmpData _data = new Integer32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            // TODO: Use ObjectRegistryBase.Verify("SNMPv2-MIB", "snmpEnableAuthenTraps", value) to validate data
            set { _data = value; }
        }
    }

    partial class snmpSilentDrops
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpProxyDrops
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpTrapOID
    {
        private ISnmpData _data = new ObjectIdentifier(".0.0"); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            // TODO: Use ObjectRegistryBase.Verify("SNMPv2-MIB", "snmpTrapOID", value) to validate data
            set { _data = value; }
        }
    }

    partial class snmpTrapEnterprise
    {
        private ISnmpData _data = new ObjectIdentifier(".0.0"); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            // TODO: Use ObjectRegistryBase.Verify("SNMPv2-MIB", "snmpTrapEnterprise", value) to validate data
            set { _data = value; }
        }
    }

    partial class snmpSetSerialNo
    {
        private ISnmpData _data = new Integer32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            // TODO: Use ObjectRegistryBase.Verify("SNMPv2-MIB", "snmpSetSerialNo", value) to validate data
            set { _data = value; }
        }
    }

    partial class snmpOutPkts
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInTooBigs
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInNoSuchNames
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInBadValues
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInReadOnlys
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInGenErrs
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInTotalReqVars
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInTotalSetVars
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInGetRequests
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInGetNexts
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInSetRequests
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInGetResponses
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpInTraps
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpOutTooBigs
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpOutNoSuchNames
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpOutBadValues
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpOutGenErrs
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpOutGetRequests
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpOutGetNexts
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpOutSetRequests
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpOutGetResponses
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }

    partial class snmpOutTraps
    {
        private ISnmpData _data = new Counter32(0); // TODO: remove initial assignment if you want to do it in constructors.

        void OnCreate()
        {
            // TODO: initialization here
        }

        public override ISnmpData Data
        {
            get { return _data; }
            set { throw new AccessFailureException(); }
        }
    }
}

namespace Lextm.SharpSnmpPro.Mib
{
    /// <summary>
    /// Registration class for SNMPv2-MIB MIB objects.
    /// </summary>
    public static partial class ModuleRegister
    {
        /// <summary>
        /// Registers all objects from this module to the specified object store.
        /// </summary>
        /// <param name="store">The object store to register objects with.</param>
        public static void RegisterSNMPv2_MIB(ObjectStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            store.Add(new SNMPv2_MIB.sysDescr());
            store.Add(new SNMPv2_MIB.sysObjectID());
            store.Add(new SNMPv2_MIB.sysUpTime());
            store.Add(new SNMPv2_MIB.sysContact());
            store.Add(new SNMPv2_MIB.sysName());
            store.Add(new SNMPv2_MIB.sysLocation());
            store.Add(new SNMPv2_MIB.sysServices());
            store.Add(new SNMPv2_MIB.sysORLastChange());
            store.Add(new SNMPv2_MIB.sysORTable());
            store.Add(new SNMPv2_MIB.snmpInPkts());
            store.Add(new SNMPv2_MIB.snmpInBadVersions());
            store.Add(new SNMPv2_MIB.snmpInBadCommunityNames());
            store.Add(new SNMPv2_MIB.snmpInBadCommunityUses());
            store.Add(new SNMPv2_MIB.snmpInASNParseErrs());
            store.Add(new SNMPv2_MIB.snmpEnableAuthenTraps());
            store.Add(new SNMPv2_MIB.snmpSilentDrops());
            store.Add(new SNMPv2_MIB.snmpProxyDrops());
            store.Add(new SNMPv2_MIB.snmpTrapOID());
            store.Add(new SNMPv2_MIB.snmpTrapEnterprise());
            store.Add(new SNMPv2_MIB.snmpSetSerialNo());
            store.Add(new SNMPv2_MIB.snmpOutPkts());
            store.Add(new SNMPv2_MIB.snmpInTooBigs());
            store.Add(new SNMPv2_MIB.snmpInNoSuchNames());
            store.Add(new SNMPv2_MIB.snmpInBadValues());
            store.Add(new SNMPv2_MIB.snmpInReadOnlys());
            store.Add(new SNMPv2_MIB.snmpInGenErrs());
            store.Add(new SNMPv2_MIB.snmpInTotalReqVars());
            store.Add(new SNMPv2_MIB.snmpInTotalSetVars());
            store.Add(new SNMPv2_MIB.snmpInGetRequests());
            store.Add(new SNMPv2_MIB.snmpInGetNexts());
            store.Add(new SNMPv2_MIB.snmpInSetRequests());
            store.Add(new SNMPv2_MIB.snmpInGetResponses());
            store.Add(new SNMPv2_MIB.snmpInTraps());
            store.Add(new SNMPv2_MIB.snmpOutTooBigs());
            store.Add(new SNMPv2_MIB.snmpOutNoSuchNames());
            store.Add(new SNMPv2_MIB.snmpOutBadValues());
            store.Add(new SNMPv2_MIB.snmpOutGenErrs());
            store.Add(new SNMPv2_MIB.snmpOutGetRequests());
            store.Add(new SNMPv2_MIB.snmpOutGetNexts());
            store.Add(new SNMPv2_MIB.snmpOutSetRequests());
            store.Add(new SNMPv2_MIB.snmpOutGetResponses());
            store.Add(new SNMPv2_MIB.snmpOutTraps());
        }
    }
}
