﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Budziszewski.Venture.Events
{
    public enum PaymentDirection { Unspecified, Neutral, Inflow, Outflow }

    public abstract class Event
    {
        public string UniqueId
        {
            get
            {
                return $"{EventType}_{Timestamp:yyyy-MM-dd}_{TransactionIndex}_{Guid.NewGuid()}";
            }
        }

        public string EventType
        {
            get
            {
                return GetType().Name;
            }
        }

        public int TransactionIndex { get; protected set; } = -1;

        public DateTime Timestamp { get; set; }

        public Assets.Asset ParentAsset { get; set; }

        public PaymentDirection Direction { get; protected set; } = PaymentDirection.Unspecified;

        public decimal Amount { get; protected set; } = 0;

        public decimal Count { get; protected set; } = 0;

        public decimal FXRate { get; protected set; } = 1;

        public Event(Assets.Asset parentAsset)
        {
            ParentAsset = parentAsset;
        }


    }
}
