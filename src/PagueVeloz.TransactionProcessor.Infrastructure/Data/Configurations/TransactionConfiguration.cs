using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PagueVeloz.TransactionProcessor.Domain.Entities;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Data.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.ToTable("Transactions");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .ValueGeneratedNever();

            builder.Property(t => t.AccountId)
                .IsRequired();

            builder.Property(t => t.Operation)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(t => t.Currency)
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(t => t.ReferenceId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(t => t.MetadataJson)
                .HasColumnType("nvarchar(max)");

            builder.Property(t => t.BalanceAfter)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(t => t.ReservedBalanceAfter)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(t => t.AvailableBalanceAfter)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(t => t.ErrorMessage)
                .HasMaxLength(500);

            builder.Property(t => t.ProcessedAt)
                .IsRequired();

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            builder.HasIndex(t => t.ReferenceId)
                .IsUnique();

            builder.HasIndex(t => t.AccountId);
            builder.HasIndex(t => t.ProcessedAt);
            builder.HasIndex(t => new { t.AccountId, t.ProcessedAt });

            builder.Ignore(t => t.DomainEvents);
        }
    }
}
