﻿/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 11/6/2010
 * Time: 3:26 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Net;
using Samples.Pipeline;
using Xunit;

namespace Samples.Unit.Pipeline
{
    /// <summary>
    /// Description of ListenerBindingTestFixture.
    /// </summary>
    public class ListenerTestFixture
    {
        [Fact]
        public void AddBindingDuplicate()
        {
            Assert.Equal(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21), new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            var listener = new Listener();
            listener.AddBinding(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            listener.AddBinding(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            Assert.Single(listener.Bindings);
        }
        
        [Fact]
        public void RemoveBinding()
        {
            Assert.Equal(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21), new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            var listener = new Listener();
            listener.AddBinding(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            listener.RemoveBinding(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 21));
            Assert.Empty(listener.Bindings);
        }
    }
}
