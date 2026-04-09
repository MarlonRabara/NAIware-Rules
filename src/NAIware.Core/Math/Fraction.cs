namespace NAIware.Core.Math;

/// <summary>
/// Represents a mathematical fraction with arithmetic operations and operator overloads.
/// </summary>
public class Fraction : ICloneable, IFraction
{
    private uint _num;
    private uint _den;
    private bool _isneg;
    private decimal _val;

    /// <summary>Creates a fraction from numerator and denominator.</summary>
    public Fraction(uint numerator, uint denominator) : this(numerator, denominator, false) { }

    /// <summary>Creates a fraction from numerator, denominator, and sign.</summary>
    public Fraction(uint numerator, uint denominator, bool isNegative)
    {
        _num = numerator;
        _den = denominator;
        _isneg = isNegative;
        RecalculateValue();
    }

    /// <summary>Creates a fraction from a decimal value.</summary>
    public Fraction(decimal val) { Value = val; }

    /// <summary>Creates a fraction from a double value.</summary>
    public Fraction(double val) : this(System.Convert.ToDecimal(val)) { }

    /// <summary>Creates a fraction from a long value.</summary>
    public Fraction(long val) : this(System.Convert.ToDecimal(val)) { }

    /// <summary>Creates a fraction from an int value.</summary>
    public Fraction(int val) : this(System.Convert.ToDecimal(val)) { }

    /// <summary>Creates a fraction from a uint value.</summary>
    public Fraction(uint val) : this(System.Convert.ToDecimal(val)) { }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{(_isneg ? "-" : string.Empty)}{_num}/{_den}";

    /// <summary>Reduces the fraction to its simplest form.</summary>
    public void Reduce()
    {
        if (_den == 1) return;

        if (_num != 0)
        {
            uint common = MathHelper.GCF(_num, _den);
            if (common == 1) return;
            _num /= common;
            _den /= common;
        }

        RecalculateValue();
    }

    private void RecalculateValue()
    {
        _val = (decimal)_num / (decimal)_den;
        if (_isneg) _val *= -1;
    }

    /// <summary>Gets or sets the decimal value of the fraction.</summary>
    public decimal Value
    {
        get => _val;
        set
        {
            _isneg = value < 0;
            value = System.Math.Abs(value);
            string fullval = value.ToString();
            int decindex = fullval.IndexOf('.');
            if (decindex < 0)
            {
                _num = System.Convert.ToUInt32(value);
                _den = 1;
            }
            else
            {
                decimal wholenum = System.Convert.ToDecimal(fullval[..decindex]);
                string decimalpart = fullval[(decindex + 1)..];
                int decimaldigitstokeep = uint.MaxValue.ToString().Length - 3;

                if (decimalpart.Length < uint.MaxValue.ToString().Length)
                    decimaldigitstokeep = decimalpart.Length;

                _den = 1;
                for (int i = 0; i < decimaldigitstokeep; i++)
                    _den *= 10;

                _num = System.Convert.ToUInt32(decimalpart[..decimaldigitstokeep]) + System.Convert.ToUInt32(wholenum * _den);
            }

            RecalculateValue();
            Reduce();
        }
    }

    /// <summary>Gets or sets whether the fraction is negative.</summary>
    public bool IsNegative
    {
        get => _isneg;
        set { _isneg = value; RecalculateValue(); }
    }

    /// <summary>Gets or sets the numerator.</summary>
    public uint Numerator
    {
        get => _num;
        set { _num = value; RecalculateValue(); }
    }

    /// <summary>Gets or sets the denominator.</summary>
    public uint Denominator
    {
        get => _den;
        set
        {
            if (value == 0) throw new DivideByZeroException("The denominator of a fraction cannot be zero.");
            _den = value;
            RecalculateValue();
        }
    }

    #region Operator Overloads

