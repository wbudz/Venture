using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public class Cash : StandardAsset
    {
        public Cash(PayTransactionDefinition ptd) : base()
        {
            if (String.IsNullOrEmpty(ptd.AccountDst))
            {
                throw new Exception("Tried creating cash with outgoing transaction.");
            }

            UniqueId = $"Cash_Payment_{ptd.Index}";
            AssetType = AssetType.Cash;
            Portfolio = Definitions.Portfolios.Single(x=>x.UniqueId == ptd.PortfolioDst);
            Currency = ptd.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            AddEvent(new PaymentEvent(this, ptd, ptd.Amount, PaymentDirection.Inflow));
            GenerateFlows();
        }

        public Cash(SellTransactionDefinition std, IEnumerable<DerecognitionEvent> dr) : base()
        {
            if (dr.Count() < 1) throw new Exception("Tried to create cash with 0 derecognition events.");
            UniqueId = $"Cash_Sale_{dr.First().ParentAsset.UniqueId}_{dr.First().Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = dr.First().ParentAsset.Portfolio;
            Currency = dr.First().ParentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            if (dr.Sum(x => x.Amount) == 0) throw new Exception($"Tried to create cash (from sale) with amount equal to 0.");

            AddEvent(new PaymentEvent(this, std, dr));
            GenerateFlows();
        }

        public Cash(FuturesRecognitionEvent fr) : base()
        {
            UniqueId = $"Cash_FuturesRecognition_{fr.ParentAsset.UniqueId}_{fr.Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = fr.ParentAsset.Portfolio;
            Currency = fr.ParentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            if (fr.Amount <= 0) throw new Exception($"Tried to create cash (from sale) with amount equal or less than 0.");

            PaymentDirection direction = PaymentDirection.Inflow;
            AddEvent(new PaymentEvent(this, fr, fr.Amount, direction));
            GenerateFlows();
        }

        public Cash(FuturesRevaluationEvent fs) : base()
        {
            UniqueId = $"Cash_FuturesSettlement_{fs.ParentAsset.UniqueId}_{fs.Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = fs.ParentAsset.Portfolio;
            Currency = fs.ParentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            if (fs.Amount <= 0) throw new Exception($"Tried to create cash (from sale) with amount equal or less than 0.");

            PaymentDirection direction = PaymentDirection.Inflow;
            AddEvent(new PaymentEvent(this, fs, fs.Amount, direction));
            GenerateFlows();
        }

        public Cash(FlowEvent fl) : base()
        {
            UniqueId = $"Cash_{fl.FlowType}_{fl.ParentAsset.UniqueId}_{fl.Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = fl.ParentAsset.Portfolio;
            Currency = fl.ParentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            // Recalculate current amount of flow
            fl.RecalculateAmount();

            if (fl.Amount == 0) throw new Exception($"Tried to create cash (from {fl.FlowType}) with amount equal to 0.");

            AddEvent(new PaymentEvent(this, fl, fl.Amount));
            GenerateFlows();
        }

        public Cash(AdditionalPremiumEventDefinition mn) : base()
        {
            UniqueId = $"Cash_AdditionalPremium_{mn.Description}_{mn.Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = Definitions.Portfolios.Single(x => x.UniqueId == mn.Portfolio);
            Currency = mn.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            AddEvent(new PaymentEvent(this, mn));
            GenerateFlows();
        }

        public Cash(EquityRedemptionEventDefinition mn, Security parentAsset, decimal amount) : base()
        {
            UniqueId = $"Cash_EquityRedemption_{mn.InstrumentUniqueId}_{mn.Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = parentAsset.Portfolio;
            Currency = parentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            AddEvent(new PaymentEvent(this, mn, amount));
            GenerateFlows();
        }

        protected override void GenerateFlows()
        {
            // No events to be generated.
        }

        protected override void RecalculateBounds()
        {
            decimal amount = 0;
            foreach (PaymentEvent p in Events.OfType<PaymentEvent>())
            {
                if (p.Direction == PaymentDirection.Inflow)
                {
                    amount = p.Amount;
                    bounds.startDate = p.Timestamp;
                    bounds.startIndex = p.TransactionIndex;
                }
                if (p.Direction == PaymentDirection.Outflow)
                {
                    amount -= p.Amount;
                    if (amount <= 0)
                    {
                        bounds.endDate = p.Timestamp;
                        bounds.endIndex = p.TransactionIndex;
                        return;
                    }
                }
            }
            bounds.endDate = Common.EndDate.AddDays(1);
            bounds.endIndex = -1;
        }

        public override ValuationEvent? GenerateValuation(DateTime date)
        {
            return null;
        }

        public override string ToString()
        {
            return $"Asset:Cash {UniqueId}";
        }

        public override decimal GetCount()
        {
            return 0;
        }

        public override decimal GetCount(TimeArg time)
        {
            return 0;
        }

        public override decimal GetCouponRate(DateTime date)
        {
            return 0;
        }

        #region Price

        public override decimal GetPurchasePrice(TimeArg time, bool dirty)
        {
            return IsActive(time) ? 1 : 0;
        }

        public override decimal GetMarketPrice(TimeArg time, bool dirty)
        {
            return IsActive(time) ? 1 : 0;
        }

        public override decimal GetAmortizedCostPrice(TimeArg time, bool dirty)
        {
            return IsActive(time) ? 1 : 0;
        }
        public override decimal GetAccruedInterest(DateTime date)
        {
            return 0;
        }

        #endregion

        #region Asset amount

        public override decimal GetNominalAmount(TimeArg time)
        {
            if (!IsActive(time)) return 0;

            decimal amount = 0;
            foreach (PaymentEvent p in GetEventsUntil(time).OfType<PaymentEvent>())
            {
                if (p.Direction == PaymentDirection.Inflow) amount += p.Amount;
                if (p.Direction == PaymentDirection.Outflow) amount -= p.Amount;
            }
            return Common.Round(amount);
        }

        public override decimal GetNominalAmount()
        {
            return Events.OfType<PaymentEvent>().Where(x => x.Direction == PaymentDirection.Inflow).FirstOrDefault()?.Amount ?? 0;
        }

        public override decimal GetInterestAmount(TimeArg time)
        {
            return 0;
        }

        public override decimal GetPurchaseAmount(TimeArg time, bool dirty)
        {
            return GetNominalAmount(time);
        }

        public override decimal GetMarketValue(TimeArg time, bool dirty)
        {
            return Common.Round((decimal)GetMarketPrice(time, dirty) * GetNominalAmount(time));
        }

        public override decimal GetAmortizedCostValue(TimeArg time, bool dirty)
        {
            return Common.Round((decimal)GetAmortizedCostPrice(time, dirty) * GetNominalAmount(time));
        }

        #endregion

        #region Parameters

        public override double GetTenor(DateTime date)
        {
            return 0;
        }

        public override double GetModifiedDuration(DateTime date)
        {
            return 0;
        }

        public override double GetYieldToMaturity(DateTime date, double price)
        {
            return 0;
        }

        public override double GetYieldToMaturity(DateTime date)
        {
            return 0;
        }

        #endregion

        public override decimal GetUnrealizedPurchaseFee(TimeArg time)
        {
            return 0;
        }

        #region Income

        public override decimal GetTimeValueOfMoneyIncome(TimeArg time)
        {
            return 0;
        }

        public override decimal GetCashflowIncome(TimeArg time)
        {
            return 0;
        }

        public override decimal GetRealizedGainsLossesFromValuation(Event e)
        {
            return 0;
        }

        public override decimal GetUnrealizedGainsLossesFromValuation(TimeArg time)
        {
            return 0;
        }

        public override decimal GetRealizedGainsLossesFromFX(Event e)
        {
            throw new NotImplementedException();
        }

        public override decimal GetUnrealizedGainsLossesFromFX(TimeArg time)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
