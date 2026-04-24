using System.Collections;
using System.Data;
using System.Data.Common;

namespace Deadpool.Infrastructure.Tests.Helpers;

// ── FakeDbConnection ──��───────────────────────────────────────────────────────

/// <summary>
/// In-memory DbConnection that captures every SQL command Dapper sends.
/// Never connects to a real SQL Server — safe for pure unit tests.
/// </summary>
internal sealed class FakeDbConnection : DbConnection
{
    public List<FakeCommandRecord> ExecutedCommands { get; } = new();

    private string _cs = "fake";
    public override string ConnectionString  { get => _cs; set => _cs = value; }
    public override string Database          => "FakeDb";
    public override string DataSource        => "FakeServer";
    public override string ServerVersion     => "0.0";
    public override ConnectionState State    => ConnectionState.Open;

    public override void ChangeDatabase(string databaseName) { }
    public override void Close()  { }
    public override void Open()   { }
    public override Task OpenAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        throw new NotSupportedException();

    protected override DbCommand CreateDbCommand() => new FakeDbCommand(this);
}

/// <summary>Immutable record of a captured Dapper command.</summary>
internal sealed record FakeCommandRecord(
    string CommandText,
    Dictionary<string, object?> Parameters);

// ── FakeDbCommand ─────────────────────────────────────────────────────────────

internal sealed class FakeDbCommand : DbCommand
{
    private readonly FakeDbConnection _owner;
    private readonly FakeDbParameterCollection _params = new();

    public FakeDbCommand(FakeDbConnection owner) => _owner = owner;

    public override string          CommandText       { get; set; } = string.Empty;
    public override int             CommandTimeout    { get; set; }
    public override CommandType     CommandType       { get; set; }
    public override bool            DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource  { get; set; }

    protected override DbConnection?         DbConnection          { get; set; }
    protected override DbParameterCollection DbParameterCollection => _params;
    protected override DbTransaction?        DbTransaction         { get; set; }

    public override void Cancel()  { }
    public override void Prepare() { }

    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    public override int ExecuteNonQuery()
    {
        Capture();
        return 1;
    }

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        Capture();
        return Task.FromResult(1);
    }

    public override object? ExecuteScalar() => null;

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        throw new NotSupportedException();

    private void Capture()
    {
        // Strip Dapper's "@" prefix so tests can assert with bare key names e.g. "FilePath"
        var parameters = _params
            .Cast<FakeDbParameter>()
            .ToDictionary(
                p => p.ParameterName.TrimStart('@'),
                p => p.Value);

        _owner.ExecutedCommands.Add(new FakeCommandRecord(CommandText, parameters));
    }
}

// ── FakeDbParameter ───────────────────────────────────────────────────────────

internal sealed class FakeDbParameter : DbParameter
{
    public override DbType             DbType                  { get; set; }
    public override ParameterDirection Direction               { get; set; }
    public override bool               IsNullable              { get; set; }
    public override string             ParameterName           { get; set; } = string.Empty;
    public override int                Size                    { get; set; }
    public override string             SourceColumn            { get; set; } = string.Empty;
    public override bool               SourceColumnNullMapping { get; set; }
    public override object?            Value                   { get; set; }
    public override void ResetDbType() { }
}

// ── FakeDbParameterCollection ─────────────────────────────────────────────────

internal sealed class FakeDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _list = new();

    public override int    Count    => _list.Count;
    public override object SyncRoot => this;

    public override int  Add(object value)              { _list.Add((DbParameter)value); return _list.Count - 1; }
    public override void AddRange(Array values)         { foreach (var v in values) Add(v); }
    public override void Clear()                        => _list.Clear();
    public override bool Contains(object value)         => _list.Contains((DbParameter)value);
    public override bool Contains(string value)         => _list.Any(p => p.ParameterName == value);
    public override void CopyTo(Array array, int index) => throw new NotSupportedException();
    public override IEnumerator GetEnumerator()         => _list.GetEnumerator();
    public override int  IndexOf(object value)          => _list.IndexOf((DbParameter)value);
    public override int  IndexOf(string parameterName)  => _list.FindIndex(p => p.ParameterName == parameterName);
    public override void Insert(int index, object value)       => _list.Insert(index, (DbParameter)value);
    public override void Remove(object value)                  => _list.Remove((DbParameter)value);
    public override void RemoveAt(int index)                   => _list.RemoveAt(index);
    public override void RemoveAt(string parameterName)        => _list.RemoveAll(p => p.ParameterName == parameterName);

    protected override DbParameter GetParameter(int index)             => _list[index];
    protected override DbParameter GetParameter(string parameterName)  => _list.First(p => p.ParameterName == parameterName);
    protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var idx = IndexOf(parameterName);
        if (idx >= 0) _list[idx] = value;
    }
}

