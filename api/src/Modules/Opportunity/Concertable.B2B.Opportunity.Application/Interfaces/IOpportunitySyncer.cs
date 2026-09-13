using Concertable.B2B.Opportunity.Application.Requests;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.DataAccess.Application.Diffing;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunitySyncer : ICollectionSyncer<OpportunityEntity, OpportunityRequest>;
