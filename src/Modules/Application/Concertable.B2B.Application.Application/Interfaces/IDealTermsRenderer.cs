namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IDealTermsRenderer
{
    string Render(IDeal deal);
}

internal interface IDealTerms
{
    string Render(IDeal deal);
    string Serialize(IDeal deal);
}

internal interface IStepResolver<TStep>
    where TStep : class
{
    TStep Resolve(DealType dealType);
}
