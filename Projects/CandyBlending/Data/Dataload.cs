using OptimFoundation.Core;
using OptimFoundation.Core.IO;

namespace CandyBlending
{
    /// <summary>資料唯一入口：把 Data/*.csv 讀成積木，再由 DataContext 驗證。</summary>
    public sealed partial class Dataload : DataContext
    {
        public List<Set_RawMaterial> set_RawMaterial = new();
        public List<Set_CandyBrand> set_CandyBrand = new();
        public List<Parameter_MaterialCost> parameter_MaterialCost = new();
        public List<Parameter_MonthlySupplyLimit> parameter_MonthlySupplyLimit = new();
        public List<Parameter_ProcessingCost> parameter_ProcessingCost = new();
        public List<Parameter_SellingPrice> parameter_SellingPrice = new();
        public List<Parameter_MinContentRatio> parameter_MinContentRatio = new();
        public List<Parameter_MaxContentRatio> parameter_MaxContentRatio = new();

        /// <summary>無參數建構子只做一件事：把預設來源餵給下面真正讀檔的建構子。</summary>
        public Dataload() : this(new CsvDataSource()) { }

        /// <summary>標準接口：一行載一顆，只讀不算。換 CSV / DB / 記憶體只換 source。</summary>
        public Dataload(IDataSource source)
        {
            set_RawMaterial = source.Load<Set_RawMaterial>("Set_RawMaterial");
            set_CandyBrand = source.Load<Set_CandyBrand>("Set_CandyBrand");
            parameter_MaterialCost = source.Load<Parameter_MaterialCost>("Parameter_MaterialCost");
            parameter_MonthlySupplyLimit = source.Load<Parameter_MonthlySupplyLimit>("Parameter_MonthlySupplyLimit");
            parameter_ProcessingCost = source.Load<Parameter_ProcessingCost>("Parameter_ProcessingCost");
            parameter_SellingPrice = source.Load<Parameter_SellingPrice>("Parameter_SellingPrice");
            parameter_MinContentRatio = source.Load<Parameter_MinContentRatio>("Parameter_MinContentRatio");
            parameter_MaxContentRatio = source.Load<Parameter_MaxContentRatio>("Parameter_MaxContentRatio");
        }

        /// <summary>import 模式：把 Data/raw/ 的不規則來源攤平成標準 CSV。rawFile 相對於 Data/、不帶副檔名。
        /// 本題的 canonical CSV 直接由 Model.md 的表格逐格謄寫而來，沒有 raw 來源需要攤平；此建構子維持四段模板要求的
        /// import 能力，載入現行標準 CSV 後由 Export() 原樣寫回（round-trip 自檢）。</summary>
        public Dataload(string rawFile) : this(new CsvDataSource())
        {
            Logging.Info($"[import] 本專案無 raw 攤平步驟（canonical CSV 由 Model.md 表格逐格謄寫）；rawFile='{rawFile}' 僅記錄不使用。");
        }

        /// <summary>把積木寫成標準 CSV，成為求解模式的 input。檔名 MUST 與上面讀取時一致。</summary>
        public void Export()
        {
            CsvCtrl.WriteRows(set_RawMaterial, "Set_RawMaterial");
            CsvCtrl.WriteRows(set_CandyBrand, "Set_CandyBrand");
            CsvCtrl.WriteRows(parameter_MaterialCost, "Parameter_MaterialCost");
            CsvCtrl.WriteRows(parameter_MonthlySupplyLimit, "Parameter_MonthlySupplyLimit");
            CsvCtrl.WriteRows(parameter_ProcessingCost, "Parameter_ProcessingCost");
            CsvCtrl.WriteRows(parameter_SellingPrice, "Parameter_SellingPrice");
            CsvCtrl.WriteRows(parameter_MinContentRatio, "Parameter_MinContentRatio");
            CsvCtrl.WriteRows(parameter_MaxContentRatio, "Parameter_MaxContentRatio");
            Logging.Info("[import] exported: Set_RawMaterial, Set_CandyBrand, Parameter_MaterialCost, " +
                         "Parameter_MonthlySupplyLimit, Parameter_ProcessingCost, Parameter_SellingPrice, " +
                         "Parameter_MinContentRatio, Parameter_MaxContentRatio");
        }
    }
}
