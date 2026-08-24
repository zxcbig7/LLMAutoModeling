using OptimFoundation.Core;
using OptimFoundation.Core.IO;

namespace Template
{
    /// <summary>資料唯一入口：把 Data/*.csv 讀成積木，再由 DataContext 驗證。
    /// MUST 是 public sealed partial class 且繼承 DataContext —— generator 靠這兩個條件掃出它，
    /// 少任一個不會報錯，只會整顆 Dataload 一輩子不受驗證。</summary>
    public sealed partial class Dataload : DataContext
    {
        public List<Set_Item> set_Item = new();
        public List<Set_Date> set_Date = new();
        public List<Set_AllowedSlot> set_AllowedSlot = new();
        public List<Parameter_Demand> parameter_Demand = new();
        public List<Parameter_DailyCapacity> parameter_DailyCapacity = new();
        public List<Parameter_MinActiveDays> parameter_MinActiveDays = new();
        public List<Parameter_MaxShortage> parameter_MaxShortage = new();
        public List<Parameter_ShortagePenalty> parameter_ShortagePenalty = new();
        public List<Parameter_BigMProduce> parameter_BigMProduce = new();

        /// <summary>無參數建構子只做一件事：把預設來源餵給下面真正讀檔的建構子。</summary>
        public Dataload() : this(new CsvDataSource()) { }

        /// <summary>標準接口：一行載一顆，只讀不算。換 CSV / DB / 記憶體只換 source。
        /// 這個建構子的敘述白名單只有 source.Load<T> 一種句子——NEVER 出現迴圈、if、Random、日期運算、補值。</summary>
        public Dataload(IDataSource source)
        {
            set_Item = source.Load<Set_Item>("Set_Item");
            set_Date = source.Load<Set_Date>("Set_Date");
            set_AllowedSlot = source.Load<Set_AllowedSlot>("Set_AllowedSlot");
            parameter_Demand = source.Load<Parameter_Demand>("Parameter_Demand");
            parameter_DailyCapacity = source.Load<Parameter_DailyCapacity>("Parameter_DailyCapacity");
            parameter_MinActiveDays = source.Load<Parameter_MinActiveDays>("Parameter_MinActiveDays");
            parameter_MaxShortage = source.Load<Parameter_MaxShortage>("Parameter_MaxShortage");
            parameter_ShortagePenalty = source.Load<Parameter_ShortagePenalty>("Parameter_ShortagePenalty");
            parameter_BigMProduce = source.Load<Parameter_BigMProduce>("Parameter_BigMProduce");
        }

        /// <summary>import 模式：攤平 Data/raw/ 的不規則來源，或依生成規格產出 instance。rawFile 相對於 Data/、不帶副檔名。
        /// 本範本的 canonical CSV 是直接寫好的，沒有 raw 來源需要攤平，因此採 api-guide §2.4 的退化形式：
        /// 委派給標準來源，讓 Export() 變成 round-trip 自檢。真的有 raw 檔要攤平時，把
        /// `new CsvDataSource().LoadData(rawFile)` 讀進來的 DataTable 在這裡展開成 List<Set_*> / List<Parameter_*>
        /// ——這是全專案唯一允許出現迴圈、Random 與日期運算的地方。</summary>
        public Dataload(string rawFile) : this(new CsvDataSource())
        {
            Logging.Info($"[import] 本範本無 raw 攤平步驟；rawFile='{rawFile}' 僅記錄不使用。");
        }

        /// <summary>把積木寫成標準 CSV，成為求解模式的 input。檔名 MUST 與上面讀取時一致（compiler 不會驗這件事）。</summary>
        public void Export()
        {
            CsvCtrl.WriteRows(set_Item, "Set_Item");
            CsvCtrl.WriteRows(set_Date, "Set_Date");
            CsvCtrl.WriteRows(set_AllowedSlot, "Set_AllowedSlot");
            CsvCtrl.WriteRows(parameter_Demand, "Parameter_Demand");
            CsvCtrl.WriteRows(parameter_DailyCapacity, "Parameter_DailyCapacity");
            CsvCtrl.WriteRows(parameter_MinActiveDays, "Parameter_MinActiveDays");
            CsvCtrl.WriteRows(parameter_MaxShortage, "Parameter_MaxShortage");
            CsvCtrl.WriteRows(parameter_ShortagePenalty, "Parameter_ShortagePenalty");
            CsvCtrl.WriteRows(parameter_BigMProduce, "Parameter_BigMProduce");
            Logging.Info("[import] exported: Set_Item, Set_Date, Set_AllowedSlot, Parameter_Demand, " +
                         "Parameter_DailyCapacity, Parameter_MinActiveDays, Parameter_MaxShortage, " +
                         "Parameter_ShortagePenalty, Parameter_BigMProduce");
        }
    }
}
