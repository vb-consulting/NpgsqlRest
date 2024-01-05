using NpgsqlTypes;

namespace NpgsqlRest;

public class TypeDescriptor
{
    public string OriginalType { get; }
    public string Type { get; }
    public bool IsArray { get; }
    public bool IsNumeric { get; }
    public bool IsJson { get; }
    public bool IsDate { get; }
    public bool IsDateTime { get; }
    public bool IsBoolean { get; }
    public bool IsText { get; }
    public NpgsqlDbType DbType { get; }

    public TypeDescriptor(string type)
    {
        OriginalType = type;
        IsArray = type.EndsWith("[]");
        Type = (IsArray ? type[..^2] : type).Trim('"');
        DbType = this.GetDbType();

        IsNumeric = DbType switch
        {
            NpgsqlDbType.Smallint => true,
            NpgsqlDbType.Integer => true,
            NpgsqlDbType.Bigint => true,
            NpgsqlDbType.Numeric => true,
            NpgsqlDbType.Real => true,
            NpgsqlDbType.Double => true,
            NpgsqlDbType.Money => true,
            _ => false
        };
        IsJson = DbType switch
        {
            NpgsqlDbType.Jsonb => true,
            NpgsqlDbType.Json => true,
            NpgsqlDbType.JsonPath => true,
            _ => false
        };
        IsDate = DbType == NpgsqlDbType.Date;
        IsBoolean = DbType == NpgsqlDbType.Boolean;
        IsDateTime = DbType == NpgsqlDbType.Timestamp || DbType == NpgsqlDbType.TimestampTz;

        IsText = DbType == NpgsqlDbType.Text ||
            DbType == NpgsqlDbType.Xml ||
            DbType == NpgsqlDbType.Varchar ||
            DbType == NpgsqlDbType.Char ||
            DbType == NpgsqlDbType.Name ||
            DbType == NpgsqlDbType.Jsonb ||
            DbType == NpgsqlDbType.Json ||
            DbType == NpgsqlDbType.JsonPath;
    }

    private NpgsqlDbType GetDbType()
    {
        var result = Type switch
        {
            "smallint" => NpgsqlDbType.Smallint,
            "integer" => NpgsqlDbType.Integer,
            "bigint" => NpgsqlDbType.Bigint,
            "decimal" => NpgsqlDbType.Numeric,
            "numeric" => NpgsqlDbType.Numeric,
            "real" => NpgsqlDbType.Real,
            "double precision" => NpgsqlDbType.Double,
            "int2" => NpgsqlDbType.Smallint,
            "int4" => NpgsqlDbType.Integer,
            "int8" => NpgsqlDbType.Bigint,
            "float4" => NpgsqlDbType.Real,
            "float8" => NpgsqlDbType.Double,
            "money" => NpgsqlDbType.Money,
            "smallserial" => NpgsqlDbType.Smallint,
            "serial" => NpgsqlDbType.Integer,
            "bigserial" => NpgsqlDbType.Bigint,

            "text" => NpgsqlDbType.Text,
            "xml" => NpgsqlDbType.Xml,
            "varchar" => NpgsqlDbType.Varchar,
            "character varying" => NpgsqlDbType.Varchar,
            "bpchar" => NpgsqlDbType.Char,
            "character" => NpgsqlDbType.Char,
            "char" => NpgsqlDbType.Char,
            "name" => NpgsqlDbType.Name,
            "refcursor" => NpgsqlDbType.Refcursor,
            "jsonb" => NpgsqlDbType.Jsonb,
            "json" => NpgsqlDbType.Json,
            "jsonpath" => NpgsqlDbType.JsonPath,

            "timestamp" => NpgsqlDbType.Timestamp,
            "timestamptz" => NpgsqlDbType.TimestampTz,
            "timestamp without time zone" => NpgsqlDbType.Timestamp,
            "timestamp with time zone" => NpgsqlDbType.TimestampTz,
            "date" => NpgsqlDbType.Date,
            "time" => NpgsqlDbType.Time,
            "timetz" => NpgsqlDbType.TimeTz,
            "time without time zone" => NpgsqlDbType.Time,
            "time with time zone" => NpgsqlDbType.TimeTz,
            "interval" => NpgsqlDbType.Interval,

            "bool" => NpgsqlDbType.Boolean,
            "boolean" => NpgsqlDbType.Boolean,
            "bytea" => NpgsqlDbType.Bytea,
            "uuid" => NpgsqlDbType.Uuid,
            "varbit" => NpgsqlDbType.Varbit,
            "bit" => NpgsqlDbType.Bit,

            "cidr" => NpgsqlDbType.Cidr,
            "inet" => NpgsqlDbType.Inet,
            "macaddr" => NpgsqlDbType.MacAddr,
            "macaddr8" => NpgsqlDbType.MacAddr8,

            "tsquery" => NpgsqlDbType.TsQuery,
            "tsvector" => NpgsqlDbType.TsVector,

            "box" => NpgsqlDbType.Box,
            "circle" => NpgsqlDbType.Circle,
            "line" => NpgsqlDbType.Line,
            "lseg" => NpgsqlDbType.LSeg,
            "path" => NpgsqlDbType.Path,
            "point" => NpgsqlDbType.Point,
            "polygon" => NpgsqlDbType.Polygon,

            "oid" => NpgsqlDbType.Oid,
            "xid" => NpgsqlDbType.Xid,
            "xid8" => NpgsqlDbType.Xid8,
            "cid" => NpgsqlDbType.Cid,
            "regtype" => NpgsqlDbType.Regtype,
            "regconfig" => NpgsqlDbType.Regconfig,

            "int4range" => NpgsqlDbType.IntegerRange,
            "int8range" => NpgsqlDbType.BigIntRange,
            "numrange" => NpgsqlDbType.NumericRange,
            "tsrange" => NpgsqlDbType.TimestampRange,
            "tstzrange" => NpgsqlDbType.TimestampTzRange,
            "daterange" => NpgsqlDbType.DateRange,

            "int4multirange" => NpgsqlDbType.IntegerMultirange,
            "int8multirange" => NpgsqlDbType.BigIntMultirange,
            "nummultirange" => NpgsqlDbType.NumericMultirange,
            "tsmultirange" => NpgsqlDbType.TimestampMultirange,
            "tstzmultirange" => NpgsqlDbType.TimestampTzMultirange,
            "datemultirange" => NpgsqlDbType.DateMultirange,

            "int2vector" => NpgsqlDbType.Int2Vector,
            "oidvector" => NpgsqlDbType.Oidvector,
            "pg_lsn" => NpgsqlDbType.PgLsn,
            "tid" => NpgsqlDbType.Tid,

            "citext" => NpgsqlDbType.Citext,
            "lquery" => NpgsqlDbType.LQuery,
            "ltree" => NpgsqlDbType.LTree,
            "ltxtquery" => NpgsqlDbType.LTxtQuery,
            "hstore" => NpgsqlDbType.Hstore,
            "geometry" => NpgsqlDbType.Geometry,
            "geography" => NpgsqlDbType.Geography,

            _ => NpgsqlDbType.Unknown
        };

        if (this.IsArray)
        {
            result |= NpgsqlDbType.Array;
        }

        return result;
    }
}
