using System.Collections;
using System.Text;

namespace NAIware.Core.Collections;

/// <summary>
/// The type of bit conversion algorithm to use.
/// </summary>
public enum ConversionMethod
{
    /// <summary>Binary (base 2).</summary>
    Base2,
    /// <summary>Decimal (base 10).</summary>
    Base10,
    /// <summary>Hexadecimal (base 16).</summary>
    Base16,
}

/// <summary>
/// A class that represents a variable-length collection of bits backed by linked <see cref="BitArray"/> nodes.
/// Supports arithmetic, bitwise, and shift operations.
/// </summary>
public class BitCollection : ICollection, ICloneable, IComparable
{
    #region Inner Classes

    private sealed class BitArrayNode(int bitArraySize)
    {
        public BitArray Array { get; } = new BitArray(bitArraySize, false);
        public BitArrayNode? Previous { get; set; }
        public BitArrayNode? Next { get; set; }
        public int BeginRange { get; set; }
        public int EndRange { get; set; } = -1;
        public bool HasNext => Next is not null;
        public bool HasPrevious => Previous is not null;

        public void RefactorRanges()
        {
            if (HasPrevious)
                BeginRange = Previous!.EndRange + 1;

            EndRange = BeginRange + (Array.Count - 1);
        }
    }

    #endregion Inner Classes

    #region Member Fields

    private BitArrayNode? _LSBnode;
    private int _lsbpos;
    private BitArrayNode? _MSBnode;
    private int _msbpos;
    private int _bitsize;
    private long _nodecount;
    private readonly int _bitarraysize;
    private BitArrayNode? _lastindexrequestnode;

    #endregion Member Fields

    #region Constructors

    /// <summary>
    /// Creates a new instance from a binary string representation.
    /// </summary>
    public BitCollection(string binaryString) : this(0)
    {
        SetValue(binaryString, ConversionMethod.Base2);
    }

    /// <summary>
    /// Creates a bit collection with the specified bit size.
    /// </summary>
    public BitCollection(int bitSize) : this(bitSize, false) { }

    /// <summary>
    /// Creates a bit collection with the specified bit size and chunk size.
    /// </summary>
    public BitCollection(int bitSize, int chunkSize) : this(bitSize, false, chunkSize) { }

    /// <summary>
    /// Creates a bit collection with the specified bit size and initial state.
    /// </summary>
    public BitCollection(int bitSize, bool allBitsOn) : this(bitSize, allBitsOn, 32) { }

