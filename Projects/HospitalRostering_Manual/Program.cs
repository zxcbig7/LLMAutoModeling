using System.Linq;
using HospitalRostering_Manual;

// 架構 A（手動組裝）唯一進入點：solve / experiment 兩模式
//   dotnet run                → 一般求解（手寫 HospitalRosteringProblem.Execute()）
//   dotnet run -- experiment  → 參數掃描（ExperimentRunner，與 solve 共用 build-step）
if (args.Contains("experiment"))
{
    ExperimentRunner.Run();
    return;
}

using var problem = new HospitalRosteringProblem();
problem.Execute();
