namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IDealTermsSerializer
{
    string Serialize(DealDto deal);
}
