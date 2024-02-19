namespace NpgsqlRest;

public readonly struct Routine(
    RoutineType type, 
    string schema,
    string name,
    string? comment,
    bool isStrict,
    VolatilityOption volatilityOption,
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
    string definition,
    TypeDescriptor[] paramTypeDescriptor,
    bool isVoid,
    TypeDescriptor[] returnTypeDescriptor,
    string expression,
    bool variadic)
{
    public RoutineType Type { get; } = type;
    public string Schema { get; } = schema;
    public string Name { get; } = name;
    public string? Comment { get; } = comment;
    public bool IsStrict { get; } = isStrict;
    public VolatilityOption VolatilityOption { get; } = volatilityOption;
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
    public string Definition { get; } = definition;
    public TypeDescriptor[] ParamTypeDescriptor { get; } = paramTypeDescriptor;
    public bool IsVoid { get; } = isVoid;
    public TypeDescriptor[] ReturnTypeDescriptor { get; } = returnTypeDescriptor;
    public string Expression { get; } = expression;
    public bool IsVariadic { get; } = variadic;
}
