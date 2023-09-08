using System;
using System.Collections;
using System.Collections.Generic;

namespace Fracture.MyTools
{
    /// <summary>
    /// 有序哈希表，按照插入顺序存储元素，同时支持快速查询
    /// </summary>
    /// <typeparam name="T">元素类型</typeparam>
    public class OrderedHash<T> : IEnumerable<T> where T : IEquatable<T>
    {
        private const int Capacity = 100;
        private List<T> _list = new();
        private List<int>[] _hash = new List<int>[Capacity];
        
        private uint Hash(T item)
        {
            return (uint)item.GetHashCode() % Capacity;
        }
        
        public OrderedHash()
        {
            for (var i = 0; i < Capacity; i++)
            {
                _hash[i] = new List<int>();
            }
        }
        
        public T this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }
        
        public int Count => _list.Count;
        
        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 查询元素是否存在
        /// </summary>
        /// <param name="item">需要查询的元素</param>
        /// <returns>是否存在</returns>
        public bool Contains(T item)
        {
            var index = Hash(item);
            foreach (var i in _hash[index])
            {
                if (_list[i].Equals(item))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 查询元素索引
        /// </summary>
        /// <param name="item">需要查询的元素</param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            var index = Hash(item);
            foreach (var i in _hash[index])
            {
                if (_list[i].Equals(item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 添加元素
        /// </summary>
        /// <param name="item">需要添加的元素</param>
        /// <returns>是否添加成功</returns>
        public bool Add(T item)
        {
            var index = Hash(item);
            foreach (var i in _hash[index])
            {
                if (_list[i].Equals(item))
                {
                    return false;
                }
            }
            
            _hash[index].Add(_list.Count);
            _list.Add(item);
            return true;
        }
        
        /// <summary>
        /// 删除元素
        /// </summary>
        /// <param name="item">需要删除的元素</param>
        /// <returns>是否成功删除</returns>
        public bool Remove(T item)
        {
            var index = Hash(item);
            foreach (var i in _hash[index])
            {
                if (_list[i].Equals(item))
                {
                    _hash[index].Remove(i);
                    _list.RemoveAt(i);
                    
                    // 更新哈希表
                    foreach (var list in _hash)
                    {
                        for (var j = 0; j < list.Count; j++)
                        {
                            if (list[j] > i)
                            {
                                list[j]--;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 删除指定索引的元素
        /// </summary>
        /// <param name="index">需要删除的元素索引</param>
        /// <returns>是否成功删除</returns>
        public bool RemoveAt(int index)
        {
            if (index < 0 || index >= _list.Count)
            {
                return false;
            }
            
            var item = _list[index];
            var hashIndex = Hash(item);
            foreach (var i in _hash[hashIndex])
            {
                if (i == index)
                {
                    _hash[hashIndex].Remove(i);
                    _list.RemoveAt(i);
                    
                    // 更新哈希表
                    foreach (var list in _hash)
                    {
                        for (var j = 0; j < list.Count; j++)
                        {
                            if (list[j] > i)
                            {
                                list[j]--;
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 清除
        /// </summary>
        public void Clear()
        {
            _list.Clear();
            for (var i = 0; i < Capacity; i++)
            {
                _hash[i].Clear();
            }
        }

        /// <summary>
        /// 转换为列表
        /// </summary>
        /// <returns>列表</returns>
        public List<T> ToList()
        {
            return _list;
        }
        
        /// <summary>
        /// 转换为数组
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            return _list.ToArray();
        }
    }
}


