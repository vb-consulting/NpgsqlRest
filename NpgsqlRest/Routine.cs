namespace NpgsqlRest;

public enum RoutineType { Other, Function, Procedure }
public enum Language { Other, Plpgsql, Sql }
public enum SecurityType { Definer, Invoker }
public enum ParallelOption { Unsafe, Safe, Restricted }
public enum VolatilityOption { Immutable, Stable, Volatile }

public readonly struct Routine(
    RoutineType type, 
    string typeInfo,
    string schema,
    string name,
    string oid,
    string signature,
    Language language,
    string languageInfo,
    string comment,
    SecurityType securityType,
    bool isStrict,
    decimal cost,
    decimal rows,
    ParallelOption parallelOption,
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
    string[] paramDefaults,
    string definition,
    TypeDescriptor[] paramTypeDescriptor,
    bool isVoid,
    Dictionary<int, string> expressions,
    TypeDescriptor[] returnTypeDescriptor)
{
    public RoutineType Type { get; } = type;
    public string TypeInfo { get; } = typeInfo;
    public string Schema { get; } = schema;
    public string Name { get; } = name;
    public string Oid { get; } = oid;
    public string Signature { get; } = signature;
    public Language Language { get; } = language;
    public string LanguageInfo { get; } = languageInfo;
    public string Comment { get; } = comment;
    public SecurityType SecurityType { get; } = securityType;
    public bool IsStrict { get; } = isStrict;
    public decimal Cost { get; } = cost;
    public decimal Rows { get; } = rows;
    public ParallelOption ParallelOption { get; } = parallelOption;
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
    public string[] ParamDefaults { get; } = paramDefaults;
    public string Definition { get; } = definition;
    public TypeDescriptor[] ParamTypeDescriptor { get; } = paramTypeDescriptor;
    public bool IsVoid { get; } = isVoid;
    public Dictionary<int, string> Expressions { get; } = expressions;
    public TypeDescriptor[] ReturnTypeDescriptor { get; } = returnTypeDescriptor;
}