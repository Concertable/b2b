using System.Data;
using Dapper;

namespace Concertable.B2B.E2ETests;

public class BookingDb
{
    private readonly IDbConnection connection;

    public BookingDb(IDbConnection connection)
    {
        this.connection = connection;
    }

    public Task<int> GetStatusByApplicationIdAsync(int applicationId) =>
        connection.QuerySingleAsync<int>(
            "SELECT Status FROM concert.Bookings WHERE ApplicationId = @applicationId",
            new { applicationId });

    public Task<int> GetIdByApplicationIdAsync(int applicationId) =>
        connection.QuerySingleAsync<int>(
            "SELECT Id FROM concert.Bookings WHERE ApplicationId = @applicationId",
            new { applicationId });
}
