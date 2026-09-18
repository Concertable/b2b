using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.DataAccess.Infrastructure.Extensions;

public static class SpatialPropertyExtensions
{
    public static PropertyBuilder<TProperty> HasWgs84PointColumn<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder) =>
        propertyBuilder.HasColumnType("geometry (point, 4326)");
}
