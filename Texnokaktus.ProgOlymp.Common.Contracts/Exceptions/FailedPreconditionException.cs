using Grpc.Core;

namespace Texnokaktus.ProgOlymp.Common.Contracts.Exceptions;

public class FailedPreconditionException(string message, Exception? innerException = null) : RpcException(new(StatusCode.FailedPrecondition, message, innerException));
