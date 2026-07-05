# dlls/ — DLL 唯一來源（clone 後唯一的設置步驟）

所有 `csproj` 的 `<HintPath>` 都指向這個資料夾。DLL **不進版控**（商用 CPLEX 授權 + 建置產物），
所以 `git clone` 到新機器後，這裡是空的 → **要放好 6 個 DLL 才能 build**。

> 一鍵佈置：在 repo 根跑 `powershell -File scripts/setup-dlls.ps1`（自動偵測 CPLEX 安裝 + sibling OptimFoundation 建置輸出並複製過來）。
> 下方是手動對照，腳本失敗時照這張表補齊即可。

## 需要的 6 個 DLL

| 檔案 | 來源 | 說明 |
| --- | --- | --- |
| `ILOG.Concert.dll` | IBM CPLEX 安裝目錄 `cplex\bin\x64_win64\` | 商用，NEVER commit |
| `ILOG.CPLEX.dll` | 同上 | 商用，NEVER commit |
| `NLog.dll` | OptimFoundation 建置輸出（transitive）或 NuGet | logging |
| `OptimFoundation.Core.dll` | sibling repo `../OptimFoundation/` 建置輸出 | 框架本體（唯讀）|
| `OptimFoundation.Cplex.dll` | 同上 | CPLEX 後端 |
| `OptimFoundation.Generators.dll` | 同上 | source generator（analyzer）|

## 手動佈置

1. **CPLEX 兩個 DLL**：從本機 CPLEX 安裝複製（版本目錄依安裝而定）
   ```powershell
   Copy-Item "$env:CPLEX_STUDIO_DIR2211\cplex\bin\x64_win64\ILOG.Concert.dll" dlls\
   Copy-Item "$env:CPLEX_STUDIO_DIR2211\cplex\bin\x64_win64\ILOG.CPLEX.dll"   dlls\
   ```
   沒設環境變數就找 `C:\IBM\ILOG\CPLEX_Studio*\cplex\bin\x64_win64\`。

2. **OptimFoundation 四個 DLL**：先建 sibling 框架 repo，再複製 `net48` 輸出
   ```powershell
   dotnet build ..\OptimFoundation\OptimFoundation\OptimFoundation.sln -c Release
   # 從各 src\*\bin\Release\net48\ 複製 OptimFoundation.Core/Cplex/Generators.dll + NLog.dll 到 dlls\
   ```

## 執行期注意（run，不是 build）

CPLEX 的**原生** runtime（`cplex2211.dll` 等）必須在 `PATH` 上，`dotnet run` 才不會噴 `DllNotFoundException`：

```powershell
$env:PATH = "$env:CPLEX_STUDIO_DIR2211\cplex\bin\x64_win64;$env:PATH"
```

IBM 安裝程式通常已把它加進系統 PATH；換機器沒生效時手動加上這行。

## 為什麼不進版控

- `ILOG.*` 是 IBM 商用授權，散布違反授權（全域天條：NEVER commit solver DLL / license）
- `OptimFoundation.*` 是 sibling repo 的建置產物，應由來源建置，不 vender 進本 repo
- 見 `.gitignore`：`*.dll` 與 `dlls/` 皆已排除
