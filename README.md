# Final Project: TicketPipeline (Support Ticket Dispatcher)

Introduction to Functional Programming, final exam 2026/2027. Individual project.

| | |
|---|---|
| Student | Nuralieva Olimpiya, group IT-2514 |
| Repository URL | https://github.com/OlimpiyaNuralieva/TicketPipeline-FinalExam |
| Branch | `main` |
| Final commit | `bad8a166737e85c17f28b0e3b57d8b36b9cf50e5` (all code, data and tests are in this commit) |
| SDK | .NET SDK 10.0.x (`net10.0`), xUnit 2.9.3 |
| Native compiler | gcc (Windows x64: MSYS2 UCRT64 gcc; Linux x64: gcc 13), see section 8 |

## 1. Problem, users, goals

A support team has tickets (Question, Request, Incident) with an effort from 1 to 8 and agents with a capacity.
The **dispatcher** wants to know which ticket to start first and who can take it. The **team lead** wants to see the
open workload and how loaded every agent is. The program loads both data sets from CSV, ranks tickets with a
configurable policy, assigns tickets to agents without breaking their capacity, estimates the cost with a native C
function, and saves the assignments and the cost report.

## 2. Domain models (two related models)

* `Ticket` (`Models.cs`): id, title, effort, open/closed, kind. Built only by `Ticket.Create` (private constructor, no setters).
* `Agent` (`Models.cs`): name, capacity. Built only by `Agent.Create`.
* `Assignment` / `AssignmentBook` (`Models.cs`): an assignment links a ticket id to an agent name. The book is immutable.
* `TicketState` (`Models.cs`): `New`, `Assigned`, `Resolved` with different payloads.

## 3. Workflows (minimum for one student is 3; there are 4)

1. **Ticket lifecycle** `Workflow.Process`: Find (Option) -> ToResult -> Bind Begin -> Bind Assign -> Bind Resolve -> Map Describe.
2. **Priority queue** `Pipeline.Run`: Workable -> Score (policy) -> Recommendations (3 named stages, two policies).
3. **Cost report and export** `ReportWorkflow.BuildAndExport`: load -> Ensure unique ids -> two reports -> native cost per ticket -> CSV export.
4. **Assign ticket to agent** `Dispatch.Assign`: find ticket -> find agent -> business rules (open, not assigned, fits capacity) -> new `AssignmentBook` -> `Dispatch.Save`.

## 4. Reports (three)

* `Reports.WorkloadByKind`: Where(open) + GroupBy(kind) + Count/Sum, order by TotalEffort desc then Kind.
* `Reports.PriorityList`: Workable -> lazy score -> OrderByDescending(score).ThenBy(id) -> Take -> rank -> ToArray.
* `Reports.AgentLoad`: GroupBy(agent) on the book, projection per agent, order by Free desc then Name.

All are pure, return arrays, and handle an empty dataset, no matching records and tied sort values.

## 5. Requirement -> code

