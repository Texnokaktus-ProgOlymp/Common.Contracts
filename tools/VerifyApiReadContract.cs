#:property TargetFramework=net10.0
#:project ../Texnokaktus.ProgOlymp.Common.Contracts/Texnokaktus.ProgOlymp.Common.Contracts.csproj

using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Texnokaktus.ProgOlymp.Common.Contracts.Grpc.Api;

VerifyDescriptor();
Console.WriteLine("PASS: unary read operations, field types, presence, nesting and allowed data boundary");
VerifyDistinctIdentities();
Console.WriteLine("PASS: binary round-trip preserves distinct API and per-stage external identities");
VerifyOptionalValues();
Console.WriteLine("PASS: absent, present-default and cleared optional values remain distinct");
VerifyEmptyRosters();
Console.WriteLine("PASS: empty catalog and empty roster retain their message shape");
Console.WriteLine("ApiReadService contract verification: 4 checks passed.");

static void VerifyDescriptor()
{
    var file = ApiReadServiceReflection.Descriptor;
    Check(file.Package == "progolymp.api", "The new service must use its own protobuf package.");
    var service = file.Services.Single();
    Check(service.Name == "ApiReadService", "Unexpected service name.");
    Check(service.Methods.Count == 2 && service.Methods.All(method => !method.IsClientStreaming && !method.IsServerStreaming),
          "The read contract must expose exactly two unary operations.");
    var list = service.Methods.Single(method => method.Name == "ListContests");
    Check(list.InputType == Empty.Descriptor && list.OutputType == ListContestsResponse.Descriptor,
          "The catalog operation has changed its request/response shape.");
    var snapshot = service.Methods.Single(method => method.Name == "GetRegistrationSnapshot");
    Check(snapshot.InputType == GetRegistrationSnapshotRequest.Descriptor && snapshot.OutputType == RegistrationSnapshot.Descriptor,
          "The snapshot operation has changed its request/response shape.");

    VerifyFields(GetRegistrationSnapshotRequest.Descriptor, ("contestId", FieldType.Int32, false, false));
    VerifyFields(ContestMetadata.Descriptor,
                 ("contestId", FieldType.Int32, false, false),
                 ("contestName", FieldType.String, false, false),
                 ("title", FieldType.String, false, false),
                 ("preliminaryContestId", FieldType.Int64, false, true),
                 ("finalContestId", FieldType.Int64, false, true));
    VerifyFields(ListContestsResponse.Descriptor, ("contests", FieldType.Message, true, false));
    Check(ListContestsResponse.Descriptor.FindFieldByNumber(1).MessageType == ContestMetadata.Descriptor,
          "Catalog entries must share the snapshot contest metadata.");
    VerifyFields(RegistrationSnapshot.Descriptor,
                 ("contest", FieldType.Message, false, true),
                 ("registrations", FieldType.Message, true, false));
    Check(RegistrationSnapshot.Descriptor.FindFieldByNumber(1).MessageType == ContestMetadata.Descriptor,
          "The snapshot must carry the same contest metadata as the catalog.");
    Check(RegistrationSnapshot.Descriptor.FindFieldByNumber(2).MessageType == RegistrationSnapshot.Types.Registration.Descriptor,
          "The roster must contain registrations owned by the snapshot.");
    var registration = RegistrationSnapshot.Types.Registration.Descriptor;
    Check(registration.ContainingType == RegistrationSnapshot.Descriptor, "Registration must be nested inside its snapshot.");
    VerifyFields(registration,
                 ("apiApplicationId", FieldType.Int32, false, false),
                 ("userId", FieldType.Int32, false, false),
                 ("grade", FieldType.Int32, false, false),
                 ("region", FieldType.String, false, false),
                 ("firstName", FieldType.String, false, false),
                 ("lastName", FieldType.String, false, false),
                 ("patronymic", FieldType.String, false, true),
                 ("preliminaryParticipantId", FieldType.Int64, false, true),
                 ("finalParticipantId", FieldType.Int64, false, true));
}

