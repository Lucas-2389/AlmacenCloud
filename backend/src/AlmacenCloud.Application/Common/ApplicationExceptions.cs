namespace AlmacenCloud.Application.Common;

public sealed class ValidationException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class AuthenticationException(string message) : Exception(message);