    /// <summary>
    /// Creates a bit collection with the specified bit size, initial state, and chunk size.
    /// </summary>
    public BitCollection(int bitSize, bool allBitsOn, int chunkSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(chunkSize);
        _bitarraysize = chunkSize;
        Resize(bitSize, allBitsOn);
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// Gets or sets the bit value at the specified index.
    /// </summary>
    public bool this[int index]
    {
        get
        {
            var node = GetNodeAtPosition(index) ?? throw new IndexOutOfRangeException();
            return ReferenceEquals(_LSBnode, node)
                ? node.Array[_lsbpos + (index - node.BeginRange)]
                : node.Array[index - node.BeginRange];
        }
        set
        {
            var node = GetNodeAtPosition(index) ?? throw new IndexOutOfRangeException();
            if (ReferenceEquals(_LSBnode, node))
                node.Array[_lsbpos + (index - node.BeginRange)] = value;
            else
                node.Array[index - node.BeginRange] = value;
        }
    }

    #endregion Properties

    #region Instance Methods

    /// <summary>Converts the internal bits to two's complement.</summary>
    public void Neg() => Neg(this);

    /// <summary>Inverts the internal bits.</summary>
    public void Not() => Not(this);

    /// <summary>Adds a 16-bit integer to the current set of bits.</summary>
    public void Add(short val) => Add(this, GetInt16Bits(val));

    /// <summary>Adds a 32-bit integer to the current set of bits.</summary>
    public void Add(int val) => Add(this, GetInt32Bits(val));

    /// <summary>Adds a 64-bit integer to the current set of bits.</summary>
    public void Add(long val) => Add(this, GetInt64Bits(val));

    /// <summary>Adds a set of bits to the current instance.</summary>
    public void Add(BitCollection bits) => Add(this, bits);

    /// <summary>ANDs a 16-bit integer with the current bits.</summary>
    public void And(short val) => And(this, GetInt16Bits(val));

    /// <summary>ANDs a 32-bit integer with the current bits.</summary>
    public void And(int val) => And(this, GetInt32Bits(val));

    /// <summary>ANDs a 64-bit integer with the current bits.</summary>
    public void And(long val) => And(this, GetInt64Bits(val));

    /// <summary>ANDs the current bits with the specified bits.</summary>
    public void And(BitCollection bits) => And(this, bits);

    /// <summary>ORs a 16-bit integer with the current bits.</summary>
    public void Or(short val) => Or(this, GetInt16Bits(val));

    /// <summary>ORs a 32-bit integer with the current bits.</summary>
    public void Or(int val) => Or(this, GetInt32Bits(val));

    /// <summary>ORs a 64-bit integer with the current bits.</summary>
    public void Or(long val) => Or(this, GetInt64Bits(val));

    /// <summary>ORs the current bits with the specified bits.</summary>
    public void Or(BitCollection bits) => Or(this, bits);

    /// <summary>Multiplies the current bits by a 16-bit integer.</summary>
    public void MultiplyBy(short x) => MultiplyBy(GetInt16Bits(x));

    /// <summary>Multiplies the current bits by the specified bits.</summary>
    public void MultiplyBy(BitCollection bits)
    {
        var product = Multiply(this, bits);
        if (product is null) return;
        if (product.Count > Count) Resize(product.Count);
        for (int i = 0; i < product.Count; i++)
            this[i] = product[i];
    }

    /// <summary>Shift the current bits to the left.</summary>
    public void ShiftLeft() => ShiftLeft(this);

    /// <summary>Shift the current bits to the right.</summary>
    public void ShiftRight() => ShiftRight(this);

    private int GetNodeCount()
    {
        var cnode = _LSBnode;
        int ncount = 0;
        while (cnode is not null) { ncount++; cnode = cnode.Next; }
        return ncount;
    }

    /// <summary>Resizes the bits to the new dimensions.</summary>
    public void Resize(int bitSize) => Resize(bitSize, false);

    /// <summary>
    /// Resizes the bits to the new dimensions.
    /// Shrinking will result in data loss.
    /// </summary>
    public void Resize(int bitSize, bool newBitsOn)
    {
        if (bitSize == _bitsize) return;
        ArgumentOutOfRangeException.ThrowIfNegative(bitSize);

        BitArrayNode? cnode;
        int targetpos = bitSize - 1;

        if (bitSize < _bitsize)
        {
            cnode = _MSBnode;
            while (cnode is not null)
            {
                if (targetpos >= cnode.BeginRange && targetpos <= cnode.EndRange)
                {
                    cnode.EndRange = targetpos;
                    if (cnode.Next is not null) cnode.Next.Previous = null;
                    cnode.Next = null;
                    _MSBnode = cnode;
                    _msbpos = targetpos - cnode.BeginRange;
                    _bitsize = bitSize;
                    _nodecount = GetNodeCount();
                    return;
                }
                cnode = cnode.Previous;
            }
        }
        else
        {
            _bitsize = bitSize;
            long ncount = _nodecount;
            cnode = _MSBnode;

            if (cnode is not null)
            {
                for (int i = _msbpos + 1; i < cnode.Array.Length; i++)
                    cnode.Array[i] = newBitsOn;

                if (targetpos < (cnode.BeginRange + _bitarraysize))
                {
                    _msbpos = targetpos - cnode.BeginRange;
                    cnode.EndRange = targetpos;
                    return;
                }
                else if (targetpos > cnode.EndRange && cnode.EndRange != (cnode.BeginRange + _bitarraysize))
                {
                    cnode.EndRange = cnode.BeginRange + _bitarraysize - 1;
                    bitSize -= (cnode.EndRange - _msbpos);
                }
            }

            while (bitSize > 0)
            {
                if (cnode is null)
                {
                    _LSBnode = new BitArrayNode(_bitarraysize);
                    cnode = _LSBnode;
                }
                else
                {
                    cnode.Next = new BitArrayNode(_bitarraysize);
                    cnode.Next.Previous = cnode;
                    cnode = cnode.Next;
                }

                ncount++;
                cnode.RefactorRanges();
                if (newBitsOn) cnode.Array.Not();

                if (bitSize > _bitarraysize)
                    bitSize -= _bitarraysize;
                else
                {
                    _msbpos = bitSize - 1;
                    _MSBnode = cnode;
                    cnode.EndRange = cnode.BeginRange + _msbpos;
                    bitSize = 0;
                }
            }

            _nodecount = ncount;
        }
    }

    /// <summary>Converts the bits to a binary string.</summary>
    public string ToBinaryString()
    {
        var sb = new StringBuilder(_bitsize);
        for (int i = _bitsize - 1; i >= 0; i--)
            sb.Append(this[i] ? '1' : '0');
        return sb.ToString();
    }

    private BitArrayNode? GetNodeAtPosition(long absolutePosition)
    {
        BitArrayNode? currnode;

        if (_lastindexrequestnode is not null &&
            absolutePosition >= _lastindexrequestnode.BeginRange &&
            absolutePosition <= _lastindexrequestnode.EndRange)
            return _lastindexrequestnode;
        else if (_lastindexrequestnode is not null && absolutePosition > _lastindexrequestnode.EndRange)
            currnode = _lastindexrequestnode.Next;
        else
            currnode = _LSBnode;

        while (currnode is not null)
        {
            if (absolutePosition >= currnode.BeginRange && absolutePosition <= currnode.EndRange)
            {
                _lastindexrequestnode = currnode;
                return currnode;
            }
            currnode = currnode.Next;
        }
        return currnode;
    }

    /// <summary>Sets the value to the specified 64-bit integer.</summary>
    public void SetValue(long val)
    {
        if (Count < 64) throw new OverflowException($"Unable to set a 64-bit integer into a bit collection of size {Count}.");
        bool negative = val < 0;
        if (negative) val *= -1;
        int bitpos = 0;

        do
        {
            long quotient = Convert.ToInt64(System.Math.Ceiling((double)(val / 2)));
            this[bitpos] = (val % 2) == 1;
            bitpos++;
            val = quotient;
        } while (val > 0);

        if (negative) _MSBnode!.Array[_msbpos] = true;
    }

    /// <summary>Sets the value to the specified 16-bit integer.</summary>
    public void SetValue(short val)
    {
        if (Count < 16) throw new OverflowException($"Unable to set a 16-bit integer into a bit collection of size {Count}.");
        bool negative = val < 0;
        if (negative) val = (short)(val * -1);
        int bitpos = 0;

        do
        {
            short quotient = Convert.ToInt16(System.Math.Ceiling((double)(val / 2)));
            this[bitpos] = (val % 2) == 1;
            bitpos++;
            val = quotient;
        } while (val > 0);

        if (negative) _MSBnode!.Array[_msbpos] = true;
    }

    /// <summary>Sets the value to the specified 32-bit integer.</summary>
    public void SetValue(int val)
    {
        if (Count < 32) throw new OverflowException($"Unable to set a 32-bit integer into a bit collection of size {Count}.");
        bool negative = val < 0;
        if (negative) val *= -1;
        int bitpos = 0;

        do
        {
            int quotient = Convert.ToInt32(System.Math.Ceiling((double)(val / 2)));
            this[bitpos] = (val % 2) == 1;
            bitpos++;
            val = quotient;
        } while (val > 0);

        if (negative) _MSBnode!.Array[_msbpos] = true;
    }

    /// <summary>Sets the value to the specified string using the given conversion method.</summary>
    public void SetValue(string stringValue, ConversionMethod conversionMethod)
    {
        if (string.IsNullOrEmpty(stringValue))
            throw new ArgumentException("Unable to parse a null or empty string.", nameof(stringValue));

        switch (conversionMethod)
        {
            case ConversionMethod.Base2:
                if (stringValue.Length > Count) Resize(stringValue.Length);
                int bitpos = 0;
                for (int i = stringValue.Length - 1; i >= 0; i--)
                    this[bitpos++] = stringValue[i] == '1';
                break;

            case ConversionMethod.Base10:
                try
                {
                    long val = Convert.ToInt64(stringValue);
                    SetValue(val);
                }
                catch
                {
                    for (int i = 0; i < stringValue.Length; i++)
                        Add(Multiply(GetInt16Bits(Convert.ToInt16(stringValue[i])), RaisePower(10, i))!);
                }
                break;
        }
    }

    #endregion Instance Methods

    #region Static Methods

    /// <summary>Shifts bits to the left by one position.</summary>
    public static void ShiftLeft(BitCollection bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        if (bits.Count == 0) throw new ArgumentException("Unable to shift an empty bit collection.", nameof(bits));

        if (bits.Count == 1) { bits[0] = false; return; }

        if (bits._lsbpos != 0)
        {
            bits._lsbpos--;
            bits._LSBnode!.EndRange++;
        }
        else
        {
            bits._LSBnode!.Previous = new BitArrayNode(bits._bitarraysize);
            bits._LSBnode.Previous.Next = bits._LSBnode;
            bits._LSBnode = bits._LSBnode.Previous;
            bits._lsbpos = bits._bitarraysize - 1;
            bits._LSBnode.BeginRange = 0;
            bits._LSBnode.EndRange = 0;
            bits._nodecount++;
        }

        var cnode = bits._LSBnode.Next;
        while (cnode is not null) { cnode.RefactorRanges(); cnode = cnode.Next; }

        bits._MSBnode!.Array[bits._msbpos] = false;
        if (bits._msbpos != 0)
        {
            bits._msbpos--;
            bits._MSBnode.EndRange = bits._MSBnode.BeginRange + bits._msbpos;
        }
        else
        {
            bits._MSBnode = bits._MSBnode.Previous;
            bits._MSBnode!.Next!.Previous = null;
            bits._MSBnode.Next = null;
            bits._msbpos = bits._bitarraysize - 1;
            bits._nodecount--;
            bits._lastindexrequestnode = null;
        }
    }

    /// <summary>Shifts bits to the right by one position.</summary>
    public static void ShiftRight(BitCollection bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        if (bits.Count == 0) throw new ArgumentException("Unable to shift an empty bit collection.", nameof(bits));

        if (bits.Count == 1) { bits[0] = false; return; }

        bits._LSBnode!.Array[bits._lsbpos] = false;
        if (bits._lsbpos < (bits._bitarraysize - 1))
        {
            bits._lsbpos++;
        }
        else
        {
            bits._LSBnode = bits._LSBnode.Next;
            bits._LSBnode!.Previous!.Next = null;
            bits._LSBnode.Previous = null;
            bits._LSBnode.BeginRange = 0;
            bits._lsbpos = 0;
            bits._nodecount--;
            bits._lastindexrequestnode = null;
        }

        bits._LSBnode.EndRange--;

        var cnode = bits._LSBnode.Next;
        while (cnode is not null) { cnode.RefactorRanges(); cnode = cnode.Next; }

        if (bits._msbpos < (bits._bitarraysize - 1))
        {
            bits._msbpos++;
        }
        else
        {
            bits._MSBnode!.Next = new BitArrayNode(bits._bitarraysize);
            bits._MSBnode.Next.Previous = bits._MSBnode;
            bits._MSBnode = bits._MSBnode.Next;
            bits._MSBnode.RefactorRanges();
            bits._msbpos = 0;
            bits._nodecount++;
        }

        bits._MSBnode!.EndRange = bits._MSBnode.BeginRange + bits._msbpos;
    }

    /// <summary>Raises a base to a specified power and returns the result as bits.</summary>
    public static BitCollection RaisePower(int baseValue, int power) =>
        RaisePower(baseValue, power, -1);

    /// <summary>Raises a base to a specified power with a target bit size.</summary>
    public static BitCollection RaisePower(int baseValue, int power, int bitSize)
    {
        int divby2 = 0;
        while ((baseValue % 2) == 0)
        {
            divby2++;
            baseValue /= 2;
        }

        var basebits = GetInt32Bits(baseValue);
        var resultbits = bitSize > 0 ? new BitCollection(bitSize) : new BitCollection(256);
        resultbits.SetValue(1);

        for (int i = 0; i < power; i++)
            resultbits.MultiplyBy(basebits);

        for (int i = 0; i < divby2; i++)
            for (int j = 0; j < power; j++)
                resultbits.ShiftLeft();

        return resultbits;
    }

    /// <summary>Multiplies two bit collections.</summary>
    public static BitCollection? Multiply(BitCollection? multicand, BitCollection? multiplier)
    {
        if (multicand is null || multiplier is null) return null;

        var product = new BitCollection(multicand.Count > multiplier.Count ? multicand.Count : multiplier.Count);

        for (int i = 0; i < multiplier.Count; i++)
        {
            if (i == 0)
            {
                if (multiplier[i]) product.Add(multicand);
            }
            else if (multiplier[i])
            {
                var multicandcopy = multicand.Clone();
                for (int j = 0; j < i; j++)
                    multicandcopy.ShiftLeft();
                product.Add(multicandcopy);
            }
        }

        return product;
    }

    /// <summary>Increments the bit collection by one.</summary>
    public static BitCollection operator ++(BitCollection anyCollection)
    {
        Add(anyCollection, GetBit(true));
        return anyCollection;
    }

    /// <summary>Gets the two's complement of a bit collection.</summary>
    public static void Neg(BitCollection bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        Not(bits);
        Add(bits, GetBit(true));
    }

    /// <summary>NOTs (inverts) a bit collection.</summary>
    public static void Not(BitCollection bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        for (int i = 0; i < bits.Count; i++)
            bits[i] = !bits[i];
    }

    /// <summary>XORs two bit collections.</summary>
    public static BitCollection XOR(BitCollection x, BitCollection y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        var newbits = new BitCollection(x.Count > y.Count ? x.Count : y.Count);
        var smallbits = x.Count > y.Count ? y : x;
        var largebits = x.Count > y.Count ? x : y;

        for (int i = 0; i < largebits.Count; i++)
        {
            newbits[i] = i < smallbits.Count
                ? smallbits[i] != largebits[i]
                : largebits[i];
        }

        return newbits;
    }

    /// <summary>ORs one bit collection with another.</summary>
    public static void Or(BitCollection bits, BitCollection orBits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        ArgumentNullException.ThrowIfNull(orBits);

        if (bits.Count < orBits.Count) bits.Resize(orBits.Count);
        var smallbits = bits.Count > orBits.Count ? orBits : bits;
        var largebits = bits.Count > orBits.Count ? bits : orBits;

        for (int i = 0; i < largebits.Count; i++)
        {
            if (i < smallbits.Count)
                bits[i] = smallbits[i] || largebits[i];
            else
            {
                if (ReferenceEquals(bits, largebits)) break;
                bits[i] = largebits[i];
            }
        }
    }

    /// <summary>ANDs one bit collection with another.</summary>
    public static void And(BitCollection bits, BitCollection andBits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        ArgumentNullException.ThrowIfNull(andBits);

        var smallbits = bits.Count > andBits.Count ? andBits : bits;
        var largebits = bits.Count > andBits.Count ? bits : andBits;

        for (int i = 0; i < smallbits.Count; i++)
        {
            if (i < smallbits.Count)
                bits[i] = smallbits[i] && largebits[i];
            else
            {
                if (!ReferenceEquals(bits, largebits)) break;
                largebits[i] = false;
            }
        }
    }

    /// <summary>Subtracts one bit collection from another.</summary>
    public static void Subtract(BitCollection bits, BitCollection subtractBits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        ArgumentNullException.ThrowIfNull(subtractBits);
        if (bits.Count != subtractBits.Count)
            throw new InvalidOperationException("Subtracting bits of different sizes can produce erroneous results. Resize one to the other.");
        Neg(subtractBits);
        Add(bits, subtractBits);
    }

    /// <summary>Adds one bit collection to another (binary addition).</summary>
    public static void Add(BitCollection bits, BitCollection addedBits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        ArgumentNullException.ThrowIfNull(addedBits);
        if (addedBits.Count > bits.Count) bits.Resize(addedBits.Count);

        var smallbits = addedBits;
        var largebits = bits;
        bool carrybit = false;
        bool sum = false;

        for (int i = 0; i < largebits.Count; i++)
        {
            if (i < smallbits.Count)
            {
                sum = smallbits[i] != largebits[i];

                if (carrybit)
                {
                    if (sum) { sum = false; carrybit = true; }
                    else { sum = true; carrybit = false; }
                }

                if (smallbits[i] && largebits[i]) carrybit = true;
            }
            else
            {
                if (ReferenceEquals(bits, largebits) && !carrybit) break;

                if (!carrybit)
                {
                    sum = largebits[i];
                }
                else
                {
                    if (largebits[i]) { sum = false; carrybit = true; }
                    else { sum = true; carrybit = false; }
                }
            }

            if (sum != bits[i]) bits[i] = sum;
        }
    }

    /// <summary>Gets a 64-bit bit collection from the specified value.</summary>
    public static BitCollection GetInt64Bits(long val)
    {
        var bits = new BitCollection(64);
        bits.SetValue(val);
        return bits;
    }

    /// <summary>Gets a 16-bit bit collection from the specified value.</summary>
    public static BitCollection GetInt16Bits(short val)
    {
        var bits = new BitCollection(16);
        bits.SetValue(val);
        return bits;
    }

    /// <summary>Gets a bit collection with a single bit value.</summary>
    public static BitCollection GetBit(bool bitOn)
    {
        var bits = new BitCollection(1);
        bits[0] = bitOn;
        return bits;
    }

    /// <summary>Gets a 32-bit bit collection from the specified value.</summary>
    public static BitCollection GetInt32Bits(int val)
    {
        var bits = new BitCollection(32);
        bits.SetValue(val);
        return bits;
    }

    #endregion Static Methods

    #region ICollection Members

    /// <inheritdoc/>
    public bool IsSynchronized => false;

    /// <summary>Gets number of bits in the bit collection.</summary>
    public int Count => _bitsize;

    void ICollection.CopyTo(Array array, int index) =>
        throw new NotImplementedException("CopyTo is not implemented.");

    /// <inheritdoc/>
    public object SyncRoot => this;

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
        for (int i = 0; i < Count; i++)
            yield return this[i];
    }

