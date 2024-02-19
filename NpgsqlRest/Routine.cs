namespace NpgsqlRest;

public readonly struct Routine(
    RoutineType type, 
    string schema,
    string name,
    string? comment,
    bool isStrict,
    CrudType crudType,

    bool returnsRecord,
    string returnType,
    int returnRecordCount,
    string[] returnRecordNames,
    string[] returnRecordTypes,
    TypeDescriptor[] returnTypeDescriptor,
    bool returnsUnnamedSet,
    bool isVoid,

    int paramCount,
    string[] paramNames,
    TypeDescriptor[] paramTypeDescriptor,
    string expression,
    string fullDefinition,
    string simpleDefinition,
    bool formattableCommand)
{
    public RoutineType Type { get; } = type;
    public string Schema { get; } = schema;
    public string Name { get; } = name;
    public string? Comment { get; } = comment;
    public bool IsStrict { get; } = isStrict;
    public CrudType CrudType { get; } = crudType;

    public bool ReturnsRecord { get; } = returnsRecord;
    public string ReturnType { get; } = returnType;
    public int ReturnRecordCount { get; } = returnRecordCount;
    public string[] ReturnRecordNames { get; } = returnRecordNames;
    public string[] ReturnRecordTypes { get; } = returnRecordTypes;
    public TypeDescriptor[] ReturnTypeDescriptor { get; } = returnTypeDescriptor;
    public bool ReturnsUnnamedSet { get; } = returnsUnnamedSet;
    public bool IsVoid { get; } = isVoid;

    public int ParamCount { get; } = paramCount;
    public string[] ParamNames { get; } = paramNames;
    public TypeDescriptor[] ParamTypeDescriptor { get; } = paramTypeDescriptor;
    
    public string Expression { get; } = expression;
    public string FullDefinition { get; } = fullDefinition;
    public string SimpleDefinition { get; } = simpleDefinition;
    public bool FormattableCommand { get; } = formattableCommand;
}
