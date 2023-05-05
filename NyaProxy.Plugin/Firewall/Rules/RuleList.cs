using NyaFirewall.Rules;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Firewall.Rules
{
    /// <summary>
    /// 一个通过预分配比正常集合更大空间并且通过内部数组的索引不从0开始来做到插入和添加性能可以达到O(1)的集合，并且遍历的性能约等于直接对数组遍历
    /// </summary>
    public class RuleList<T> : IEnumerable<T> where T : Rule
    {

        /*
         * 我本来的想法是参考硬盘的机制，在删除操作时仅仅把数组中的那个成员标记为已移除，并不真的去做移除操作。
         * 枚举器内可以直接过滤掉被移除的数组成员，但到了索引器就出现问题了
         * 我最开始的设想是如果索引器的index是已被移除的成员，那么就找下一个成员，如果下一个还是已被移除的那么就继续下一个直到找到一个未被移除的
         * 但后面发现这样子有严重的逻辑问题。
         * 0 1 2 3 4（index）
         * 0 r 2 r 9（value）
         * 这种情况下如果我索引填1，那么会正常的到所有2，因为1已被于移除
         * 但如果我填写2，他还会索引到2，但他应该索引到4。
         * 解决该问题我需要编写出坐标转换的算法，但我智商实在是太差了，暂时想不到怎么解决，因此暂时放弃通过这种方式来移除数组。
         * 这边简单描述一下两种坐标，下面第一行是外部在使用索引器时使用的坐标，第二行是实际上需要操作的数组。因此如果我外面使用2这个坐标，实际上需要指向5，我暂时无法思考出来如何做到这种转换。
         * 0 2 9    （被删除后的样子）
         * 0 r 2 r 9（真实的数组样子）
         */

        [MaybeNull]
        public virtual T this[int index]
        {
            get
            {
                if (index > _offset)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the data array.");
                return _rules[_start + index];
            }
            set
            {
                if (index > _offset)
                    throw new IndexOutOfRangeException("Index was outside the bounds of the data array.");
                _version++;
                _rules[_start + index] = value; 
            }
        }


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

                Array.Copy(_rules, _start, newRules, start, _count);
                _split = start + (_split - _start);
                _start = start;
                _rules = newRules;
            }
        }

        public virtual int Count => _count;

        private int _start; //起始坐标
        private int _split; //起始和尾部区域的分界线
        private int _offset; //尾部坐标
        private int _count; //当前长度
        private int _version;
        private T[] _rules;
        private const int DEFAULT_CAPACITY = 128;

        public RuleList()
        {
            _start = DEFAULT_CAPACITY / 2;
            _split = _start;
            _offset = 0;
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
            Array.Copy(_rules, _start + index + 1, _rules, _start + index, _count - 1);
            _count--;
            _offset--;
        }

        public void Clear()
        {
            _start = _rules.Length / 2;
            _split = _start;
            _offset = 0;
            _version++;
        }

        public Span<T> AsSpan()
        {
            return _rules.AsSpan(_start, _count);
        }

        public Memory<T> AsMemory()
        {
            return _rules.AsMemory(_start, _count);
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

                T item = _rules[i]; //这边直接的枚举器内就把不可用的去掉，反正只是内部使用
                if (item != null && !item.Disabled && item.IsEffective)
                    yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