| Requirement | Where |
|---|---|
| Pure core, immutable models | `Models.cs`, `Pipeline.cs`, `Reports.cs`, `Dispatch.Plan`; I/O only in `Program.cs`, `Demo.cs`, `*File.cs`, `Native.cs` |
| Pattern matching | `Dispatch.Plan` (switch on a tuple of three conditions), `Workflow.Assign/Resolve`, `Scoring.CreateScore` |
| Pipeline with 3 named stages | `Pipeline.SelectWorkable` -> `ScoreTickets` -> `BuildRecommendations` (`Pipeline.Run`) |
| Higher-order function | `Func<Ticket,int>` policy in `Pipeline.ScoreTickets`, `Reports.PriorityList` |
| Two policies, different output on the same input | `Scoring.SeverityConfig` vs `Scoring.QuickWinsConfig` (test `Run_TwoPolicies_GiveDifferentOrderOnSameInput`) |
| Validated factories | `Ticket.Create`, `Agent.Create` (private constructors) |
| Safe operation sequence | `TicketState` + `Workflow` (Resolve before Assign is rejected); `Dispatch.Assign` returns a new book, a failure keeps the old one |
| Result with 3+ failures | `TicketInvalid`, `TicketNotFound`, `NotAllowed`, `AgentNotFound`, `OverCapacity`, `AlreadyAssigned`, file and native errors (`Result.cs`) |
| Map and Bind, failure stops the chain | `Workflow.Process`, `Dispatch.Assign` (test `Assign_FailureStopsTheChain...`) |
| Maybe/Option | `Option<T>`, `Pipeline.FindTicket`, `Dispatch.FindAgent` |
| File and parsing exceptions at the boundary | `LineFile.ReadAll` (used by `TicketFile.Load` and `AgentFile.Load`), `ReportFile.Export` |
| 2 custom extension methods | `Workable`, `Ensure` (`Extensions.cs`), used in `Pipeline`, `Reports`, `ReportWorkflow`, `Program.cs` |
| Curry and partial application | `Scoring.CreateScore` (curried), `Scoring.Severity`, `Scoring.QuickWins`, used in `Program.cs` |
| `yield return`, deferred execution, deliberate snapshot | `Streams.ScoreLazily`, `Streams.ReadLines`; `ToArray()` in `Reports.PriorityList` and `TicketFile.Load` |
| JSON/CSV load and save | `data/tickets.csv`, `data/agents.csv`; `assignments.csv`, `queue-report.csv` |
| Resource handling | `using` in `Streams.ReadLines` and `ReportFile.Export`; the report is written to a `.tmp` file first, so a failed export keeps the old file |
| Native C via P/Invoke | `native/ticketcost.c/.h`, `Native.cs`, used in `ReportWorkflow.Price` |

## 6. Data files

`TicketPipeline/data/` is copied next to the program by the `.csproj`; paths are built from `AppContext.BaseDirectory`
(tickets path can also be given as the first argument).

* `tickets.csv`: `id;title;effort;isOpen;kind`
* `agents.csv`: `name;capacity`
* `malformed.csv`, `malformed-agents.csv`: broken examples used by the demo and by tests

## 7. Build, run, test

Run in the folder that contains `TicketPipeline.slnx`.

```
powershell -ExecutionPolicy Bypass -File native\build.ps1    # Linux: bash native/build.sh
dotnet build
dotnet run --project TicketPipeline            # interactive menu
dotnet run --project TicketPipeline -- --demo  # scripted demonstration of every workflow and failure
dotnet test
```

Menu: `1` list, `2` priority queue, `3` ticket lifecycle, `4` assign to agent, `5` reports, `6` cost report + export, `7` save assignments.
Suggested defense demo: `4` ticket 5 -> Chen (OK), then `4` ticket 8 -> Chen (FAIL over capacity), `5`, `7`.

## 8. Native C library

### Function contract (`native/ticketcost.h`, `native/ticketcost.c`)

```c
int32_t ticket_cost_cents(int32_t kind, int32_t effort);
```

