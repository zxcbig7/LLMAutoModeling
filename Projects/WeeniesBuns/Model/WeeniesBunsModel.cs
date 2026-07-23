using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Set;
using WeeniesBuns.Variable;
using WeeniesBuns.Objective;
using WeeniesBuns.Constraint;

namespace WeeniesBuns.Model
{
    // 模型組裝：變數 + 目標式 + 全部限制式組成一顆完整模型，plug 進 OptModel（solve / experiment 共用）。
    // 各層仍是子積木（Variable* / ObjectiveFunction / Constraint_*），本類只負責「組裝順序」。
    public class WeeniesBunsModel
    {
        private readonly WeeniesBunsDataload _d;

        public WeeniesBunsModel(WeeniesBunsDataload d) => _d = d;

        // 變數層（給 OptModel.AddVariables）
        public void CreateVariables(OptEngine engine)
        {
            engine.BuildCVs<VariableX_Production>(_d.ProductTypes);
            Logging.Info($"Variables created: {engine.varCount}");
        }

        // 目標式 + 限制式層（給 OptModel.AddModel）
        public void CreateModel(OptEngine engine)
        {
            new ObjectiveFunction(_d.parameter_ProductSpec, engine).Build();

            new Constraint_Flour(_d.parameter_ProductSpec, _d.FlourCapacity, engine).Build(); // [C1] ≤
            new Constraint_Pork(_d.parameter_ProductSpec, _d.PorkCapacity, engine).Build(); // [C2] ≤
            new Constraint_Labor(_d.parameter_ProductSpec, _d.LaborCapacity, engine).Build(); // [C3] ≤
        }

        // 一次組全（experiment / 手動建 engine 用）：變數 → 目標式 → 限制式
        public void Build(OptEngine engine)
        {
            CreateVariables(engine);
            CreateModel(engine);
        }
    }
}
