using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PagueVeloz.TransactionProcessor.Domain.Entities;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Data.Configurations
{
    public class ClientConfiguration : IEntityTypeConfiguration<Client>
    {
        public void Configure(EntityTypeBuilder<Client> builder)
        {
            builder.ToTable("Clients");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .ValueGeneratedNever();

            builder.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(c => c.Document)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(c => c.Email)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.IsActive)
                .IsRequired();

            builder.Property(c => c.CreatedAt)
                .IsRequired();

            builder.Property(c => c.UpdatedAt);

            builder.HasIndex(c => c.Document)
                .IsUnique();

            builder.HasIndex(c => c.Email);
            builder.HasIndex(c => c.IsActive);

            builder.Ignore(c => c.DomainEvents);
        }
    }
}
