// ReSharper disable once CheckNamespace
namespace Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Common;

public partial class DecimalValue
{
    private const decimal NanoFactor = 1_000_000_000;
    public DecimalValue(long units, int nanos)
    {
        Units = units;
        Nanos = nanos;
    }

    public static implicit operator decimal(DecimalValue grpcDecimal) =>
        grpcDecimal.Units + grpcDecimal.Nanos / NanoFactor;

    public static implicit operator DecimalValue(decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanos = decimal.ToInt32((value - units) * NanoFactor);
        return new(units, nanos);
    }
}
