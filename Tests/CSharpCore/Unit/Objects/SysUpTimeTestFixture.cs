using Lextm.SharpSnmpLib;
using Samples.Objects;
using Samples.Pipeline;
using Xunit;

namespace Samples.Unit.Objects
{
    public class SysUpTimeTestFixture
    {
        [Fact]
        public void Test()
        {
            var sys = new SysUpTime();
            Assert.Throws<AccessFailureException>(() => sys.Data = OctetString.Empty);
        }
    }
}
