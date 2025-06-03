namespace NpgsqlRest.UploadHandlers;

public enum UploadFileStatus { Empty, ProbablyBinary, InvalidImage, NoNewLines, InvalidMimeType, Ok }
