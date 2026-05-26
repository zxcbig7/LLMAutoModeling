using OptimFoundation.Cplex;
using OptimFoundation.Core;
using SandBox.Data;
using SandBox.VariableClass;

namespace SandBox.Constraints
{
    /// <summary>
    /// 通用限制式語法全覽。
    /// 每個 region 對應一種常見約束模式，可直接複製到新的 Constraint_Xxx.cs 使用。
    ///
    /// 核心規則：
    ///   - AddLHS / AddRHS 是「累加」的，呼叫 CreateXxx 後才清空。
    ///   - 限制式名稱格式：$"{ConstraintName}@{index1}@{index2}"（用 @ 分隔，方便除錯）。
    ///   - 每建一條 ConstraintCount++。
    /// </summary>
    public class GenericConstraint_AllSyntax : ConstraintBase
    {
        private OptEngine      optEngine;
        private GenericDataload dataload;

        public GenericConstraint_AllSyntax(GenericDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                PatternA_Equality();
                PatternB_LessEqual();
                PatternC_GreatEqual();
                PatternD_SlidingWindow();
                PatternE_VariableOnRHS();
                PatternF_ConditionalSkip();

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception)
            {
                throw;
            }
        }

        #region A — 等式：∑ x[i][m][p] = demand[i][p]
        /// <summary>
        /// 每個 Item × Period 的總指派數必須等於需求量。
        /// 數學式：∑_m Assign[i][m][p] = Demand[i][p]   ∀ i, p
        /// </summary>
        private void PatternA_Equality()
        {
            dataload.Periods.ForEach(p =>
            {
                dataload.Items.ForEach(i =>
                {
                    // LHS：對所有 Machine 加總
                    dataload.Machines.ForEach(m =>
                    {
                        optEngine.AddLHS(1, new VariableB_Assign { Item = i, Machine = m, Period = p });
                    });

                    // RHS：需求常數
                    double demand = dataload.parameter_Demand
                        .FirstOrDefault(x => x.Item == i && x.Period == p)?.QTY ?? 0;
                    optEngine.AddRHS(demand);

                    optEngine.CreateEqual($"{ConstraintName}@{i}@{p:yyyy_MM_dd}");
                    ConstraintCount++;
                });
            });
        }
        #endregion

        #region B — 不等式（≤）：每台 Machine 每 Period 上限為 capacity
        /// <summary>
        /// 每台機台在每個 Period 的負載不超過容量（固定常數 RHS）。
        /// 數學式：∑_i Assign[i][m][p] ≤ Capacity   ∀ m, p
        /// </summary>
        private void PatternB_LessEqual()
        {
            const double capacity = 5;

            dataload.Periods.ForEach(p =>
            {
                dataload.Machines.ForEach(m =>
                {
                    dataload.Items.ForEach(i =>
                    {
                        optEngine.AddLHS(1, new VariableB_Assign { Item = i, Machine = m, Period = p });
                    });

                    optEngine.AddRHS(capacity);
                    optEngine.CreateLessEqual($"{ConstraintName}@{m}@{p:yyyy_MM_dd}");
                    ConstraintCount++;
                });
            });
        }
        #endregion

        #region C — 不等式（≥）：每個 Item 整月的 Slack 下界
        /// <summary>
        /// 每個 Item 的累積 Slack 必須 ≥ 下界。
        /// 數學式：Slack[i] + ∑_p ∑_m Assign[i][m][p] ≥ LowerBound   ∀ i
        /// </summary>
        private void PatternC_GreatEqual()
        {
            const double lowerBound = 10;

            dataload.Items.ForEach(i =>
            {
                // Continuous 變數
                optEngine.AddLHS(1, new VariableX_Slack { Item = i });

                // 加總所有 Binary 變數
                dataload.Periods.ForEach(p =>
                    dataload.Machines.ForEach(m =>
                        optEngine.AddLHS(1, new VariableB_Assign { Item = i, Machine = m, Period = p })
                    )
                );

                optEngine.AddRHS(lowerBound);
                optEngine.CreateGreatEqual($"{ConstraintName}@{i}");
                ConstraintCount++;
            });
        }
        #endregion

