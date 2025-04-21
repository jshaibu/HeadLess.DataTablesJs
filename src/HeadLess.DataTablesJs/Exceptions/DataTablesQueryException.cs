namespace HeadLess.DataTablesJs.Exceptions;

public class DataTablesQueryException : Exception
{
    public DataTablesQueryException() { }

    public DataTablesQueryException(string message) 
        : base(message) { }

    public DataTablesQueryException(string message, Exception innerException) 
        : base(message, innerException) { }
}
