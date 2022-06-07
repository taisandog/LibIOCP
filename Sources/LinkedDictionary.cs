using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibIOCP
{
    /// <summary>
    /// 保存了活跃度的Dictionary
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="K"></typeparam>
    public class LinkedDictionary<T, K> : IDictionary<T, K>
    {
        /// <summary>
        /// 存储数据的字典
        /// </summary>
        private IDictionary<T, LinkedListNode<KeyValuePair<T, K>>> _dic = null;
        /// <summary>
        /// 存储数据的字典
        /// </summary>
        private LinkedList<KeyValuePair<T, K>> _lk = null;
        /// <summary>
        /// get值时候是否触发
        /// </summary>
        private bool _isGetToUpdate;
        /// <summary>
        /// 保存了活跃度的Dictionary
        /// </summary>
        /// <param name="dic">托管的字典</param>
        /// <param name="isGetToUpdate">Get值时候是否要更新活跃度</param>
        public LinkedDictionary(IDictionary<T, LinkedListNode<KeyValuePair<T, K>>> dic, bool isGetToUpdate = true)
        {
            _dic = dic;
            _lk = new LinkedList<KeyValuePair<T, K>>();
            _isGetToUpdate = isGetToUpdate;
        }
        /// <summary>
        /// 保存了活跃度的Dictionary
        /// </summary>
        public LinkedDictionary(bool isGetToUpdate = true) : this(new Dictionary<T, LinkedListNode<KeyValuePair<T, K>>>(),isGetToUpdate)
        {
        }



        /// <summary>
        /// 存取值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public K this[T key]
        {
            get
            {
                LinkedListNode<KeyValuePair<T, K>> node = _dic[key];
                if (_isGetToUpdate)
                {
                    MoveToLast(node);
                }
                return node.Value.Value;
            }
            set
            {
                LinkedListNode<KeyValuePair<T, K>> node = null;
                if (_dic.TryGetValue(key, out node))
                {
                    node.Value = new KeyValuePair<T, K>(key, value);

                    MoveToLast(node);
                }
                else
                {
                    node = _lk.AddLast(new KeyValuePair<T, K>(key, value));
                    _dic[key] = node;
                }

            }
        }

        /// <summary>
        /// 把节点移动到最新
        /// </summary>
        /// <param name="node"></param>
        private void MoveToLast(LinkedListNode<KeyValuePair<T, K>> node)
        {
            _lk.Remove(node);
            _lk.AddLast(node);
        }



        public ICollection<T> Keys
        {
            get
            {

                return _dic.Keys;
            }
        }

        public ICollection<K> Values
        {
            get
            {
                List<K> lst = new List<K>(_dic.Count);
                foreach (KeyValuePair<T, LinkedListNode<KeyValuePair<T, K>>> kvp in _dic)
                {
                    lst.Add(kvp.Value.Value.Value);
                }
                return lst;
            }
        }

        public int Count
        {
            get
            {
                return _dic.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return _dic.IsReadOnly;
            }
        }

        /// <summary>
        /// get值时候是否触发更新
        /// </summary>
        public bool IsGetToUpdate
        {
            get { return _isGetToUpdate; }
            set { _isGetToUpdate = value; }
        }

        /// <summary>
        /// 活跃度信息
        /// </summary>
        public LinkedList<KeyValuePair<T, K>> TimeInfos
        {
            get { return _lk; }
        }
        /// <summary>
        /// 添加一个带有所提供的键和值的元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(T key, K value)
        {
            LinkedListNode<KeyValuePair<T, K>> node = new LinkedListNode<KeyValuePair<T, K>>(new KeyValuePair<T, K>(key, value));

            _dic.Add(key, node);
            _lk.AddLast(node);
        }
        /// <summary>
        /// 添加一个带有所提供的键和值的元素
        /// </summary>
        /// <param name="item">项</param>
        public void Add(KeyValuePair<T, K> item)
        {
            LinkedListNode<KeyValuePair<T, K>> node = new LinkedListNode<KeyValuePair<T, K>>(item);

            _dic.Add(item.Key, node);
            _lk.AddLast(node);
        }

        /// <summary>
        /// 清空所有
        /// </summary>
        public void Clear()
        {
            _dic.Clear();
            _lk.Clear();
        }

        /// <summary>
        /// 是否包含此项
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<T, K> item)
        {
            return ContainsKey(item.Key);
        }
        /// <summary>
        ///  是否包含此键
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(T key)
        {
            LinkedListNode<KeyValuePair<T, K>> ret = null;

            if (_dic.TryGetValue(key, out ret))
            {
                if (_isGetToUpdate)
                {
                    MoveToLast(ret);
                }
                return true;
            }

            return false;
        }
        /// <summary>
        /// 把值复制到数组
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<T, K>[] array, int arrayIndex)
        {
            int curIndex = arrayIndex;
            foreach (KeyValuePair<T, LinkedListNode<KeyValuePair<T, K>>> kvp in _dic)
            {
                if (curIndex >= array.Length)
                {
                    break;
                }
                KeyValuePair<T, K> retKvp = new KeyValuePair<T, K>(kvp.Key, kvp.Value.Value.Value);
                array[curIndex] = retKvp;
                curIndex++;
            }


        }
        /// <summary>
        /// 获取枚举
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<T, K>> GetEnumerator()
        {
            return new LinkedDictionaryEnumerator<T, K>(_dic.GetEnumerator());
        }

        /// <summary>
        /// 删除键
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(T key)
        {
            LinkedListNode<KeyValuePair<T, K>> ret = null;
            bool isRemove = false;
            if (_dic.TryGetValue(key, out ret))
            {
                isRemove = _dic.Remove(key);
                _lk.Remove(ret);
            }
            
            return isRemove;
        }
        /// <summary>
        /// 删除键并返回值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public K RemoveKey(T key)
        {
            LinkedListNode<KeyValuePair<T, K>> retNode = null;
            bool isRemove = false;
            if (_dic.TryGetValue(key, out retNode))
            {
                isRemove = _dic.Remove(key);
                _lk.Remove(retNode);
            }
            if (!isRemove || retNode == null )
            {
                return default( K);
            }
            K ret = retNode.Value.Value;
            retNode = null;
            return ret;
        }
        /// <summary>
        /// 删除项
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(LinkedListNode<KeyValuePair<T, K>> item)
        {
            if(item == null) 
            {
                return true;
            }
            bool isRemove = _dic.Remove(item.Value.Key);
            _lk.Remove(item);
            return isRemove;

        }
        /// <summary>
        /// 删除项
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<T, K> item)
        {
            
            bool isRemove = _dic.Remove(item.Key);
            _lk.Remove(item);
            return isRemove;

        }
        /// <summary>
        /// 获取与指定键关联的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(T key, out K value)
        {
            LinkedListNode<KeyValuePair<T, K>> ret = null;

            if (_dic.TryGetValue(key, out ret))
            {
                if (_isGetToUpdate)
                {
                    MoveToLast(ret);
                }
                value = ret.Value.Value;
                return true;
            }
            value = default(K);
            return false;
        }
        /// <summary>
        /// 获取枚举器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new LinkedDictionaryEnumerator<T, K>(_dic.GetEnumerator());
        }
        /// <summary>
        ///  按最低活跃度开始裁剪元素集合
        /// </summary>
        /// <param name="count">保留个数</param>
        public void TrimCount(int count)
        {
            DateTime dt = DateTime.Now;
            LinkedListNode<KeyValuePair<T, K>> curNode = null;
            while (_dic.Count > count)
            {
                curNode = _lk.First;
                if (curNode == null)
                {
                    break;
                }
                if (!Remove(curNode))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// 最老的节点
        /// </summary>
        public LinkedListNode<KeyValuePair<T, K>> OldestNode 
        {
            get 
            {
                return _lk.First;
            }
        }

        /// <summary>
        /// 最新的节点
        /// </summary>
        public LinkedListNode<KeyValuePair<T, K>> LatestNode
        {
            get
            {
                return _lk.Last;
            }
        }
    }



    /// <summary>
    /// LRU字典的枚举
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="K"></typeparam>
    public class LinkedDictionaryEnumerator<T, K> : IEnumerator<KeyValuePair<T, K>>
    {
        private IEnumerator<KeyValuePair<T, LinkedListNode<KeyValuePair<T, K>>>> _enumTk;
        /// <summary>
        /// LRU字典的枚举
        /// </summary>
        /// <param name="enumTk">枚举器</param>
        public LinkedDictionaryEnumerator(IEnumerator<KeyValuePair<T, LinkedListNode<KeyValuePair<T, K>>>> enumTk)
        {
            _enumTk = enumTk;
        }
        public KeyValuePair<T, K> Current
        {
            get
            {
                return new KeyValuePair<T, K>(_enumTk.Current.Key, _enumTk.Current.Value.Value.Value);
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return new KeyValuePair<T, K>(_enumTk.Current.Key, _enumTk.Current.Value.Value.Value);
            }
        }

        public void Dispose()
        {
            _enumTk.Dispose();
        }

        public bool MoveNext()
        {
            return _enumTk.MoveNext();
        }

        public void Reset()
        {
            _enumTk.Reset();
        }
    }
}
