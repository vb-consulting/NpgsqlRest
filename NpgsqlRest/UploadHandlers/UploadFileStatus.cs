namespace NpgsqlRest.UploadHandlers;

public enum UploadFileStatus { Empty, ProbablyBinary, NotAnImage, NoNewLines, InvalidMimeType, Ok }
