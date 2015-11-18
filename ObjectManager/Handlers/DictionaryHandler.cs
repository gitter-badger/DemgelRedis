﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using DemgelRedis.Common;
using DemgelRedis.Extensions;
using DemgelRedis.Interfaces;
using DemgelRedis.ObjectManager.Attributes;
using DemgelRedis.ObjectManager.Proxy;
using DemgelRedis.ObjectManager.Proxy.DictionaryInterceptor;
using DemgelRedis.ObjectManager.Proxy.Selectors;
using StackExchange.Redis;

namespace DemgelRedis.ObjectManager.Handlers
{
    public class DictionaryHandler : RedisHandler
    {
        private readonly DictionarySelector _dictionarySelector;

        public DictionaryHandler(RedisObjectManager demgelRedis) : base(demgelRedis)
        {
            _dictionarySelector = new DictionarySelector();
        }

        /// <summary>
        /// Need to pass in the Proxy object of the list/enumerable
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool CanHandle(object obj)
        {
            var targetObject = GetTarget(obj);
            return targetObject is IDictionary;
        }

        public override object Read<T>(object obj, Type objType, IDatabase redisDatabase, string id, PropertyInfo basePropertyInfo, LimitObject<T> limits = null)
        {
            var hashKey = new RedisKeyObject(basePropertyInfo, id);
            RedisObjectManager.RedisBackup?.RestoreHash(redisDatabase, hashKey);

            var targetType = GetTarget(obj).GetType();
            Type keyType = null;
            Type itemType = null;

            if (targetType.GetInterfaces().Any(interfaceType => interfaceType.IsGenericType &&
                      interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                if (targetType.GetGenericArguments().Any())
                {
                    keyType = targetType.GetGenericArguments()[0];
                    itemType = targetType.GetGenericArguments()[1];
                }
            }

            var method = objType.GetMethod("Add", new []{keyType, itemType});

            if (itemType != null && itemType.GetInterfaces().Contains(typeof(IRedisObject)))
            {
                // TODO this all needs to be changed to handle IRedisObjects in a Dictionary, shouldn't be to hard
                
                var retlist = redisDatabase.HashGetAll(hashKey.RedisKey);
                foreach (var ret in retlist)
                {
                    // We need to check to make sure the object exists (Overhead... it happens)
                    if (!redisDatabase.KeyExists((string) ret.Value))
                    {
                        redisDatabase.HashDelete(hashKey.RedisKey, ret.Name);
                        continue;
                    }

                    var newObj = Activator.CreateInstance(itemType);
                    var newProxy = RedisObjectManager.RetrieveObjectProxy(itemType, id, redisDatabase, newObj);
                    //var redisKeyProp = itemType.GetProperties().SingleOrDefault(x => x.GetCustomAttributes().Any(y => y is RedisIdKey));

                    //if (redisKeyProp != null)
                    //{
                    //    // Parse the key...
                    //    var keyindex1 = ((string)ret.Value).IndexOf(":", StringComparison.Ordinal);
                    //    var stringPart1 = ((string)ret.Value).Substring(keyindex1 + 1);
                    //    var keyindex2 = stringPart1.IndexOf(":", StringComparison.Ordinal);
                    //    var key = keyindex2 > 0 ? stringPart1.Substring(keyindex2) : stringPart1;

                    //    if (redisKeyProp.PropertyType == typeof (string))
                    //    {
                    //        redisKeyProp.SetValue(newProxy, key);
                    //    }
                    //    else
                    //    {
                    //        redisKeyProp.SetValue(newProxy,
                    //            Guid.Parse(key));
                    //    }
                    //}
                    method.Invoke(obj, new[] { (string) ret.Name, newProxy });
                }
                return obj;
            }

            if (itemType != typeof (RedisValue))
            {
                // Try to process each entry as a proxy, or fail
                throw new InvalidCastException($"Use RedisValue instead of {itemType?.Name}.");
            }

            var retList = redisDatabase.HashGetAll(hashKey.RedisKey);
            foreach (var ret in retList)
            {
                method.Invoke(obj, new[] {(string)ret.Name, (object)ret.Value});
            }
            return obj;
        }

        public override bool Save(object obj, Type objType, IDatabase redisDatabase, string id, PropertyInfo basePropertyInfo = null)
        {
            Debug.WriteLine("Save was called on this object...");
            return true;
        }

        public override bool Delete(object obj, Type objType, IDatabase redisDatabase, string id, PropertyInfo basePropertyInfo = null)
        {
            var hashKey = new RedisKeyObject(basePropertyInfo, id);

            redisDatabase.KeyDelete(hashKey.RedisKey);

            return true;
        }

        public override object BuildProxy(ProxyGenerator generator, Type objType, CommonData data, object baseObj)
        {
            if (!objType.IsInterface)
            {
                throw new Exception("Dictionary can only be created from IDictionary Interface");
            }

            object proxy;

            if (baseObj == null)
            {
                proxy = generator.CreateInterfaceProxyWithoutTarget(objType,
                    new ProxyGenerationOptions(new GeneralProxyGenerationHook())
                    {
                        Selector = _dictionarySelector
                    },
                    new GeneralGetInterceptor(data), new DictionaryAddInterceptor(data), new DictionarySetInterceptor(data), new DictionaryRemoveInterceptor(data));
            }
            else
            {
                proxy = generator.CreateInterfaceProxyWithTarget(objType, baseObj,
                    new ProxyGenerationOptions(new GeneralProxyGenerationHook())
                    {
                        Selector = _dictionarySelector
                    },
                    new GeneralGetInterceptor(data), new DictionaryAddInterceptor(data), new DictionarySetInterceptor(data), new DictionaryRemoveInterceptor(data));
            }
            return proxy;
        }
    }
}