namespace Concertable.B2B.User.Infrastructure.Data;

internal class AdminProfileEntity
{
    private AdminProfileEntity() { }

    public AdminProfileEntity(Guid sub)
    {
        Sub = sub;
    }

    public Guid Sub { get; private set; }
}
