using Grpc.Core;

namespace Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;

public class AlreadyExistsException(string message, Exception? innerException = null) : RpcException(new(StatusCode.AlreadyExists, message, innerException));
