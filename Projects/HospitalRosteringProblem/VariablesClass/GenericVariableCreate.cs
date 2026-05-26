using OptimFoundation.Cplex;
using OptimFoundation.Core;
using SandBox.Data;
using SandBox.VariableClass;

namespace SandBox.VariablesClass
{
    /// <summary>
    /// 通用變數建立範本：統一建立所有決策變數。
    ///
    /// BuildBVs&lt;T&gt;(set1, set2, ...)  →  Binary 變數，屬性順序對應 set 順序
    /// BuildCVs&lt;T&gt;(set1, ...)         →  Continuous 變數
    /// </summary>
    public class GenericVariableCreate
    {
        private OptEngine     optEngine;
        private GenericDataload dataload;
        private int varCount => optEngine.varCount;

        public GenericVariableCreate(GenericDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // Binary 變數（3 個 sets → Item × Machine × Period）
                optEngine.BuildBVs<VariableB_Assign>(
                    dataload.Items,
                    dataload.Machines,
                    dataload.Periods);

                // Binary 輔助變數（2 個 sets → Item × Period）
                optEngine.BuildBVs<VariableB_Overflow>(
                    dataload.Items,
                    dataload.Periods);

                // Continuous 變數（1 個 set → Item）
                optEngine.BuildCVs<VariableX_Slack>(dataload.Items);

                Logging.Info($"Variables created: {varCount}");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
