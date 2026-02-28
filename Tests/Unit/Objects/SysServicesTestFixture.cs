using Lextm.SharpSnmpLib;
using Samples.Objects;
using Samples.Pipeline;
using Xunit;

namespace Samples.Unit.Objects
{
    public class SysServicesTestFixture
    {
        [Fact]
        public void Test()
        {
            var sys = new SysServices();
            Assert.Equal(new Integer32(72), sys.Data);
            Assert.Throws<AccessFailureException>(() => sys.Data = new TimeTicks(0));
        }
    }
}
