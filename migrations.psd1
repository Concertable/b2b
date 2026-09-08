@{
    Environment = @{
        ConnectionStrings__B2BDb = 'Server=localhost;Database=concertable-b2b;Trusted_Connection=True;TrustServerCertificate=True'
    }
    Migrations = @(
        @{ Context = 'UserDbContext'; Project = 'src/Modules/User/Concertable.B2B.User.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'TenantDbContext'; Project = 'src/Modules/Tenant/Concertable.B2B.Tenant.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'AdminDbContext'; Project = 'src/Modules/Admin/Concertable.B2B.Admin.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'ArtistDbContext'; Project = 'src/Modules/Artist/Concertable.B2B.Artist.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'VenueDbContext'; Project = 'src/Modules/Venue/Concertable.B2B.Venue.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'OpportunityDbContext'; Project = 'src/Modules/Opportunity/Concertable.B2B.Opportunity.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'ApplicationDbContext'; Project = 'src/Modules/Application/Concertable.B2B.Application.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'BookingDbContext'; Project = 'src/Modules/Booking/Concertable.B2B.Booking.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'ConcertDbContext'; Project = 'src/Modules/Concert/Concertable.B2B.Concert.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'DealDbContext'; Project = 'src/Modules/Deal/Concertable.B2B.Deal.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
        @{ Context = 'ConversationsDbContext'; Project = 'src/Modules/Conversations/Concertable.B2B.Conversations.Infrastructure'; StartupProject = 'src/Concertable.B2B.Web'; OutputDir = 'Data/Migrations' }
    )
}
