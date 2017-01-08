using System;
using System.Threading;
using Rs.Util;
using Xunit;

namespace Rs.Test
{

    public class LruCacheTest
    {
        private LruCache<string, string> mStringCache;
        const int Capacity = 5;

        public LruCacheTest()
        {
            mStringCache = new LruCache<string, string> { Capacity = Capacity };
        }


        [Fact]
        public void ConstructDefaults()
        {
            Assert.Equal(expected: Capacity, actual: mStringCache.Capacity);
            Assert.Equal(expected: TimeSpan.Zero, actual: mStringCache.MaxAge);
        }


        [Fact]
        public void AddElement()
        {
            Assert.False(mStringCache.ContainsKey("key"));
            mStringCache["key"] = "value";
            Assert.True(mStringCache.ContainsKey("key"));
            Assert.Equal(expected: "value", actual: mStringCache["key"]);
        }


        [Fact]
        public void ModifyElement()
        {
            mStringCache["key"] = "value";
            Assert.True(mStringCache.ContainsKey("key"));
            Assert.Equal(expected: "value", actual: mStringCache["key"]);
        
            mStringCache["key"] = "value2";
            Assert.True(mStringCache.ContainsKey("key"));
            Assert.Equal(expected: "value2", actual: mStringCache["key"]);
        }


        [Fact]
        public void AddMultiple()
        {
            mStringCache["key"] = "value";
            Assert.True(mStringCache.ContainsKey("key"));
            Assert.Equal(expected: "value", actual: mStringCache["key"]);
        
            mStringCache["key2"] = "value2";
            Assert.True(mStringCache.ContainsKey("key2"));
            Assert.Equal(expected: "value2", actual: mStringCache["key2"]);

            Assert.True(mStringCache.ContainsKey("key"));
            Assert.Equal(expected: "value", actual: mStringCache["key"]);
        }


        [Fact]
        public void AddPastCapacity()
        {
            for (int i = 0; i <= Capacity; i++)
            {
                mStringCache[$"key{i}"] = $"value{i}";
            }

            Assert.False(mStringCache.ContainsKey("key0"));

            for (int i = 1; i <= Capacity; i++)
            {
                Assert.True(mStringCache.ContainsKey($"key{i}"));
            Assert.Equal(expected: $"value{i}", actual: mStringCache[$"key{i}"]);
            }
        }


        [Fact]
        public void AddPastCapacityAfterGet()
        {
            for (int i = 0; i < Capacity; i++)
            {
                mStringCache[$"key{i}"] = $"value{i}";
            }

            string value = mStringCache["key0"];
            mStringCache[$"key{Capacity}"] = $"value{Capacity}";
            Assert.True(mStringCache.ContainsKey($"key{Capacity}"));
            Assert.Equal(expected: $"value{Capacity}", actual: mStringCache[$"key{Capacity}"]);

            Assert.True(mStringCache.ContainsKey("key0"));
            Assert.Equal(expected: "value0", actual: mStringCache["key0"]);
            Assert.False(mStringCache.ContainsKey("key1"));
        }


        [Fact]
        public void AddPastCapacityAfterModify()
        {
            for (int i = 0; i < Capacity; i++)
            {
                mStringCache[$"key{i}"] = $"value{i}";
            }

            mStringCache["key0"] = "new";
            mStringCache[$"key{Capacity}"] = $"value{Capacity}";
            Assert.True(mStringCache.ContainsKey($"key{Capacity}"));
            Assert.Equal(expected: $"value{Capacity}", actual: mStringCache[$"key{Capacity}"]);

            Assert.True(mStringCache.ContainsKey("key0"));
            Assert.Equal(expected: "new", actual: mStringCache["key0"]);
            Assert.False(mStringCache.ContainsKey("key1"));
        }


        [Fact]
        public void MaxAgeExpire()
        {
            mStringCache.MaxAge = TimeSpan.FromMilliseconds(1);

            mStringCache["key"] = "value";
            Assert.True(mStringCache.ContainsKey("key"));
            Assert.Equal(expected: "value", actual: mStringCache["key"]);
            Assert.False(mStringCache.ContainsKey("key1"));

            Thread.Sleep(2);

            Assert.False(mStringCache.ContainsKey("key"));
        }


        [Fact]
        public void MaxAgeRetain()
        {
            mStringCache.MaxAge = TimeSpan.FromHours(1);

            mStringCache["key"] = "value";
            Assert.True(mStringCache.ContainsKey("key"));
            Assert.Equal(expected: "value", actual: mStringCache["key"]);
            Assert.False(mStringCache.ContainsKey("key1"));

            Thread.Sleep(2);

            Assert.True(mStringCache.ContainsKey("key"));
        }

    }

}
