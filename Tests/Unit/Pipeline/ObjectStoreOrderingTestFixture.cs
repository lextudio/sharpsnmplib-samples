using System.Collections.Generic;
using Lextm.SharpSnmpLib;
using Samples.Pipeline;
using Xunit;

namespace Samples.Unit.Pipeline
{
    public class ObjectStoreOrderingTestFixture
    {
        [Fact]
        public void GetNextObjectChoosesLexicographicallySmallestMatch()
        {
            var store = new ObjectStore();
            store.Add(new TestScalar("1.3.6.1.2.1.4.23.0"));
            store.Add(new TestScalar("1.3.6.1.2.1.4.20.1.1.127.0.0.1"));

            var next = store.GetNextObject(new ObjectIdentifier("1.3.6.1.2.1.4.20"));

            Assert.NotNull(next);
            Assert.Equal(new ObjectIdentifier("1.3.6.1.2.1.4.20.1.1.127.0.0.1"), next.Variable.Id);
        }

        [Fact]
        public void TableObjectChoosesLexicographicallySmallestRowMatch()
        {
            var table = new TestTable(
                new TestScalar("1.3.6.1.2.1.4.20.1.2.127.0.0.1"),
                new TestScalar("1.3.6.1.2.1.4.20.1.1.127.0.0.1"));

            var next = table.MatchGetNext(new ObjectIdentifier("1.3.6.1.2.1.4.20"));

            Assert.NotNull(next);
            Assert.Equal(new ObjectIdentifier("1.3.6.1.2.1.4.20.1.1.127.0.0.1"), next.Variable.Id);
        }

        private sealed class TestScalar : ScalarObject
        {
            public TestScalar(string oid)
                : base(new ObjectIdentifier(oid))
            {
            }

            public override ISnmpData Data
            {
                get { return new Integer32(0); }
                set { }
            }
        }

        private sealed class TestTable : TableObject
        {
            private readonly IReadOnlyList<ScalarObject> _objects;

            public TestTable(params ScalarObject[] objects)
                : base(new ObjectIdentifier("1.3.6.1.2.1.4.20"))
            {
                _objects = objects;
            }

            protected override IEnumerable<ScalarObject> Objects
            {
                get { return _objects; }
            }
        }
    }
}
