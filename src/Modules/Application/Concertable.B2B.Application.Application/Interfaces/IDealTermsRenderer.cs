namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IDealTermsRenderer
{
    string Render(DealDto deal);
}

internal interface IDealTerms
{
    string Render(DealDto deal);
    string Serialize(DealDto deal);
}

internal interface IStepResolver<TStep>
    where TStep : class
{
    TStep Resolve(DealType dealType);
}
