using Venture.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Venture.Data;

namespace Venture.Assets
{
    public class Cash : Asset
    {
        public Cash(Data.Transaction tr) : base()
        {
            if (tr.TransactionType != TransactionType.Cash)
            {
                throw new Exception("Tried creating cash with transaction type other than cash.");
            }
            if (String.IsNullOrEmpty(tr.AccountDst))
            {
                throw new Exception("Tried creating cash with outgoing transaction.");
            }

            UniqueId = $"Cash_{tr.TransactionType}_{tr.Index}";
            AssetType = AssetType.Cash;
            Portfolio = tr.PortfolioDst;
            CashAccount = tr.AccountDst;
            CustodyAccount = "";
            Currency = tr.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            AddEvent(new Events.Payment(this, tr, tr.Amount, Venture.Events.PaymentDirection.Inflow));
            GenerateFlows();
        }

        public Cash(Data.Transaction tr, IEnumerable<Events.Derecognition> dr) : base()
        {
            if (dr.Count() < 1) throw new Exception("Tried to create cash with 0 derecognition events.");
            UniqueId = $"Cash_Sale_{dr.First().ParentAsset.UniqueId}_{dr.First().Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = dr.First().ParentAsset.Portfolio;
            CashAccount = dr.First().ParentAsset.CashAccount;
            CustodyAccount = "";
            Currency = dr.First().ParentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            if (dr.Sum(x => x.Amount) == 0) throw new Exception($"Tried to create cash (from sale) with amount equal to 0.");

            AddEvent(new Events.Payment(this, tr, dr, Venture.Events.PaymentDirection.Inflow));
            GenerateFlows();
        }

        public Cash(Events.Recognition r) : base()
        {
            if (r.ParentAsset.AssetType != AssetType.Futures) throw new Exception("Tried to create cash with recognition event not originating from futures contract.");
            UniqueId = $"Cash_Futures_{r.ParentAsset.UniqueId}_{r.Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = r.ParentAsset.Portfolio;
            CashAccount = r.ParentAsset.CashAccount;
            CustodyAccount = "";
            Currency = r.ParentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            if (r.Amount == 0) throw new Exception($"Tried to create cash (from sale) with amount equal to 0.");

            Events.PaymentDirection direction = Venture.Events.PaymentDirection.Inflow;
            AddEvent(new Events.Payment(this, r, Math.Abs(r.Amount), direction));
            GenerateFlows();
        }

        public Cash(Events.Flow fl) : base()
        {
            UniqueId = $"Cash_{fl.FlowType}_{fl.ParentAsset.UniqueId}_{fl.Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = fl.ParentAsset.Portfolio;
            CashAccount = fl.ParentAsset.CashAccount;
            CustodyAccount = "";
            Currency = fl.ParentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            // Recalculate current amount of flow
            fl.RecalculateAmount();

            if (fl.Amount == 0) throw new Exception($"Tried to create cash (from {fl.FlowType}) with amount equal to 0.");

            AddEvent(new Events.Payment(this, fl, fl.Amount, Venture.Events.PaymentDirection.Inflow));
            GenerateFlows();
        }

        public Cash(Manual mn) : base()
        {
            // Creates cash from account balance interest manual event.
            if (mn.AdjustmentType != ManualAdjustmentType.AdditionalPremium)
                throw new Exception($"Unexpected manual adjustment type used for creating cash: {mn.AdjustmentType}.");

            UniqueId = $"Cash_{mn.AdjustmentType}_{mn.Text1}_{mn.Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = mn.Text2;
            CustodyAccount = "";
            CashAccount = mn.Text1;
            Currency = mn.Text1.Split(':')[2];
            ValuationClass = ValuationClass.AvailableForSale;

            AddEvent(new Events.Payment(this, mn, mn.Amount1, Venture.Events.PaymentDirection.Inflow));
            GenerateFlows();
        }

        public Cash(Manual mn, Security parentAsset, decimal amount) : base()
        {
            // Creates cash from equity redemption manual event.
            if (mn.AdjustmentType != ManualAdjustmentType.EquityRedemption)
                throw new Exception($"Unexpected manual adjustment type used for creating cash: {mn.AdjustmentType}.");

            UniqueId = $"Cash_{mn.AdjustmentType}_{mn.Text1}_{mn.Timestamp.ToString("yyyyMMdd")}";
            AssetType = AssetType.Cash;
            Portfolio = parentAsset.Portfolio;
            CustodyAccount = "";
            CashAccount = parentAsset.CashAccount;
            Currency = parentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            AddEvent(new Events.Payment(this, mn, amount, Venture.Events.PaymentDirection.Inflow));
            GenerateFlows();
        }

        protected override void GenerateFlows()
        {
            // No events to be generated.
        }

        protected override void RecalculateBounds()
        {
            decimal amount = 0;
            foreach (Events.Payment p in Events.OfType<Events.Payment>())
            {
                if (p.Direction == Venture.Events.PaymentDirection.Inflow)
                {
                    amount = p.Amount;
                    bounds.startDate = p.Timestamp;
                    bounds.startIndex = p.TransactionIndex;
                }
                if (p.Direction == Venture.Events.PaymentDirection.Outflow)
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
            bounds.endDate = Common.FinalDate.AddDays(1);
            bounds.endIndex = -1;
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
            foreach (Events.Payment p in GetEvents(time).OfType<Events.Payment>())
            {
                if (p.Direction == Venture.Events.PaymentDirection.Inflow) amount += p.Amount;
                if (p.Direction == Venture.Events.PaymentDirection.Outflow) amount -= p.Amount;
            }
            return Common.Round(amount);
        }

        public override decimal GetNominalAmount()
        {
            return Events.OfType<Events.Payment>().Where(x => x.Direction == Venture.Events.PaymentDirection.Inflow).FirstOrDefault()?.Amount ?? 0;
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

        public override decimal GetRealizedGainsLossesFromValuation(Events.Event e)
        {
            return 0;
        }

        public override decimal GetUnrealizedGainsLossesFromValuation(TimeArg time)
        {
            return 0;
        }

        public override decimal GetRealizedGainsLossesFromFX(Events.Event e)
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