        #region D — 時間視窗（滑動窗口）：連續 N 期內不得超過 maxInWindow 次
        /// <summary>
        /// 滑動窗口限制：任意連續 windowSize 期內，某 Item 的指派總量 ≤ maxInWindow。
        /// 數學式：∑_{p' ∈ [p-N+1..p]} ∑_m Assign[i][m][p'] ≤ maxInWindow   ∀ i, p
        /// </summary>
        private void PatternD_SlidingWindow()
        {
            int    windowSize   = 5;
            double maxInWindow  = 3;

            dataload.Periods.ForEach(p =>
            {
                var window = dataload.Periods
                    .Where(sd => p.AddDays(-(windowSize - 1)) <= sd && sd <= p)
                    .ToList();

                if (window.Count < windowSize) return; // 資料不足，跳過

                dataload.Items.ForEach(i =>
                {
                    window.ForEach(wp =>
                        dataload.Machines.ForEach(m =>
                            optEngine.AddLHS(1, new VariableB_Assign { Item = i, Machine = m, Period = wp })
                        )
                    );

                    optEngine.AddRHS(maxInWindow);
                    optEngine.CreateLessEqual($"{ConstraintName}@{i}@{p:yyyy_MM_dd}");
                    ConstraintCount++;
                });
            });
        }
        #endregion

        #region E — 變數在 RHS（移項）：輔助 Binary 偵測是否超額
        /// <summary>
        /// 若某 Item 在某 Period 的指派量超過 threshold，則 Overflow flag = 1。
        /// 利用移項把右邊的變數搬到 RHS：
        ///   Assign_sum - threshold ≤ Overflow  →  AddLHS(Assign), AddRHS(threshold + Overflow)
        ///
        /// 數學式：∑_m Assign[i][m][p] - threshold ≤ BigM × Overflow[i][p]
        /// 簡化（BigM = 1 時）：Assign_sum ≤ threshold + Overflow[i][p]
        /// </summary>
        private void PatternE_VariableOnRHS()
        {
            const double threshold = 2;
            const double bigM      = 10; // 足夠大的常數

            dataload.Periods.ForEach(p =>
            {
                dataload.Items.ForEach(i =>
                {
                    // LHS：指派加總
                    dataload.Machines.ForEach(m =>
                        optEngine.AddLHS(1, new VariableB_Assign { Item = i, Machine = m, Period = p })
                    );

                    // RHS：常數 threshold + BigM × Overflow（移項後 Overflow 移到 RHS）
                    optEngine.AddRHS(threshold);
                    optEngine.AddRHS(bigM, new VariableB_Overflow { Item = i, Period = p });

                    optEngine.CreateLessEqual($"{ConstraintName}@{i}@{p:yyyy_MM_dd}");
                    ConstraintCount++;
                });
            });
        }
        #endregion

        #region F — 條件跳過：根據資料決定是否建立限制式
        /// <summary>
        /// 當某些條件不滿足時，直接 return 跳過，避免建立無效約束。
        /// 例如：窗口期數不足、參數不存在、值為 0 等。
        /// </summary>
        private void PatternF_ConditionalSkip()
        {
            int minPeriods = 3;

            dataload.Items.ForEach(i =>
            {
                dataload.Periods.ForEach(p =>
                {
                    // 條件 1：期數不足，跳過
                    var history = dataload.Periods.Where(sd => sd <= p).ToList();
                    if (history.Count < minPeriods) return;

                    // 條件 2：查無對應參數，跳過（QTY == 0 也可略過）
                    var param = dataload.parameter_Demand
                        .FirstOrDefault(x => x.Item == i && x.Period == p);
                    if (param == null || param.QTY == 0) return;

                    // 正常建立限制
                    dataload.Machines.ForEach(m =>
                        optEngine.AddLHS(1, new VariableB_Assign { Item = i, Machine = m, Period = p })
                    );
                    optEngine.AddRHS(param.QTY);
                    optEngine.CreateLessEqual($"{ConstraintName}@{i}@{p:yyyy_MM_dd}");
                    ConstraintCount++;
                });
            });
        }
        #endregion
    }
}
