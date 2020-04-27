/*
 * Created by SharpDevelop.
 * User: Lex
 * Date: 8/4/2012
 * Time: 9:32 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using Lextm.SharpSnmpLib.Pipeline;
using System;
using System.Linq;
using Xunit;

namespace Lextm.SharpSnmpLib.Unit.Pipeline
{
    /// <summary>
    /// Description of EngineGroupTestFixture.
    /// </summary>
    public class EngineGroupTestFixture
    {

        [Fact]
        public void TestIsInTime()
        {
            Assert.True(EngineGroup.IsInTime(new[] { 0, 0 }, 0, -499));
            Assert.False(EngineGroup.IsInTime(new[] { 0, 0 }, 0, -150001));

            Assert.True(EngineGroup.IsInTime(new[] { 0, int.MinValue + 1, }, 0, int.MaxValue - 1));
            Assert.False(EngineGroup.IsInTime(new[] { 0, int.MinValue + 150002 }, 0, int.MaxValue));
        }

        [Fact]
        public void InstantiateWithoutArguments_Should_ShowDefaultBehavior()
        {
            var eg = new EngineGroup();
            Assert.True(eg.EngineId.GetRaw().SequenceEqual(EngineGroup.EngineIdDefault.GetRaw()));
            Assert.True(eg.ContextName.Equals(OctetString.Empty));
            Assert.Equal(0, eg.EngineTimeData[0]);
        }

        [Fact]
        public void InstantiateWithEngineBootsOnly_Should_UseEngineBootsButDefaultEngineId()
        {
            var eg = new EngineGroup(42);
            Assert.True(eg.EngineId.GetRaw().SequenceEqual(EngineGroup.EngineIdDefault.GetRaw()));
            Assert.Equal(OctetString.Empty, eg.ContextName);
            Assert.Equal(42, eg.EngineTimeData[0]);
        }

        [Fact]
        public void InstantiateWithNegativeEngineBoots_Should_ThrowArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => { var eg = new EngineGroup(-1); });
        }

        [Fact]
        public void InstantiateWithEngineIdAndEngineBoots_Should_UseValues()
        {
            var engineId = new byte[] { 0x80, 0x00, 0x1f, 0x88, 0x80, 0xaf, 0xbc, 0x29, 0x10, 0xfc, 0x64, 0x12, 0x56, 0x00, 0x00, 0x00, 0x00 };
            var eg = new EngineGroup(new OctetString(engineId), new OctetString("TheContext"), 42);
            Assert.True(eg.EngineId.GetRaw().SequenceEqual(engineId));
            Assert.Equal(new OctetString("TheContext"), eg.ContextName);
            Assert.Equal(42, eg.EngineTimeData[0]);
        }

        [Fact]
        public void InstantiateWithTooShortEngineId_Should_ThrowArgumentException()
        {
            var engineId = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            Assert.Throws<ArgumentException>(() => { new EngineGroup(new OctetString(engineId), OctetString.Empty, 0); });
        }

        [Fact]
        public void InstantiateWithTooLongEngineId_Should_ThrowArgumentException()
        {
            var engineId = new byte[] {
                0x80, 0x00, 0x1f, 0x88, 0x80, 0xaf, 0xbc, 0x29,
                0x10, 0xfc, 0x64, 0x12, 0x56, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00 };
            Assert.Throws<ArgumentException>(() => { new EngineGroup(new OctetString(engineId), OctetString.Empty, 0); });
        }

        [Fact]
        public void InstantiateWithEngineIdNull_Should_ThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => { new EngineGroup(null, OctetString.Empty, 0); });
        }

        [Fact]
        public void InstantiateWithEngineIdNotBeginningWithOneBit_Should_ThrowArgumentException()
        {
            var engineId = new byte[] { 0x00, 0x00, 0x1f, 0x88, 0x80, 0xaf, 0xbc, 0x29, 0x10, 0xfc, 0x64, 0x12, 0x56, 0x00, 0x00, 0x00, 0x00 };
            Assert.Throws<ArgumentException>(() => { new EngineGroup(new OctetString(engineId), OctetString.Empty, 0); });
        }

        [Fact]
        public void InstantiateWithContextNameNull_Should_ThrowArgumentNullException()
        {
            var engineId = new byte[] { 0x80, 0x00, 0x1f, 0x88, 0x80, 0xaf, 0xbc, 0x29, 0x10, 0xfc, 0x64, 0x12, 0x56, 0x00, 0x00, 0x00, 0x00 };
            Assert.Throws<ArgumentNullException>(() => { new EngineGroup(new OctetString(engineId), null, 0); });
        }

    }
}
