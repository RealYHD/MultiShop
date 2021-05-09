using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameServiceWarden.Core.Collection
{
    public class LRUCache<K, V> : IEnumerable<V>
    {
        private class Node
        {
            public K key;
            public V value;
            public Node front;
            public Node back;
        }

        public int Size {get { return size; } }
        public int Count {get { return valueDictionary.Count; } }
        private readonly int size;
        private Node top;
        private Node bottom;
        private Dictionary<K, Node> valueDictionary;
        private Action<V> cleanupAction;

        public LRUCache(int size = 100, Action<V> cleanup = null)
        {
            this.size = size;
            valueDictionary = new Dictionary<K, Node>(size);
            this.cleanupAction = cleanup;
        }

        private void MoveToTop(K key) {
            Node node = valueDictionary[key];
            if (node != null && top != node) {
                node.front.back = node.back;
                node.back = top;
                node.front = null;
                top = node;
            }
        }

        private Node AddToTop(K key, V value) {
            Node node = new Node();
            node.front = null;
            node.back = top;
            node.value = value;
            node.key = key;
            top = node;
            if (bottom == null) {
                bottom = node;
            } else if (valueDictionary.Count == Size) {
                valueDictionary.Remove(bottom.key);
                cleanupAction?.Invoke(bottom.value);
                bottom = bottom.front;
            }
            valueDictionary[key] = node;
            return node;
        }

        public V Use(K key, Func<V> generate) {
            if (generate == null) throw new ArgumentNullException("generate");
            Node value = null;
            if (valueDictionary.ContainsKey(key)) {
                value = valueDictionary[key];
                MoveToTop(key);
            } else {
                value = AddToTop(key, generate());
            }
            return value.value;
        }

        public async Task<V> UseAsync(K key, Func<Task<V>> generate) {
            if (generate == null) throw new ArgumentNullException("generate");
            Node value = null;
            if (valueDictionary.ContainsKey(key)) {
                value = valueDictionary[key];
                MoveToTop(key);
            } else {
                value = AddToTop(key, await generate());
            }
            return value.value;
        }

        public bool IsCached(K key) {
            return valueDictionary.ContainsKey(key);
        }

        public void Clear() {
            top = null;
            bottom = null;
            valueDictionary.Clear();
        }

        public IEnumerator<V> GetEnumerator()
        {
            foreach (Node node in valueDictionary.Values)
            {
                yield return node.value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}