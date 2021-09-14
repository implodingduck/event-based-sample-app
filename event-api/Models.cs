using System;
using System.Collections;
using System.Collections.Generic;

namespace event_api
{
    public class Account 
    {
        public string id { get; set; }
        public string type { get; set; }
        public decimal balance { get; set; }
        public string uid { get; set; }

    }

    public class Transaction 
    {
        public string id { get; set; }
        public decimal amount { get; set; }
        public string  description { get; set; }
        public string accountId { get; set; }

        public DateTime creationTime { get; set; }
        public DateTime completionTime { get; set; }

    }

    public class TransactionWrapper
    {
        public Transaction transaction { get; set; }
    }
}