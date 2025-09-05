using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Services
{
    public class TransactionMetrics
    {
        private readonly Counter<long> _transactionCounter;
        private readonly Histogram<double> _transactionDuration;
        private readonly Counter<long> _transactionErrors;

        public TransactionMetrics(IMeterFactory meterFactory)
        {
            var meter = meterFactory.Create("PagueVeloz.Transactions");

            _transactionCounter = meter.CreateCounter<long>(
                "transactions.processed",
                description: "Número de transações processadas");

            _transactionDuration = meter.CreateHistogram<double>(
                "transactions.duration",
                unit: "ms",
                description: "Duração do processamento da transação");

            _transactionErrors = meter.CreateCounter<long>(
                "transactions.errors",
                description: "Número de erros de transação");
        }

        public void RecordTransaction(string operation, string status)
        {
            var tags = new TagList
        {
            { "operation", operation },
            { "status", status }
        };

            _transactionCounter.Add(1, tags);

            if (status == "failed")
            {
                _transactionErrors.Add(1, tags);
            }
        }

        public void RecordDuration(string operation, double duration)
        {
            var tags = new TagList { { "operation", operation } };
            _transactionDuration.Record(duration, tags);
        }
    }
}
