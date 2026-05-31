# Data 資料夾規則

## Dataload 結構

- Namespace：`ProjectName.Data`
- 全域常數直接宣告為 `public field`
- Sets 用 `=>` 由 Parameters 衍生，不重複定義資料

## WriteToCSV 規範

- 必須先呼叫 `FolderDir.Solution.CreateFolder()`
- 呼叫 `CsvCtrl.SaveSolutionToCSV<T>(engine, dataId, userId)`
