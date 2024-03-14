using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Venture
{
    public abstract class ManualEventDefinition : Definition
    {
        public abstract string UniqueId { get; }

        public DateTime Timestamp { get; private set; } = DateTime.MinValue;

        public ManualEventDefinition(Dictionary<string, string> data) : base(data)
        {
            Timestamp = ConvertToDateTime(data["timestamp"]);
        }

        public override string ToString()
        {
            return $"Manual: {UniqueId}";
        }
    }

    public abstract class ModifyingManualEventDefinition: ManualEventDefinition
    {
        public ModifyingManualEventDefinition(Dictionary<string, string> data) : base(data)
        {
        }
    }

    public abstract class FlowAmountAdjustmentEventDefinition : ModifyingManualEventDefinition
    {
        public override string UniqueId { get { return $"{GetType()}_{Timestamp:yyyyMMdd}_{AssetUniqueId}"; } }

        public string AssetUniqueId { get; private set; }

        public decimal Amount { get; private set; }

        public FlowAmountAdjustmentEventDefinition(Dictionary<string, string> data) : base(data)
        {
            AssetUniqueId = data["text1"];
            Amount = ConvertToDecimal(data["amount1"]);
        }

    }

    public class CouponAmountAdjustmentEventDefinition : FlowAmountAdjustmentEventDefinition
    {
        public CouponAmountAdjustmentEventDefinition(Dictionary<string, string> data) : base(data)
        {
        }
    }

    public class DividendAmountAdjustmentEventDefinition : FlowAmountAdjustmentEventDefinition
    {
        public DividendAmountAdjustmentEventDefinition(Dictionary<string, string> data) : base(data)
        {
        }
    }

    public abstract class TaxAmountAdjustmentEventDefinition : ModifyingManualEventDefinition
    {
        public override string UniqueId { get { return $"{GetType()}_{Timestamp:yyyyMMdd}_{AssetUniqueId}"; } }

        public string AssetUniqueId { get; private set; }

        public decimal Tax { get; private set; }

        public TaxAmountAdjustmentEventDefinition(Dictionary<string, string> data) : base(data)
        {
            AssetUniqueId = data["text1"];
            Tax = ConvertToDecimal(data["amount1"]);
        }
    }

    public class RedemptionTaxAdjustmentEventDefinition : TaxAmountAdjustmentEventDefinition
    {
        public RedemptionTaxAdjustmentEventDefinition(Dictionary<string, string> data) : base(data)
        {
        }
    }

    public class CouponTaxAdjustmentEventDefinition : TaxAmountAdjustmentEventDefinition
    {
        public CouponTaxAdjustmentEventDefinition(Dictionary<string, string> data) : base(data)
        {
        }
    }

    public class DividendTaxAdjustmentEventDefinition : TaxAmountAdjustmentEventDefinition
    {
        public DividendTaxAdjustmentEventDefinition(Dictionary<string, string> data) : base(data)
        {
        }
    }

    public class IncomeTaxAdjustmentEventDefinition : TaxAmountAdjustmentEventDefinition
    {
        public IncomeTaxAdjustmentEventDefinition(Dictionary<string, string> data) : base(data)
        {
        }
    }

    public class PrematureRedemptionEventDefinition : ModifyingManualEventDefinition
    {
        public override string UniqueId { get { return $"{GetType()}_{Timestamp:yyyyMMdd}_{InstrumentUniqueId}"; } }

        public string InstrumentUniqueId { get; private set; }

        public decimal DirtyPrice { get; private set; }

        public PrematureRedemptionEventDefinition(Dictionary<string, string> data) : base(data)
        {
            InstrumentUniqueId = data["text1"];
            DirtyPrice = ConvertToDecimal(data["amount1"]);
        }
    }

    public abstract class NonModifyingManualEventDefinition : ManualEventDefinition
    {
        public NonModifyingManualEventDefinition(Dictionary<string, string> data) : base(data)
        {
        }
    }

    public class EquitySpinOffEventDefinition : NonModifyingManualEventDefinition
    {
        public override string UniqueId { get { return $"{GetType()}_{Timestamp:yyyyMMdd}_{OriginalInstrumentUniqueId}"; } }

        public string OriginalInstrumentUniqueId { get; private set; }

        public string ConvertedInstrumentUniqueId { get; private set; }

        public string SpunOffInstrumentUniqueId { get; private set; }

        public decimal OriginalInstrumentCountMultiplier { get; private set; }

        public decimal ConvertedInstrumentCountMultiplier { get; private set; }

        public decimal SpunOffInstrumentCountMultiplier { get; private set; }

        public EquitySpinOffEventDefinition(Dictionary<string, string> data) : base(data)
        {
            OriginalInstrumentUniqueId = data["text1"];
            ConvertedInstrumentUniqueId = data["text2"];
            SpunOffInstrumentUniqueId = data["text3"];
            OriginalInstrumentCountMultiplier = ConvertToDecimal(data["amount1"]);
            ConvertedInstrumentCountMultiplier = ConvertToDecimal(data["amount2"]);
            SpunOffInstrumentCountMultiplier = ConvertToDecimal(data["amount3"]);
        }
    }

    public class EquityRedemptionEventDefinition : NonModifyingManualEventDefinition
    {
        public override string UniqueId { get { return $"{GetType()}_{Timestamp:yyyyMMdd}_{InstrumentUniqueId}"; } }

        public string InstrumentUniqueId { get; private set; }

        public decimal Price { get; private set; }

        public EquityRedemptionEventDefinition(Dictionary<string, string> data) : base(data)
        {
            InstrumentUniqueId = data["text1"];
            Price = ConvertToDecimal(data["amount1"]);
        }
    }

    public class AdditionalPremiumEventDefinition : NonModifyingManualEventDefinition
    {
        public override string UniqueId { get { return $"{GetType()}_{Timestamp:yyyyMMdd}_{CashAccount}_{Portfolio}"; } }

        public string CashAccount { get; private set; }

        public string Portfolio { get; private set; }

        public string Description { get; private set; }

        public decimal Amount { get; private set; }

        public decimal FXRate { get; private set; }

        public AdditionalPremiumEventDefinition(Dictionary<string, string> data) : base(data)
        {
            CashAccount = data["text1"];
            Portfolio = data["text2"];
            Description = data["text3"];
            Amount = ConvertToDecimal(data["amount1"]);
            FXRate = GetFXRateFromData(data["amount2"]);
        }
    }

    public class AdditionalChargeEventDefinition : NonModifyingManualEventDefinition
    {
        public override string UniqueId { get { return $"{GetType()}_{Timestamp:yyyyMMdd}_{CashAccount}_{Portfolio}"; } }

        public string CashAccount { get; private set; }

        public string Portfolio { get; private set; }

        public string Description { get; private set; }

        public decimal Amount { get; private set; }

        public decimal FXRate { get; private set; }

        public AdditionalChargeEventDefinition(Dictionary<string, string> data) : base(data)
        {
            CashAccount = data["text1"];
            Portfolio = data["text2"];
            Description = data["text3"];
            Amount = ConvertToDecimal(data["amount1"]);
            FXRate = GetFXRateFromData(data["amount2"]);
        }
    }
}
