using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PagueVeloz.TransactionProcessor.Domain.Entities;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Data.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("Accounts");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .ValueGeneratedNever();

            builder.Property(a => a.ClientId)
                .IsRequired();

            builder.Property(a => a.Balance)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(a => a.ReservedBalance)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(a => a.CreditLimit)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(a => a.Currency)
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(a => a.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            builder.Property(a => a.UpdatedAt);

            builder.Property(a => a.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            builder.HasOne<Client>()
                .WithMany(c => c.Accounts)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(a => a.Transactions)
                .WithOne()
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.ClientId);
            builder.HasIndex(a => a.Status);
            builder.HasIndex(a => new { a.ClientId, a.Currency });

            builder.Ignore(a => a.AvailableBalance);
            builder.Ignore(a => a.TotalAvailable);
            builder.Ignore(a => a.DomainEvents);
        }
    }
}
