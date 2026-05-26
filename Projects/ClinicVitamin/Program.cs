using ClinicVitamin;

using (var problem = new ClinicVitaminProblem())
{
    bool ok = problem.Execute();
    if (!ok) Console.Error.WriteLine("[ERROR] 求解失敗");
}
