namespace Concertable.B2B.Privacy.Infrastructure.Mappers;

internal static class SubjectErasureMappers
{
    extension(SubjectErasureRequestEntity request)
    {
        public SubjectErasureRequestDto ToDto() => new()
        {
            Id = request.Id,
            SubjectId = request.SubjectId,
            State = request.State,
            RequestedAtUtc = request.RequestedAtUtc,
            CompletedAtUtc = request.CompletedAtUtc,
            DeferralReason = request.DeferralReason,
        };
    }
}
