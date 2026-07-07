namespace AquaHome.Domain.Exceptions;

/// <summary>Ném khi R2 staging vượt hard block (90% = 9GB) — API filter map thành HTTP 503.</summary>
public class StorageOverloadedException(string message) : Exception(message);
