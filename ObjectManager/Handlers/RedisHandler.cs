﻿using System;
using System.Reflection;
using Demgel.Redis.Interfaces;
using StackExchange.Redis;

namespace DemgelRedis.ObjectManager.Handlers
{
    public abstract class RedisHandler : IRedisHandler
    {
        protected readonly global::DemgelRedis.ObjectManager.DemgelRedis _demgelRedis;

        protected RedisHandler(global::DemgelRedis.ObjectManager.DemgelRedis demgelRedis)
        {
            _demgelRedis = demgelRedis;
        }

        public abstract bool CanHandle(object obj);
        public abstract object Read(object obj, Type objType, IDatabase redisDatabase, string id, PropertyInfo basePropertyInfo = null);
        public abstract bool Save(object obj, Type objType, IDatabase redisDatabase, string id, PropertyInfo basePropertyInfo = null);
    }
}