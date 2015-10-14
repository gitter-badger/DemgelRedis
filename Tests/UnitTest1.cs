﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DemgelRedis.BackingManager;
using DemgelRedis.ObjectManager;
using Microsoft.WindowsAzure.Storage;
using NUnit.Framework;
using StackExchange.Redis;

namespace DemgelRedis.Tests
{
    [TestFixture]
    public class UnitTest1
    {
        private readonly RedisObjectManager _redis = new RedisObjectManager(new TableRedisBackup(CloudStorageAccount.DevelopmentStorageAccount));
        [Test]
        public void TestConvertToRedisHash()
        {
            //var demgelRedis = new DemgelRedis();

            var test = new TestClass
            {
                TestGuid = Guid.NewGuid(),
                TestString = "Some String..."
            };

            var ret = _redis.ConvertToRedisHash(test).ToList();

            Assert.IsTrue(ret.Count == 3);
        }

        [Test]
        public void TestRedisHashToObject()
        {
            var hashList = new List<HashEntry>
            {
                new HashEntry("TestGuid", Guid.NewGuid().ToByteArray()),
                new HashEntry("TestString", "SomeTest"),
                new HashEntry("TestInt", "123234"),
                new HashEntry("TestFloat", "76234233234.323"),
                new HashEntry("TestDouble", "32342938283982.234232")
            };

            var ret = _redis.ConvertToObject(new TestClass(), hashList.ToArray());
            ((TestClass)ret).TestFloat = 1231128128182.242342F;
            Debug.WriteLine(((TestClass)ret).TestFloat);
        }

        [Test]
        [Ignore]
        public async void TestRedisRetrieveObject()
        {
            var connection = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("REDIS"));

            var test3 = await _redis.RetrieveObjectProxy<TestConvertClassSubSuffix>("12345", connection.GetDatabase());
            Debug.WriteLine(test3.subTest.Id);

            Assert.IsTrue(test3 != null);
        }

        [Test]
        [Ignore]
        public async void TestRedisSaveObject()
        {
            var connection = ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable("REDIS"));

            //var test = connection.GetSubscriber();
            
            //test.Subscribe("__key*__:*", (redisChannel, redisValue) => Debug.WriteLine($"{redisChannel} -- {redisValue}"));

            var test3 = await _redis.RetrieveObjectProxy<TestConvertClassSubSuffix>("12347", connection.GetDatabase());
            Debug.WriteLine(test3.test);
            //var tt = test3.SomeStrings;
            test3.SomeStrings.Add("test9");
            test3.SomeStrings.Add("test1");
            test3.SomeStrings.Add("test5");
            test3.SomeStrings[2] = "something else";
            //test3.subTest;
            //e.test = "hello...Test";
            test3.test = "Hello Redis... lets see if you saved";

            // Change the value and see if it saves...
            //_redis.SaveObject(test3, test3.Id, connection.GetDatabase());
            test3.test = "This should be changed to this new value...";
        }
    }
}
