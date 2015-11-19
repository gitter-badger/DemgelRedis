﻿using System;
using System.Collections.Generic;
using Castle.Components.DictionaryAdapter;
using Castle.DynamicProxy;
using DemgelRedis.Common;
using DemgelRedis.ObjectManager;

namespace DemgelRedis.Extensions
{
    public static class DictionaryExtensions
    {
        public static int FullCount<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var accessor = dictionary as IProxyTargetAccessor;
            if (accessor == null) return 0;

            // Get the Common data
            var commonData = accessor.GetCommonData();
            var key = new RedisKeyObject(accessor.GetTargetPropertyInfo(), commonData.Id);
            return (int)commonData.RedisDatabase.HashLength(key.RedisKey);
        }

        public static IDictionary<TKey, TValue> FullDictionary<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var accessor = dictionary as IProxyTargetAccessor;
            if (accessor == null) return dictionary;
            // Get the Common data
            var commonData = accessor.GetCommonData();
            commonData.Processing = true;
            commonData.RedisObjectManager.RetrieveObject(dictionary, commonData.Id, commonData.RedisDatabase, accessor.GetTargetPropertyInfo());
            commonData.Processing = false;
            return dictionary;
        }

        public static LimitObject<TKey, TValue> Limit<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return new LimitObject<TKey, TValue> { LimitedObject = dictionary };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="cursor">The start point of the hash (usually the previous cursor)</param>
        /// <param name="pagesize">The maximum number of entries to return</param>
        /// <returns></returns>
        public static IDictionary<TKey, TValue> Limit<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, int cursor,
            int pagesize)
        {
            var limits = new LimitObject<TKey, TValue>
            {
                LimitedObject = dictionary,
                StartLimit = cursor,
                TakeLimit = pagesize
            };

            return limits.ExecuteLimit();
        }

        public static IDictionary<TKey, TValue> Limit<TKey, TValue>(this IDictionary<TKey, TValue> dictionary,
            params object[] limitKeys)
        {
            var limits = new LimitObject<TKey, TValue>
            {
                LimitedObject = dictionary,
                KeyLimit = limitKeys
            };

            return limits.ExecuteLimit();
        }

        public static LimitObject<TKey, TValue> Start<TKey, TValue>(this LimitObject<TKey, TValue> limits, int startCount)
        {
            limits.StartLimit = startCount;
            return limits;
        }

        

        public static LimitObject<TKey, TValue> TakeLimit<TKey, TValue>(this LimitObject<TKey, TValue> limits, int takeCount)
        {
            limits.TakeLimit = takeCount;
            return limits;
        }

        public static IDictionary<TKey, TValue> ExecuteLimit<TKey, TValue>(this LimitObject<TKey, TValue> limits)
        {
            var accessor = limits.LimitedObject as IProxyTargetAccessor;
            if (accessor == null) return limits.LimitedObject as IDictionary<TKey, TValue>;
            // Get the Common data
            var commonData = accessor.GetCommonData();
            commonData.Processing = true;
            commonData.RedisObjectManager.RetrieveObject(limits.LimitedObject, commonData.Id, commonData.RedisDatabase, accessor.GetTargetPropertyInfo(), limits);
            commonData.Processing = false;
            return limits.LimitedObject as IDictionary<TKey, TValue>;
        }
    }
}