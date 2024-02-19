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
    bool returnsUnnamedSet,
    int paramCount,
    string[] paramNames,
    string[] paramTypes,
    string?[] paramDefaults,
    TypeDescriptor[] paramTypeDescriptor,
    bool isVoid,
    TypeDescriptor[] returnTypeDescriptor,
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
    public bool ReturnsUnnamedSet { get; } = returnsUnnamedSet;
    public int ParamCount { get; } = paramCount;
    public string[] ParamNames { get; } = paramNames;
    public string[] ParamTypes { get; } = paramTypes;
    public string?[] ParamDefaults { get; } = paramDefaults;
    public TypeDescriptor[] ParamTypeDescriptor { get; } = paramTypeDescriptor;
    public bool IsVoid { get; } = isVoid;
    public TypeDescriptor[] ReturnTypeDescriptor { get; } = returnTypeDescriptor;
    public string Expression { get; } = expression;
    public string FullDefinition { get; } = fullDefinition;
    public string SimpleDefinition { get; } = simpleDefinition;
    public bool FormattableCommand { get; } = formattableCommand;
}
