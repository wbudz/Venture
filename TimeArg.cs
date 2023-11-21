﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture
{
    public enum TimeArgDirection { Unspecified, Start, End }

    public class TimeArg
    {
        public TimeArgDirection Direction { get; protected set; } = TimeArgDirection.Unspecified;
        public DateTime? Date { get; protected set; } = null;
        public int TransactionIndex { get; protected set; } = -1;

        public TimeArg(TimeArgDirection direction, DateTime date, int transactionIndex = -1)
        {
            this.Direction = direction;
            this.Date = date;
            this.TransactionIndex = transactionIndex;
        }
    }
}