| | |
|---|---|
| Purpose | Estimated cost of a ticket in **integer cents** |
| `kind` | 0 = Question, 1 = Request, 2 = Incident (same numbers as C# `TicketKind`) |
| `effort` | 1 to 8 |
| Rates | cents per effort point: Question 2050, Request 3575, Incident 6025 |
| Discount | effort >= 6: 5 % off, in cents **rounded half up**: `floor((base*5 + 50) / 100)` |
| Return | cost in cents (> 0), or `-1` invalid kind, `-2` invalid effort |
| Types | `int32_t` in C, `int` (32 bit) in C#. No pointers and no native memory. |
| Manual example | Incident, effort 7: base 6025 x 7 = 42175; discount (210875 + 50) / 100 = 2109; cost **40066** cents (400.66) |

### C# side

- `Native.cs`: `NativeMethods.TicketCostCents` with `[DllImport("ticketcost", EntryPoint = "ticket_cost_cents", CallingConvention = CallingConvention.Cdecl)]`. `DllImport` needs no project setting. The name has no prefix or extension, so .NET finds `ticketcost.dll` (Windows) or `libticketcost.so` (Linux).
- Adapter `NativeCost.Calculate(...)`: it checks kind and effort **before** the call (`native.invalid_input`). It turns `DllNotFoundException`, `EntryPointNotFoundException` and `BadImageFormatException` into `native.unavailable`, and a negative return code into `native.rejected`. The native function is a parameter (`Func<int,int,int>`), so a test can pass a failing one.
- Real use: `ReportWorkflow.BuildFrom` calls the adapter for every ranked ticket. The costs and `TotalCostCents` appear in the report and in the exported CSV. The native call is outside the pure query code.

### Build and run

Target platforms: **Windows x64** (`ticketcost.dll`) and Linux x64 (`libticketcost.so`).

| Platform | Build command | Output |
|---|---|---|
| Windows x64 | `powershell -ExecutionPolicy Bypass -File native\build.ps1` (runs `gcc -std=c99 -O2 -shared -o out\ticketcost.dll ticketcost.c`; if there is no `gcc` it uses `cl` from the "x64 Native Tools Command Prompt for VS") | `native\out\ticketcost.dll` |
| Linux x64 | `bash native/build.sh` (runs `gcc -std=c99 -O2 -fPIC -fvisibility=hidden -shared -o out/libticketcost.so ticketcost.c`) | `native/out/libticketcost.so` |

I checked the C part on Ubuntu 24.04, x86-64, gcc 13.3.0 (the build works, `nm -D` shows `ticket_cost_cents`, and the manual examples give the same values). Windows x64: gcc 16.2.0 (MSYS2 UCRT64), build command `powershell -ExecutionPolicy Bypass -File native\build.ps1`, output `native\out\ticketcost.dll`.

The `.csproj` copies everything from `native/out/` next to the program and, through the project reference, next to the tests. **I must build the native library first**, then run the .NET commands. If I build the native library after `dotnet build`, I run `dotnet build` again so the file is copied. The library must have the same architecture as .NET (x64).

### Difficulties and how I solved them

1. **No C compiler on Windows.** On my Windows computer `gcc` was not recognized, so `build.ps1` failed and no DLL was created. Then every native call returned `native.unavailable` and the tests that make a real native call failed. Solution: I installed MSYS2 (`winget install MSYS2.MSYS2`), installed gcc in the UCRT64 terminal (`pacman -S mingw-w64-ucrt-x86_64-gcc`) and added `C:\msys64\ucrt64\bin` to PATH. Then I restarted the terminal, built the DLL with `build.ps1` and ran `dotnet build` again. After that all native calls worked.
2. **Hidden export on Linux.** With `-fvisibility=hidden`, a build without the export attribute works but exports nothing: `nm -D` showed no function and loading failed with `undefined symbol: ticket_cost_cents` (in .NET it is `EntryPointNotFoundException`). Solution: the `TICKETCOST_API` macro (`__declspec(dllexport)` on Windows, `visibility("default")` on Linux) in front of the function. The adapter reports this case as `native.unavailable`, and `Adapter_RealMissingExport_BecomesNativeUnavailable` tests it with a real wrong function name.

### Loading and adapter failure tests

One injected failing function (`DllNotFoundException`) and one real missing export both become `native.unavailable`.

## 9. Tests (xUnit)

Test files: `TicketTests`, `PipelineTests`, `WorkflowTests`, `ResultTests`, `ScoringTests`, `ExtensionTests`, `ReportTests`,
`LazyAndResourceTests`, `FileTests`, `NativeTests`, `IntegrationTests`, `AgentTests`, `DispatchTests`.
Required groups: pure functions and validation (`TicketTests`, `PipelineTests`, `ScoringTests`, `AgentTests`), complete workflows
(`IntegrationTests`, `DispatchTests.FullWorkflow_FromFiles_AssignsReportsAndSaves`), file handling
(`FileTests` incl. the empty-path case, `AgentTests.Load_*`), native (`NativeTests`: a real call and a controlled failure).
Total: 83 tests (`dotnet test`; every `[Fact]` and every `[InlineData]` case is counted). Last run (.NET 10.0.12, native library built with gcc 16.2.0): total 83, failed 0, succeeded 83. `FileTests` also checks that a failed export keeps the old report file.

## 10. Help received

I received help in understanding the structure of the project and in writing the reports. I read and tested the code and I am responsible for the submitted work.

## 11. Limitations

* Console application only. Assignments are kept in memory during a run and saved with menu item 7; they are not loaded back on the next start.
* The native library must be built for the same architecture as .NET (x64). Without it the cost report fails with `native.unavailable` (this is a controlled failure, not a crash).
* No external libraries except xUnit.
