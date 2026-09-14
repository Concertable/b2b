using Concertable.B2B.Privacy.Infrastructure.Data;

namespace Concertable.B2B.Privacy.Infrastructure.Repositories;

internal sealed class SubjectErasureRepository(PrivacyDbContext context)
    : Repository<SubjectErasureRequestEntity>(context), ISubjectErasureRepository;