static void VerifyDistinctIdentities()
{
    var original = new RegistrationSnapshot
    {
        Contest = new ContestMetadata
        {
            ContestId = 17,
            ContestName = "contract-example",
            Title = "Contract example",
            PreliminaryContestId = 7_000_000_001,
            FinalContestId = 7_000_000_002
        },
        Registrations =
        {
            new RegistrationSnapshot.Types.Registration
            {
                ApiApplicationId = 31,
                UserId = 47,
                Grade = 9,
                Region = "Synthetic region",
                FirstName = "Example",
                LastName = "Participant",
                Patronymic = "Synthetic",
                PreliminaryParticipantId = 9_000_000_001,
                FinalParticipantId = 9_000_000_002
            }
        }
    };
    var restored = RegistrationSnapshot.Parser.ParseFrom(original.ToByteArray());
    Check(restored.Equals(original), "The binary snapshot changed during round-trip.");
    Check(restored.Contest.ContestId == 17 && restored.Contest.PreliminaryContestId == 7_000_000_001
          && restored.Contest.FinalContestId == 7_000_000_002, "API contest and external contest IDs were mixed.");
    var registration = restored.Registrations.Single();
    Check(registration.ApiApplicationId == 31 && registration.UserId == 47,
          "Application and user identities were mixed.");
    Check(registration.PreliminaryParticipantId == 9_000_000_001 && registration.FinalParticipantId == 9_000_000_002,
          "Participant identities from the two stages were mixed or narrowed to Int32.");
}

static void VerifyOptionalValues()
{
    var absent = RegistrationSnapshot.Parser.ParseFrom(new RegistrationSnapshot
    {
        Contest = new ContestMetadata(),
        Registrations = { new RegistrationSnapshot.Types.Registration() }
    }.ToByteArray());
    Check(!absent.Contest.HasPreliminaryContestId && !absent.Contest.HasFinalContestId,
          "Missing stage bindings must retain absence.");
    var registration = absent.Registrations.Single();
    Check(!registration.HasPatronymic && !registration.HasPreliminaryParticipantId && !registration.HasFinalParticipantId,
          "Missing registration details must retain absence.");

    absent.Contest.PreliminaryContestId = 0;
    absent.Contest.FinalContestId = 0;
    registration.Patronymic = "";
    registration.PreliminaryParticipantId = 0;
    registration.FinalParticipantId = 0;
    var present = RegistrationSnapshot.Parser.ParseFrom(absent.ToByteArray());
    registration = present.Registrations.Single();
    Check(present.Contest.HasPreliminaryContestId && present.Contest.HasFinalContestId && registration.HasPatronymic
          && registration.HasPreliminaryParticipantId && registration.HasFinalParticipantId,
          "Explicit protobuf defaults must not be mistaken for absence.");

    present.Contest.ClearPreliminaryContestId();
    present.Contest.ClearFinalContestId();
    registration.ClearPatronymic();
    registration.ClearPreliminaryParticipantId();
    registration.ClearFinalParticipantId();
    Check(RegistrationSnapshot.Parser.ParseFrom(present.ToByteArray()).Equals(new RegistrationSnapshot
    {
        Contest = new ContestMetadata(),
        Registrations = { new RegistrationSnapshot.Types.Registration() }
    }), "Cleared optional values must return to the absent representation.");
}

static void VerifyEmptyRosters()
{
    var catalog = ListContestsResponse.Parser.ParseFrom(new ListContestsResponse().ToByteArray());
    Check(catalog.Contests.Count == 0, "An empty catalog must remain empty.");
    var snapshot = RegistrationSnapshot.Parser.ParseFrom(new RegistrationSnapshot
    {
        Contest = new ContestMetadata { ContestId = 17, ContestName = "empty", Title = "Empty roster" }
    }.ToByteArray());
    Check(snapshot.Contest.ContestId == 17 && snapshot.Registrations.Count == 0,
          "An empty roster must retain the selected contest metadata.");
}

static void VerifyFields(MessageDescriptor message, params (string Name, FieldType Type, bool Repeated, bool Presence)[] expected)
{
    var fields = message.Fields.InFieldNumberOrder().ToArray();
    Check(fields.Length == expected.Length, $"Unexpected fields in {message.FullName}; review the data boundary explicitly.");
    for (var index = 0; index < expected.Length; index++)
    {
        var field = fields[index];
        var specification = expected[index];
        Check(field.FieldNumber == index + 1 && field.Name == specification.Name && field.FieldType == specification.Type
              && field.IsRepeated == specification.Repeated && field.HasPresence == specification.Presence,
              $"Unexpected field number/type/presence: {message.FullName}.{specification.Name}.");
    }
}

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
