using Lextm.SharpSnmpLib;
using Samples.Objects;
using Samples.Pipeline;
using Xunit;

namespace Samples.Unit.Objects
{
    public class SysDescrTestFixture
    {
        [Fact]
        public void Test()
        {
            var sys = new SysDescr();
            Assert.Throws<AccessFailureException>(() => sys.Data = OctetString.Empty);
        }
    }
}
