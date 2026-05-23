using Concertable.Concert.Application.Requests;
using Concertable.Concert.Domain.Entities;
using Concertable.DataAccess.Application.Diffing;

namespace Concertable.Concert.Application.Interfaces;

internal interface IOpportunitySyncer : ICollectionSyncer<OpportunityEntity, OpportunityRequest>;
