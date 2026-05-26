using SandwichProduction;

using (var problem = new SandwichProblem())
{
    bool success = problem.Execute();
    if (!success) Console.Error.WriteLine("[ERROR] 求解失敗");
}
