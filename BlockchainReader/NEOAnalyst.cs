using Neo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainReader
{
    public class IndexPair : IEquatable<IndexPair>
    {
        public UInt256 hash;
        public UInt16 index;

        public IndexPair(UInt256 h, UInt16 idx)
        {
            hash = h;
            index = idx;
        }

        public bool Equals(IndexPair other)
        {
            return this.hash.Equals(other.hash) && this.index.Equals(other.index);
        }

        public override int GetHashCode()
        {
            int hashCode = this.hash.GetHashCode() ^ this.index.GetHashCode();
            return hashCode;
        }
    }

    public class AddressPair : IEquatable<AddressPair>
    {
        public Fixed8 value;
        public UInt160 address;

        public AddressPair(Fixed8 v, UInt160 addr)
        {
            value = v;
            address = addr;
        }

        public bool Equals(AddressPair other)
        {
            return this.value.Equals(other.value) && this.address.Equals(other.address);
        }
    }

    interface INeoAnalyst
    {
        void Add(UInt256 hash, UInt16 index, Fixed8 value, UInt160 address);
        AddressPair Remove(UInt256 hash, UInt16 index);
        AddressPair Peek(UInt256 hash, UInt16 index);
        IEnumerable<AddressPair> AsEnumerable();
    }

    class NeoMemAnalyst : INeoAnalyst
    {
        Dictionary<IndexPair, AddressPair> entries = new Dictionary<IndexPair, AddressPair>();

        public void Add(UInt256 hash, UInt16 index, Fixed8 value, UInt160 address)
        {
            IndexPair idxPair = new IndexPair(hash, index);
            AddressPair addPair = new AddressPair(value, address);
            entries.Add(idxPair, addPair);
        }

        public AddressPair Remove(UInt256 hash, UInt16 index)
        {
            IndexPair idxPair = new IndexPair(hash, index);
            AddressPair result = entries.ContainsKey(idxPair) ? entries[idxPair] : null;
            entries.Remove(idxPair);
            return result;
        }

        public AddressPair Peek(UInt256 hash, UInt16 index)
        {
            IndexPair idxPair = new IndexPair(hash, index);
            AddressPair result = entries.ContainsKey(idxPair) ? entries[idxPair] : null;
            return result;
        }

        public IEnumerable<AddressPair> AsEnumerable()
        {
            return entries.Values.AsEnumerable<AddressPair>();
        }
    }
}
