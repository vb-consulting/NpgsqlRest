namespace NpgsqlRest.UploadHandlers;

public enum UploadFileStatus
{ 
    Empty,
    ProbablyBinary,
    InvalidImage,
    InvalidFormat,
    NoNewLines,
    InvalidMimeType,
    Ignored,
    Ok 
}
