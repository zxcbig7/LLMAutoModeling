using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Objective;

namespace HospitalRostering_Generator.Constraint
{
    /// <summary>
    /// 統一呼叫目標式與所有限制式（solve 與 experiment 兩模式共用，確保模型一致）。
    /// 與 HospitalRostering_Manual 的 BuildModel 內容**逐行相同**（架構差異不在此）。
    /// </summary>
    public class BuildModel
    {
        private readonly OptEngine engine;
        private readonly Dataload  dataload;

        public BuildModel(Dataload dataload, OptEngine engine)
        {
            this.engine   = engine;
            this.dataload = dataload;
        }

        public void Build()
        {
            try
            {
                Logging.Info("【建構目標式】");
                new ObjectiveFunction(dataload, engine).Build();

                Logging.Info("【建構限制式】");
                new Constraint_OneGroup(dataload, engine).Build();       // C1
                new Constraint_FullfillDemand(dataload, engine).Build(); // C2
                new Constraint_PreAssign(dataload, engine).Build();      // C3
                new Constraint_SixDayWork(dataload, engine).Build();     // C4
                new Constraint_CrossGroup(dataload, engine).Build();     // C5
                new Constraint_NightToDay(dataload, engine).Build();     // C6
                new Constraint_OffOneDay(dataload, engine).Build();      // C7
                new Constraint_DoubleOffLT2(dataload, engine).Build();   // C8 + C9
                new Constraint_BelowAVG(dataload, engine).Build();       // C10
                new Constraint_WeekendLT4(dataload, engine).Build();     // C11
            }
            catch (Exception) { throw; }
        }
    }
}
