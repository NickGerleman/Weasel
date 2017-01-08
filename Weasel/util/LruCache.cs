using System;
using System.Collections.Generic;

namespace Rs.Util {

    /// <summary>
    /// Dictionary containing a fixed number of elements. The least recently used
    /// items will be purged from the cache if the fixed capacity is exceeded. The
    /// cache can additionally have a MaxAge set. Mappings existing longer than
    /// this age will be evicted from the cache. This functionality is disabled if
    /// MaxAge is sedt to 0.
    /// </summary>
    public class LruCache<K, V>
    {
        public int Capacity { get; set; } = 10;
        public TimeSpan MaxAge { get; set; }

        private readonly Dictionary<K, ListNode> mBackingDictionary;
        private readonly ListNode mDummyHead;
        private readonly ListNode mDummyTail;


        /// <summary>
        /// Construct an empty LruCache
        /// </summary>
        public LruCache()
        {
            this.mBackingDictionary = new Dictionary<K, ListNode>();
            mDummyHead = new ListNode();
            mDummyTail = new ListNode();
        
            mDummyHead.Next = mDummyTail;
            mDummyTail.Previous = mDummyHead;
        }


        /// <summary>
        /// Get a value corresponding to a key or set a value for a given key. The
        /// caller must first check to see if the key exists in the cache. It is
        /// guaranteed that any retrieved mapping has not expired at the time of
        /// the last check
        /// </summary>
        public V this[K key]
        {

            get
            {
                LogUsage(key);
                return mBackingDictionary[key].Value;
            }

            set
            {
                if (mBackingDictionary.ContainsKey(key))
                    mBackingDictionary[key].Unlink();

                var node = new ListNode
                {
                    Previous = mDummyHead,
                    Next = mDummyHead.Next,
                    Key = key,
                    Value = value,
                    LastUsage = DateTime.Now
                };

                mDummyHead.Next.Previous = node;
                mDummyHead.Next = node;
                mBackingDictionary[key] = node;
                DoCleanup();
            }
        }


        /// <summary>
        /// Checks whether or not a key is contained within the LruCache. This will
        /// not return true for any expired mappings.!--
        /// </summary>
        /// <param name="key">A potential key for a mapping in the cache</param>
        public bool ContainsKey(K key)
        {
            DoCleanup();
            return mBackingDictionary.ContainsKey(key);
        }


        /// <summary>
        /// Removes excess mappings if the cache is above capacity as well as any
        /// nodes that may be considered stale.
        /// </summary>
        private void DoCleanup()
        {
            while (mBackingDictionary.Count > Capacity)
            {
                var node = mDummyTail.Previous;
                node.Unlink();
                mBackingDictionary.Remove(node.Key);
            }

            var currentTime = DateTime.Now;
            if (MaxAge != TimeSpan.Zero)
            {
                var oldestNode = mDummyTail.Previous;
                while(IsStale(currentTime))
                {
                    mBackingDictionary.Remove(oldestNode.Key);
                    oldestNode.Unlink();
                }
            }
        }


        /// <summary>
        /// Whether the list contains items older than allowable by MaxAge
        /// </summary>
        /// <param name="currentTime">The current time to use as a basis</param>
        private bool IsStale(DateTime currentTime)
        {
            if (mDummyHead.Next == mDummyTail)
                return false;

            return (currentTime - mDummyTail.Previous.LastUsage) > MaxAge;
        }


        /// <summary>
        /// Logs that a given key has been used, updating its last usage time and
        /// moving it to the head of our list. It is assumed that this key exists
        /// in the cache
        /// </summary>
        /// <param name="key">The key of the pairing to log</param>
        private void LogUsage(K key)
        {
            var node = mBackingDictionary[key];
            node.LastUsage = DateTime.Now;
            node.Unlink();

            node.Next = mDummyHead.Next;
            node.Previous = mDummyHead;
            mDummyHead.Next.Previous = node;
            mDummyHead.Next = node;
        }


        /// <summary>
        /// Node in a doubly linked list containing mappings of keys to values as
        /// well as the time of last usage
        /// </summary>
        private class ListNode
        {
            public K Key { get; set; }
            public V Value { get; set; }
            public DateTime LastUsage { get; set; }
            public ListNode Previous { get; set; }
            public ListNode Next { get; set; }

            /// <summary>
            /// Unlink the node from the rest of the liked list
            /// </summary>
            public void Unlink()
            {
                Previous.Next = Next;
                Next.Previous = Previous;
            }
        }

    }

}
