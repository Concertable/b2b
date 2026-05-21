using Concertable.DataAccess.Diffing;
using Concertable.Concert.Application.Requests;

namespace Concertable.Concert.Application.Interfaces;

internal interface IOpportunitySyncer : ICollectionSyncer<OpportunityEntity, OpportunityRequest>;