    #endregion

    #region ICloneable Members

    object ICloneable.Clone() => Clone();

    /// <summary>Creates a clone of the bit collection.</summary>
    public BitCollection Clone()
    {
        var clone = new BitCollection(Count);
        for (int i = 0; i < clone.Count; i++)
            clone[i] = this[i];
        return clone;
    }

    #endregion

    #region IComparable Members

    int IComparable.CompareTo(object? obj) =>
        obj is BitCollection bits ? CompareTo(bits) : 1;

    /// <summary>Compares two bit collections for ordering.</summary>
    public int CompareTo(BitCollection? bits)
    {
        if (bits is null) return 1;
        if (ReferenceEquals(this, bits)) return 0;

        var smallbits = bits.Count < Count ? bits : this;
        var largebits = bits.Count < Count ? this : bits;

        for (int i = largebits.Count - 1; i >= 0; i--)
        {
            if (i < smallbits.Count)
            {
                if (smallbits[i] != largebits[i])
                {
                    return smallbits[i]
                        ? (ReferenceEquals(this, smallbits) ? 1 : -1)
                        : (ReferenceEquals(this, largebits) ? 1 : -1);
                }
            }
            else
            {
                if (largebits[i])
                    return ReferenceEquals(this, largebits) ? 1 : -1;
            }
        }

        return 0;
    }

    #endregion
}
