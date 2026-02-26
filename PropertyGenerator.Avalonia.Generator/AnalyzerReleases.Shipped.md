; Shipped analyzer releases
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.3.1

### New Rules

| Rule ID | Category                   | Severity | Notes                                                      |
| ------- | -------------------------- | -------- | ---------------------------------------------------------- |
| PGA1001 | PropertyGenerator.Avalonia | Error    | invalid property declaration shape for generated property. |
| PGA1002 | PropertyGenerator.Avalonia | Error    | containing type must inherit AvaloniaObject.               |
| PGA1003 | PropertyGenerator.Avalonia | Error    | callback method not found.                                 |
| PGA1004 | PropertyGenerator.Avalonia | Error    | callback method signature invalid.                         |
| PGA1005 | PropertyGenerator.Avalonia | Error    | invalid direct getter/setter method reference.             |
| PGA1006 | PropertyGenerator.Avalonia | Error    | invalid attached property name.                            |
| PGA1007 | PropertyGenerator.Avalonia | Error    | Containing type must be partial                            |
| PGA1008 | PropertyGenerator.Avalonia | Warning  | duplicate attached property name on owner.                 |
| PGA1009 | PropertyGenerator.Avalonia | Warning  | GenerateOnPropertyChanged target property not found.       |
| PGA1010 | PropertyGenerator.Avalonia | Warning  | GenerateOnPropertyChanged disabled                         |

