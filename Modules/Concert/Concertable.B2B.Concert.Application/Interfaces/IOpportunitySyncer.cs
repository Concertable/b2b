using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application.Diffing;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IOpportunitySyncer : ICollectionSyncer<OpportunityEntity, OpportunityRequest>;
