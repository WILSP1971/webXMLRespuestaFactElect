using System.Data;

namespace webXMLRespuestaFactElect.Tests;

/// <summary>
/// Doble de prueba minimo de <see cref="IDataRecord"/> que simula una fila de
/// resultado de un Stored Procedure, sin necesidad de una conexion ni base de datos
/// real (CHECKPOINT C7). Solo implementa los miembros usados por los mapeadores de
/// Services/GetLogWebServiceQuery.cs y Services/CatalogosQuery.cs; el resto lanza
/// NotSupportedException para dejar explicito que no se usan en estas pruebas.
/// </summary>
public sealed class FakeDataRecord : IDataRecord
{
    private readonly List<(string Nombre, object? Valor)> _columnas = new();

    public FakeDataRecord ConColumna(string nombre, object? valor)
    {
        _columnas.Add((nombre, valor));
        return this;
    }

    public int FieldCount => _columnas.Count;

    public string GetName(int i) => _columnas[i].Nombre;

    public bool IsDBNull(int i) => _columnas[i].Valor is null or DBNull;

    public object GetValue(int i) => _columnas[i].Valor ?? DBNull.Value;

    public string GetString(int i) => (string)_columnas[i].Valor!;

    public int GetOrdinal(string name)
    {
        for (var i = 0; i < _columnas.Count; i++)
        {
            if (string.Equals(_columnas[i].Nombre, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException(name);
    }

    public object this[int i] => GetValue(i);
    public object this[string name] => GetValue(GetOrdinal(name));

    // Miembros de IDataRecord no usados por los mapeadores bajo prueba.
    public bool GetBoolean(int i) => throw new NotSupportedException();
    public byte GetByte(int i) => throw new NotSupportedException();
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public char GetChar(int i) => throw new NotSupportedException();
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public IDataReader GetData(int i) => throw new NotSupportedException();
    public string GetDataTypeName(int i) => throw new NotSupportedException();
    public DateTime GetDateTime(int i) => throw new NotSupportedException();
    public decimal GetDecimal(int i) => throw new NotSupportedException();
    public double GetDouble(int i) => throw new NotSupportedException();
    public Type GetFieldType(int i) => throw new NotSupportedException();
    public float GetFloat(int i) => throw new NotSupportedException();
    public Guid GetGuid(int i) => throw new NotSupportedException();
    public short GetInt16(int i) => throw new NotSupportedException();
    public int GetInt32(int i) => throw new NotSupportedException();
    public long GetInt64(int i) => throw new NotSupportedException();
    public int GetValues(object[] values) => throw new NotSupportedException();
}
