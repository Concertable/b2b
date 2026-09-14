using Concertable.DataAccess.Application;

namespace Concertable.B2B.Privacy.Application.Interfaces;

internal interface ISubjectErasureRepository : IRepository<SubjectErasureRequestEntity, Guid>;
