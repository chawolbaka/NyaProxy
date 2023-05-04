using NyaFirewall.Rules;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Firewall.Rules
{

    /// <summary>
    /// 一个通过预分配比正常集合更大空间并且通过内部数组的索引不从0开始来做到插入、添加、删除性能可以达到O(1)的集合，并且遍历的性能约等于直接对数组遍历
    /// <para/>
    /// (删除操作部分抄袭了硬盘的机制，不在操作时真的去删除，仅仅是标记为已移除。虽然这样做到了非常快的删除，但这会导致在扩容的性能很低，因为会在扩容时会删除已被删除的成员暂时不能直接复制内存)
    /// </summary>
    public class RuleList<T> : IEnumerable<T> where T : Rule
    {
        public virtual int Count => _count;

        public virtual int Capacity
        {
            get { return _rules.Length; }
            set
            {
                if (value < _count)
                    throw new ArgumentOutOfRangeException(nameof(Capacity), "Capacity was less than the current size.");
                else if (value == _count)
                    return;

                T[] newRules = new T[value];

                //一般来说插入规则的情况会比加在末位更多，使用这边刻意的给插入留出更多的空间
                int start = newRules.Length - _count - (int)((newRules.Length - _count) / 2.4);
                _version++;
                
                if (_hasRemove)
                {
                    CompressRules(AsSpan(), newRules.AsSpan(start));
                    _split = start + (_split - _start);
                    _start = start;
                    _hasRemove = false;
                }
                else
                {
                    AsSpan().CopyTo(newRules.AsSpan(start, _count));
                    _split = start + (_split - _start);
                    _start = start;
                }
                _rules = newRules;
            }
        }

        [MaybeNull]
        public virtual T this[int index]
        {
            get
            {
                if (index > _offset)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the data array.");
                
                _version++;
                while (index < _rules.Length)
                {
                    T rule = _rules[_start + index++];
                    if (rule == null || !rule.IsRemoved)
                        return rule;
                }
                throw new OverflowException();
            }
            set
            {
                if (index > _offset)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the data array.");
             
                _version++;
                while (index < _count)
                {
                    T rule = _rules[_start + index++];
                    if (rule == null || !rule.IsRemoved)
                    {
                        _rules[_start + index - 1] = value;
                        return;
                    }

                }
                throw new OverflowException();
            }
        }

        private int _start; //起始坐标
        private int _split; //起始和尾部区域的分界线
        private int _offset; //尾部坐标
        private int _count; //当前长度
        private bool _hasRemove; //是否有元素被移除
        private int _version;
        private T[] _rules;
        private const int DEFAULT_CAPACITY = 128;

        public RuleList()
        {
            _start = DEFAULT_CAPACITY / 2;
            _split = _start;
            _offset = 0;
            _hasRemove = true;
            _rules = new T[DEFAULT_CAPACITY];
        }

        public void AddFirst(T item)
        {
            TryGrowFirst(1);
            _count++;
            _rules[--_start] = item;
        }

        public void AddLast(T item)
        {
            TryGrowLast(1);
            _count++;
            _rules[_split + _offset++] = item;
        }

        public bool Remove(T item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            int version = _version;
            for (int i = 0; i < _count; i++)
            {
                if (version != _version)
                    throw new InvalidOperationException("data was modified, enumeration operation may not execute");
                if (item.IsRemoved)
                    continue;

                if (item is IEquatable<T> eq && eq.Equals(this[i]) || item.Equals(this[i]))
                {
                    Remove(i);
                    return true;
                }
            }

            return false;
        }

        public void Remove(int index)
        {
            this[index].IsRemoved = true;
            _hasRemove = true;
        }

        public void Clear()
        {
            _start = _rules.Length / 2;
            _split = _start;
            _offset = 0;
            _hasRemove = false;
            _version++;
        }

        public Span<T> AsSpan()
        {
            if (_hasRemove)
                CompressRules();

            return _rules.AsSpan(_start, _count);
        }

        public Memory<T> AsMemory()
        {
            if (_hasRemove)
                CompressRules();

            return _rules.AsMemory(_start, _count);
        }

        
        private void CompressRules()
        {
            T[] oldRules = _rules;
            T[] newRules = new T[oldRules.Length];

            int start = newRules.Length - _count - (int)((newRules.Length - _count) / 2);
            int remove = CompressRules(oldRules.AsSpan(_start, _count), newRules.AsSpan(start));
            _count -= remove;
            _offset -= remove;
            _split = start + (_split - _start);
            _start = start;
            _rules = newRules;
        }

        /// <summary>
        /// 将input中未被remove的元素移动到output中
        /// </summary>
        /// <returns>被移除的元素数量</returns>
        private int CompressRules(Span<T> input, Span<T> output)
        {
            int offset = 0, remove = 0;
            for (int i = 0; i < input.Length; i++)
            {
                T item = input[i];
                if (item != null && !item.IsRemoved)
                    output[offset++] = item;
                else
                    remove++;
            }
            return remove;
        }

        private void TryGrowFirst(int length)
        {
            if (_start - length < 0)
                Capacity *= 2;
        }

        private void TryGrowLast(int length)
        {
            if (_offset + length > _rules.Length - _start)
                Capacity *= 2;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int version = _version;
            for (int i = _start; i < _count; i++)
            {
                if (version != _version)
                    throw new InvalidOperationException("data was modified, enumeration operation may not execute");

                if (!_rules[i].IsRemoved && !_rules[i].Disabled && _rules[i].IsEffective)
                    yield return _rules[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
