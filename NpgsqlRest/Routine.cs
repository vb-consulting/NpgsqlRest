namespace NpgsqlRest;

public readonly struct Routine(
    string name,
    string expression,
    RoutineType type = RoutineType.Other,
    string schema = "",
    string? comment = null,
    bool isStrict = false,
    CrudType crudType = CrudType.Select,
    bool returnsRecordType = false,
    bool returnsSet = false,
    int columnCount = 1,
    string[]? columnNames = null,
    TypeDescriptor[]? columnsTypeDescriptor = null,
    bool returnsUnnamedSet = true,
    bool isVoid = true,
    int paramCount = 0,
    string[]? paramNames = null,
    TypeDescriptor[]? paramTypeDescriptor = null,
    string? fullDefinition = null,
    string? simpleDefinition = null,
    string? formatUrlPattern = null,
    string[]? tags = null,
    Func<RoutineEndpoint?, RoutineEndpoint?>? endpointHandler = null,
    object? metadata = null)
{
    /// <summary>
    /// Routine type: Function, Procedure, Table, View, or other.
    /// </summary>
    public RoutineType Type { get; init; } = type;

    /// <summary>
    /// Schema name (parsed by quote_ident).
    /// </summary>
    public string Schema { get; init; } = schema;

    /// <summary>
    /// PostgreSQL object name (parsed by quote_ident).
    /// </summary>
    public string Name { get; init; } = name;

    /// <summary>
    /// PostgreSQL object comment.
    /// </summary>
    public string? Comment { get; init; } = comment;

    /// <summary>
    /// Strict or non-strict function. Strict functions will return NULL if any parameter is NULL.
    /// </summary>
    public bool IsStrict { get; init; } = isStrict;

    /// <summary>
    /// The type of CRUD operation associated with the routine (Select, Insert, Update or Delete).
    /// </summary>
    public CrudType CrudType { get; init; } = crudType;

    /// <summary>
    /// Indicates whether the routine returns a PostgreSQL record type.
    /// </summary>
    public bool ReturnsRecordType { get; init; } = returnsRecordType;

    /// <summary>
    /// Indicates whether the routine returns a set of records or single record.
    /// </summary>
    public bool ReturnsSet { get; init; } = returnsSet;

    /// <summary>
    /// The number of columns returned.
    /// </summary>
    public int ColumnCount { get; init; } = columnCount;

    /// <summary>
    /// The names of the returned columns.
    /// </summary>
    public string[] ColumnNames { get; init; } = columnNames ?? [];

    /// <summary>
    /// The type descriptors for the returned columns.
    /// </summary>
    public TypeDescriptor[] ColumnsTypeDescriptor { get; init; } = columnsTypeDescriptor ?? [];

    /// <summary>
    /// Indicates whether the routine returns an unnamed set. 
    /// </summary>
    public bool ReturnsUnnamedSet { get; init; } = returnsUnnamedSet;

    /// <summary>
    /// Indicates whether the routine returns void.
    /// </summary>
    public bool IsVoid { get; init; } = isVoid;

    /// <summary>
    /// The number of parameters.
    /// </summary>
    public int ParamCount { get; init; } = paramCount;

    /// <summary>
    /// The names of the parameters.
    /// </summary>
    public string[] ParamNames { get; init; } = paramNames ?? [];

    /// <summary>
    /// The type descriptors of the parameter types.
    /// </summary>
    public TypeDescriptor[] ParamTypeDescriptor { get; init; } = paramTypeDescriptor ?? [];

    /// <summary>
    /// The expression associated with the routine.
    /// </summary>
    public string Expression { get; init; } = expression;

    /// <summary>
    /// The full definition of the routine. (Used for code-gen comments)
    /// </summary>
    public string FullDefinition { get; init; } = fullDefinition ?? expression;

    /// <summary>
    /// The simple definition of the routine. (Used for code-gen comments)
    /// </summary>
    public string SimpleDefinition { get; init; } = simpleDefinition ?? expression;

    /// <summary>
    /// The format URL pattern for the routine. If used (not null), placeholder {0} is used to replace the default URL.
    /// </summary>
    public string? FormatUrlPattern { get; init; } = formatUrlPattern;

    /// <summary>
    /// The tags associated with the routine.
    /// </summary>
    public string[]? Tags { get; init; } = tags;

    /// <summary>
    /// The endpoint handler for the routine.
    /// </summary>
    public Func<RoutineEndpoint?, RoutineEndpoint?>? EndpointHandler { get; init; } = endpointHandler;

    /// <summary>
    /// The meta data associated with the routine.
    /// </summary>
    public object? Metadata { get; init; } = metadata;
}
