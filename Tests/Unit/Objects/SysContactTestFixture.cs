using System;
using Lextm.SharpSnmpLib;
using Samples.Objects;
using Xunit;

namespace Samples.Unit.Objects
{
    public class SysContactTestFixture
    {
        [Fact]
        public void Test()
        {
            var sys = new SysContact();
            Assert.Throws<ArgumentNullException>(() => sys.Data = null);
            Assert.Throws<ArgumentException>(() => sys.Data = new TimeTicks(0));
        }
    }
}
