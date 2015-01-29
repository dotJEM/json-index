using System;
using DotJEM.Json.Index.Schema;
using Lucene.Net.Store;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Index.Test.Unit.Storage
{
    /// <summary>
    /// <p/>Base class for Locking implementation.  <see cref="T:Lucene.Net.Store.Directory"/> uses
    ///              instances of this class to implement locking.<p/><p/>Note that there are some useful tools to verify that
    ///              your LockFactory is working correctly: <see cref="T:Lucene.Net.Store.VerifyingLockFactory"/>
    ///             , <see cref="T:Lucene.Net.Store.LockStressTest"/>, <see cref="T:Lucene.Net.Store.LockVerifyServer"/>
    ///             .<p/>
    /// </summary>
    /// <seealso cref="T:Lucene.Net.Store.LockVerifyServer"/><seealso cref="T:Lucene.Net.Store.LockStressTest"/><seealso cref="T:Lucene.Net.Store.VerifyingLockFactory"/>
    
    [TestFixture]
    public class CashedLocksTests
    {
        [Test]
        public void Test()
        {
            
            //VerifyingLockFactory verifying = new VerifyingLockFactory();


            //var enumerator = new JObjectEnumerator();

            //var json = JObject.Parse("{ simple: 'test', complex: { child: 42 }, array: [ 'str', 'str2', { ups: 45 } ] }");


            //foreach (JNode node in enumerator.Enumerate(json, "ship"))
            //{
            //    Console.WriteLine(node.Path + " = " + node.Type);
            //}




        }

    }
}
