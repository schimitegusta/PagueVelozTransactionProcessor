using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PagueVeloz.TransactionProcessor.Domain.Enums
{
    public enum OperationType
    {
        Credit,
        Debit,
        Reserve,
        Capture,
        Reversal,
        Transfer
    }
}
