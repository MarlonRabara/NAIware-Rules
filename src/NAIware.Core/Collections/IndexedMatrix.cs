namespace NAIware.Core.Collections;

/// <summary>
/// A class that represents an indexed matrix where a value <typeparamref name="T"/> is accessible
/// via indexed positions based on row key and column key.
/// </summary>
/// <typeparam name="TRow">The type of the row key.</typeparam>
/// <typeparam name="TCol">The type of the column key.</typeparam>
/// <typeparam name="T">The value type of the matrix.</typeparam>
[Serializable]
public class IndexedMatrix<TRow, TCol, T> where TRow : notnull where TCol : notnull
{
    private sealed class KeySorter<TKey>(Dictionary<TKey, int> keyPositionTracker) : Comparer<TKey>
        where TKey : notnull
    {
        public override int Compare(TKey? x, TKey? y)
        {
            bool xExists = x is not null && keyPositionTracker.ContainsKey(x);
            bool yExists = y is not null && keyPositionTracker.ContainsKey(y);

            if (xExists && !yExists) return 1;
            if (yExists && !xExists) return -1;
            if (!xExists && !yExists) return 0;

            int xKeyPos = keyPositionTracker[x!];
            int yKeyPos = keyPositionTracker[y!];
            return xKeyPos.CompareTo(yKeyPos);
        }
    }

    private readonly T[,] _matrix;
    private readonly int _columns;
    private readonly int _rows;
    private readonly Dictionary<TRow, int> _rowPositions;
    private readonly Dictionary<TCol, int> _colPositions;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexedMatrix{TRow, TCol, T}"/> class.
    /// </summary>
    /// <param name="rows">The number of rows.</param>
    /// <param name="columns">The number of columns.</param>
    public IndexedMatrix(int rows, int columns)
    {
        _rows = rows;
        _columns = columns;
        _matrix = new T[rows, columns];
        _rowPositions = new Dictionary<TRow, int>();
        _colPositions = new Dictionary<TCol, int>();
    }

    /// <summary>
    /// Initializes the row lookups from the specified row key values.
    /// </summary>
    /// <param name="rowKeyValues">The row key values.</param>
    public void InitializeRowLookups(params TRow[] rowKeyValues)
    {
        if (rowKeyValues is null || rowKeyValues.Length == 0) return;

        for (int i = 0; i < rowKeyValues.Length; i++)
            CreateRowLookup(rowKeyValues[i], i);
    }

    /// <summary>
    /// Initializes the column lookups from the specified column key values.
    /// </summary>
    /// <param name="columnKeyValues">The column key values.</param>
    public void InitializeColumnLookups(params TCol[] columnKeyValues)
    {
        if (columnKeyValues is null || columnKeyValues.Length == 0) return;

        for (int i = 0; i < columnKeyValues.Length; i++)
            CreateColumnLookup(columnKeyValues[i], i);
    }

    /// <summary>
    /// Creates a column lookup for the specified key and position.
    /// </summary>
    public void CreateColumnLookup(TCol columnKey, int columnPosition)
    {
        if (columnPosition < 0 || columnPosition >= _columns)
            throw new IndexOutOfRangeException();

        _colPositions.Add(columnKey, columnPosition);
    }

    /// <summary>
    /// Gets whether the matrix contains a column lookup for the specified key.
    /// </summary>
    public bool ContainsColumnLookup(TCol columnKey) =>
        _colPositions.ContainsKey(columnKey);

    /// <summary>
    /// Creates a row lookup for the specified key and position.
    /// </summary>
    public void CreateRowLookup(TRow rowKey, int rowPosition)
    {
        if (rowPosition < 0 || rowPosition >= _rows)
            throw new IndexOutOfRangeException();

        _rowPositions.Add(rowKey, rowPosition);
    }

    /// <summary>
    /// Gets whether the matrix contains a row lookup for the specified key.
    /// </summary>
    public bool ContainsRowLookup(TRow rowKey) =>
        _rowPositions.ContainsKey(rowKey);

    /// <summary>
    /// Gets the underlying matrix array.
    /// </summary>
    public T[,] Matrix => _matrix;

    /// <summary>
    /// Gets the matrix values as a list of lists.
    /// </summary>
    public List<List<T>> MatrixListValues
    {
        get
        {
            var mlv = new List<List<T>>();
            if (_rows <= 0 || _columns <= 0) return mlv;

            for (int row = 0; row < _rows; row++)
            {
                var rowList = new List<T>();
                for (int col = 0; col < _columns; col++)
                    rowList.Add(this[row, col]);
                mlv.Add(rowList);
            }

            return mlv;
        }
    }

    /// <summary>
    /// Gets the column keys sorted by their position.
    /// </summary>
    public TCol[] GetColumnKeys()
    {
        var columnKeys = new List<TCol>(_colPositions.Keys);
        columnKeys.Sort(new KeySorter<TCol>(_colPositions));
        return [.. columnKeys];
    }

    /// <summary>
    /// Gets the row keys sorted by their position.
    /// </summary>
    public TRow[] GetRowKeys()
    {
        var rowKeys = new List<TRow>(_rowPositions.Keys);
        rowKeys.Sort(new KeySorter<TRow>(_rowPositions));
        return [.. rowKeys];
    }

    /// <summary>
    /// Gets or sets the value at the specified row and column key lookups.
    /// </summary>
    public T this[TRow rowLookup, TCol columnLookup]
    {
        get => _matrix[_rowPositions[rowLookup], _colPositions[columnLookup]];
        set => _matrix[_rowPositions[rowLookup], _colPositions[columnLookup]] = value;
    }

    /// <summary>
    /// Gets or sets the value at the specified row and column positions.
    /// </summary>
    public T this[int rowPosition, int columnPosition]
    {
        get => _matrix[rowPosition, columnPosition];
        set => _matrix[rowPosition, columnPosition] = value;
    }
}
