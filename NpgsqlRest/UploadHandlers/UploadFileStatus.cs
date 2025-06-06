namespace NpgsqlRest.UploadHandlers;

public enum UploadFileStatus
{ 
    Empty, 
    ProbablyBinary, 
    InvalidImage, 
    InvalidFileFormat, 
    NoNewLines, 
    InvalidMimeType, 
    Ok 
}