    public static Fraction? operator +(Fraction? x, Fraction? y)
    {
        if (x is null || y is null) return null;
        var xcopy = x.Clone();
        xcopy.Add(y);
        return xcopy;
    }

    public static Fraction? operator -(Fraction? x, Fraction? y)
    {
        if (x is null || y is null) return null;
        var xcopy = x.Clone();
        xcopy.Subtract(y);
        return xcopy;
    }

    public static Fraction? operator /(Fraction? x, Fraction? y)
    {
        if (x is null || y is null) return null;
        var xcopy = x.Clone();
        xcopy.Divide(y);
        return xcopy;
    }

    public static Fraction? operator *(Fraction? x, Fraction? y)
    {
        if (x is null || y is null) return null;
        var xcopy = x.Clone();
        xcopy.Multiply(y);
        return xcopy;
    }

    public static Fraction? operator *(Fraction? x, decimal y)
    {
        if (x is null) return null;
        var xcopy = x.Clone();
        xcopy.MultiplyByDecimal(y);
        return xcopy;
    }

    public static Fraction? operator !(Fraction? x)
    {
        if (x is null) return null;
        var xcopy = x.Clone();
        xcopy.IsNegative = !xcopy.IsNegative;
        return xcopy;
    }

    #endregion

    #region ICloneable

    object ICloneable.Clone() => Clone();

    /// <summary>Creates a shallow copy of this fraction.</summary>
    public Fraction Clone() => (MemberwiseClone() as Fraction)!;

    #endregion

    #region IComparable

    int IComparable.CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is IFraction frac) return CompareTo(frac);
        if (obj.GetType() == typeof(decimal)) return CompareTo(System.Convert.ToDecimal(obj));
        return -1;
    }

    /// <summary>Compares this fraction to another.</summary>
    public int CompareTo(IFraction? frac)
    {
        if (frac is null) return 1;
        return _val.CompareTo(frac.Value);
    }

    /// <summary>Compares this fraction to a decimal value.</summary>
    public int CompareTo(decimal anyNumber) => _val.CompareTo(anyNumber);

    #endregion

    #region IFraction

    void IFraction.Add(IFraction fraction) => Add(fraction as Fraction);
    void IFraction.Subtract(IFraction fraction) => Subtract(fraction as Fraction);
    void IFraction.Multiply(IFraction fraction) => Multiply(fraction as Fraction);
    void IFraction.Divide(IFraction fraction) => Divide(fraction as Fraction);

    private void Add(Fraction? fraction)
    {
        if (fraction is null) return;
        uint lcm = MathHelper.LCM(_den, fraction._den);
        int newnum = (int)(((IsNegative ? -1 : 1) * _num * (lcm / _den)) +
                           ((fraction.IsNegative ? -1 : 1) * fraction._num * (lcm / fraction._den)));
        IsNegative = newnum < 0;
        Numerator = (uint)System.Math.Abs(newnum);
        Denominator = lcm;
    }

    private void Subtract(Fraction? fraction)
    {
        if (fraction is null) return;
        uint lcm = MathHelper.LCM(_den, fraction._den);
        int newnum = (int)(((IsNegative ? -1 : 1) * _num * (lcm / _den)) -
                           ((fraction.IsNegative ? -1 : 1) * fraction._num * (lcm / fraction._den)));
        IsNegative = newnum < 0;
        Numerator = (uint)System.Math.Abs(newnum);
        Denominator = lcm;
    }

    private void Multiply(Fraction? fraction)
    {
        if (fraction is null) return;
        IsNegative = IsNegative != fraction.IsNegative;
        Numerator = _num * fraction._num;
        Denominator = _den * fraction._den;
    }

    private void MultiplyByDecimal(decimal dec) => Value *= dec;

    private void Divide(Fraction? fraction)
    {
        if (fraction is null) return;
        IsNegative = IsNegative != fraction.IsNegative;
        Numerator = _num * fraction._den;
        Denominator = _den * fraction._num;
    }

    #endregion
}
