using Budziszewski.Venture.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Budziszewski.Financial.Calendar;

namespace Budziszewski.Venture.Assets
{
    public class Cash : Asset
    {
        public Cash(Data.Transaction tr) : base(tr)
        {
            UniqueId = $"{tr.AccountDst}_{tr.SettlementDate:yyyyMMdd}_{tr.NominalAmount:0.00}";
            Portfolio = tr.PortfolioDst;
            CashAccount = tr.AccountDst;
            Currency = tr.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            Events.Add(new Events.Payment(this, tr));
        }

        public Cash(Events.Flow fl)
        {
            UniqueId = $"{fl.ParentAsset.CashAccount}_{fl.Timestamp:yyyyMMdd}_{fl.Amount:0.00}";

            UniqueId = "";
            Portfolio = fl.ParentAsset.Portfolio;
            CashAccount = fl.ParentAsset.CashAccount;
            Currency = fl.ParentAsset.Currency;
            ValuationClass = ValuationClass.AvailableForSale;

            Events.Add(new Events.Payment(this, fl));
        }

        protected override void GenerateFlows()
        {
            // No events to be generated.
        }

        public override AssetsViewEntry GenerateAssetViewEntry()
        {
            return new AssetsViewEntry()
            {
                UniqueId = this.UniqueId,
                Portfolio = this.Portfolio,
                CashAccount = this.CashAccount,
                Currency = this.Currency,
                ValuationClass = this.ValuationClass
            };
        }

        public override string ToString()
        {
            return $"Asset:Cash {UniqueId}";
        }
    }
}
