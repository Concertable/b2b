@{
    Environment = @{
        ConnectionStrings__B2BDb = 'Server=localhost;Database=concertable-b2b;Trusted_Connection=True;TrustServerCertificate=True'
    }
    Migrations = @(
        @{ Context = 'UserDbContext'; Project = 'api/src/Modules/User/Concertable.B2B.User.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'TenantDbContext'; Project = 'api/src/Modules/Tenant/Concertable.B2B.Tenant.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'AdminDbContext'; Project = 'api/src/Modules/Admin/Concertable.B2B.Admin.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'ArtistDbContext'; Project = 'api/src/Modules/Artist/Concertable.B2B.Artist.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'VenueDbContext'; Project = 'api/src/Modules/Venue/Concertable.B2B.Venue.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'OpportunityDbContext'; Project = 'api/src/Modules/Opportunity/Concertable.B2B.Opportunity.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'ApplicationDbContext'; Project = 'api/src/Modules/Application/Concertable.B2B.Application.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'BookingDbContext'; Project = 'api/src/Modules/Booking/Concertable.B2B.Booking.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'ConcertDbContext'; Project = 'api/src/Modules/Concert/Concertable.B2B.Concert.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'DealDbContext'; Project = 'api/src/Modules/Deal/Concertable.B2B.Deal.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'ConversationsDbContext'; Project = 'api/src/Modules/Conversations/Concertable.B2B.Conversations.Infrastructure'; StartupProject = 'api/src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
    )
}
