using Grpc.Core;

namespace Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;

public class NotFoundException(string message, Exception? innerException = null) : RpcException(new(StatusCode.NotFound, message, innerException));
