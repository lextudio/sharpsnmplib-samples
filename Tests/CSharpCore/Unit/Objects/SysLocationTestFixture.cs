using System;
using Lextm.SharpSnmpLib;
using Samples.Objects;
using Xunit;

namespace Samples.Unit.Objects
{
    public class SysLocationTestFixture
    {
        [Fact]
        public void Test()
        {
            var sys = new SysLocation();
            Assert.Throws<ArgumentNullException>(() => sys.Data = null);
            Assert.Throws<ArgumentException>(() => sys.Data = new TimeTicks(0));
            sys.Data = OctetString.Empty;
            Assert.Equal(OctetString.Empty, sys.Data);
        }
    }
}
