# Component & Service Interaction Diagrams

## 1. OVERALL ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────────────┐
│                          USER BROWSER                                │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ↓
┌─────────────────────────────────────────────────────────────────────┐
│                   BLAZOR SERVER COMPONENTS                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  App.razor (root)                                                    │
│      ↓                                                               │
│  Routes.razor (router)                                               │
│      ↓                                                               │
│  MainLayout.razor (wraps all pages)                                  │
│      ├─ NavMenu.razor                                                │
│      ├─ ReconnectModal.razor                                         │
│      └─ @Body (page renders here)                                    │
│           │                                                          │
│           ├─ Login.razor (@page "/login")                           │
│           │    └─ AuthenticationStateProvider                       │
│           │                                                          │
│           └─ Dashboard.razor (@page "/dashboard")                   │
│                ├─ Injects: AuthenticationStateProvider              │
│                ├─ Injects: NavigationManager                        │
│                │                                                    │
│                ├─ Gets CurrentUser from AMSAAuthStateProvider       │
│                │                                                    │
│                └─ Conditionally renders:                             │
│                    ├─ NationalDashboard.razor                       │
│                    ├─ StateGSDashboard.razor                        │
│                    ├─ UnitPresidentDashboard.razor                  │
│                    └─ DepartmentOfficerDashboard.razor              │
│                         │                                            │
│                         └─ Navigates to:                             │
│                             DepartmentReportEditor.razor            │
│                             (@page "/report/{DepartmentSlug}")      │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ↓ (OnInitializedAsync)
┌─────────────────────────────────────────────────────────────────────┐
│                   FRONTEND SERVICES (DI)                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  CurrentUserReportService                                            │
│      ├─ Injects: AMSAAuthStateProvider                              │
│      ├─ Injects: UnifiedReportService                               │
│      └─ Methods:                                                    │
│          ├─ ExecuteAsCurrentUserAsync<T>()                          │
│          │   └─ Gets AuthContext, passes to backend                 │
│          ├─ GetMyUnitReportsAsync()                                 │
│          ├─ GetMyStateReportsAsync()                                │
│          ├─ GetMyStateReportAsync()                                 │
│          ├─ SaveDepartmentJsonAsync()                               │
│          ├─ SubmitReportToPresidentAsync()                          │
│          ├─ ApproveAtUnitAsync()                                    │
│          ├─ ApproveAtStateAsync()                                   │
│          ├─ ApproveAtNationalAsync()                                │
│          └─ [etc.]                                                  │
│                                                                      │
│  AMSAAuthStateProvider                                               │
│      ├─ Manages AuthenticationState                                 │
│      ├─ GetCurrentUser() → AuthContext                              │
│      └─ LoginAsync() → Updates state                                │
│                                                                      │
│  NavigationManager                                                   │
│      └─ Nav.NavigateTo("/dashboard")                                │
│                                                                      │
│  Other injected services:                                            │
│      ├─ ReportAccessService (authorization)                        │
│      ├─ AmsaDirectoryLookupCache (name lookups)                    │
│      └─ ILogger                                                     │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ↓ (async calls)
┌─────────────────────────────────────────────────────────────────────┐
│                      BACKEND SERVICES                                │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  UnifiedReportService                                                │
│      ├─ Injects: AMSAReportingDbContext                             │
│      ├─ Injects: ReportAccessService                                │
│      ├─ Injects: IAmsaApiClient                                     │
│      ├─ Injects: ILogger                                            │
│      │                                                              │
│      ├─ Report Operations:                                          │
│      │  ├─ GetReportAsync(reportId)                                │
│      │  ├─ GetDraftAsync(actor, unitId, cycleId)                   │
│      │  ├─ EnsureDraftAsync(actor, unitId, cycleId)                │
│      │  ├─ GetUnitReportsAsync(actor, unitId, cycleId)             │
│      │  ├─ SubmitReportToPresidentAsync(actor, reportId)           │
│      │  └─ [etc.]                                                  │
│      │                                                              │
│      ├─ Department Operations:                                      │
│      │  ├─ SaveDepartmentDataAsync(reportId, dept, json, ...)      │
│      │  ├─ GetDepartmentReportAsync(reportId, dept)                │
│      │  └─ [etc.]                                                  │
│      │                                                              │
│      ├─ State Report Operations:                                    │
│      │  ├─ GetStateReportAsync(actor, stateId, cycleId)            │
│      │  ├─ SaveStateReportAsync(actor, stateReportId, form, ...)   │
│      │  ├─ EnsureStateReportAsync(actor, stateId, cycleId)         │
│      │  ├─ RecomputeStateAggregateAsync(stateReport)               │
│      │  └─ [etc.]                                                  │
│      │                                                              │
│      ├─ Approval Chain:                                             │
│      │  ├─ ApproveByUnitLeadershipAsync()                           │
│      │  ├─ RejectByUnitLeadershipAsync()                            │
│      │  ├─ ApproveByStateLeadershipAsync()                          │
│      │  ├─ RejectByStateLeadershipAsync()                           │
│      │  ├─ AcknowledgeByNationalAsync()                             │
│      │  └─ [etc.]                                                  │
│      │                                                              │
│      └─ Cycle Management:                                           │
│         ├─ GetActiveCycleAsync()                                   │
│         ├─ EnsureActiveCycleAsync()                                │
│         └─ EnsureCycleOpenForEditsAsync()                          │
│                                                                      │
│  ReportAccessService                                                 │
│      ├─ CanEditDepartment(actor, unitId, stateId, dept)            │
│      ├─ CanInitiateReportSubmission(actor, unitId, stateId)        │
│      ├─ CanReviewAtUnitLevel(actor, unitId)                        │
│      ├─ CanReviewAtStateLevel(actor, stateId)                      │
│      ├─ IsNationalLeadership(actor)                                │
│      └─ [etc.]                                                     │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ↓ (Entity Framework queries)
┌─────────────────────────────────────────────────────────────────────┐
│                      ENTITY FRAMEWORK                                │
│                   (AMSAReportingDbContext)                           │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  DbSet<Report>                                                       │
│  DbSet<DepartmentReport>                                             │
│  DbSet<StateReport>                                                  │
│  DbSet<StateReportProgram>                                           │
│  DbSet<ReportingCycle>                                               │
│  DbSet<ReportActivityLog>                                            │
│  DbSet<Member>                                                       │
│  [etc.]                                                              │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              ↓ (SQL queries)
┌─────────────────────────────────────────────────────────────────────┐
│                      DATABASE                                        │
│                  (SQL Server / PostgreSQL)                           │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. DATA FLOW: Department Officer Creating Report

```
DepartmentOfficerDashboard.razor
│
├─ OnInitializedAsync()
│  │
│  ├─ Call: CurrentUserReportService.EnsureActiveCycleAsync()
│  │        ├─ Returns: ReportingCycle
│  │        └─ Display: cycle.CycleMonth, DeadlineLabel
│  │
│  ├─ Call: CurrentUserReportService.GetCurrentUserDraftAsync(cycleId)
│  │        ├─ Checks: Is draft for this cycle?
│  │        ├─ Returns: Report | null
│  │        └─ Display: UI state machine (NoReport, Draft, Submitted, etc.)
│  │
│  └─ Map Report.Status → DashboardState
│
├─ RenderNoReportExistsState() [button: "Create New Report"]
│  │
│  └─ User clicks "Create"
│     │
│     ├─ Call: CurrentUserReportService.CreateCurrentUserDraftAsync(cycleId)
│     │  └─ Backend: UnifiedReportService.EnsureDraftAsync()
│     │     ├─ Create: Report { UnitId, StateId, CycleId, Status=Draft }
│     │     ├─ Create: DepartmentReport × 11 (for each department)
│     │     ├─ Create: ReportActivityLog { Action="ReportCreated" }
│     │     └─ Database: INSERT
│     │
│     └─ Navigate to: /report/taleem
│        (or another department based on user selection)
│
└─ → DepartmentReportEditor.razor


DepartmentReportEditor.razor
│
├─ Route: /report/{DepartmentSlug}
│  └─ Extract: DepartmentSlug = "taleem" → DepartmentType.Taleem
│
├─ OnInitializedAsync()
│  │
│  ├─ Validate department can be edited (auth check)
│  │  └─ Call: CurrentUserReportService.CanEditOwnUnitDepartment(dept)
│  │     ├─ Fetch: CurrentUser
│  │     ├─ Check: ReportAccessService.CanEditDepartment()
│  │     └─ Return: bool (if true, continue; if false, redirect)
│  │
│  ├─ Call: CurrentUserReportService.EnsureActiveCycleAsync()
│  │  └─ Display: CycleLabel, DeadlineLabel
│  │
│  ├─ Call: CurrentUserReportService.GetCurrentUserDraftAsync(cycleId)
│  │  ├─ Returns: Report (must exist or redirect)
│  │  └─ Store: _reportId = report.Id
│  │
│  ├─ Call: CurrentUserReportService.GetDepartmentAsync(reportId, dept)
│  │  ├─ Backend: UnifiedReportService.GetDepartmentReportAsync()
│  │  ├─ Query: SELECT * FROM DepartmentReports WHERE ReportId AND Department
│  │  ├─ Returns: DepartmentReport | null
│  │  └─ Extract: DepartmentReport.ReportData (JSON string)
│  │
│  ├─ Call: LoadDepartmentForm(reportData)
│  │  ├─ Parse: JsonDocument.Parse(reportData)
│  │  ├─ Map: JSON → Taleem form object (if Taleem)
│  │  │   └─ Set: TaleemForm.AttendanceCount, MemberParticipantCount, etc.
│  │  └─ @bind form fields to these properties
│  │
│  └─ Display form with department-specific questions
│
├─ User fills form (e.g., AttendanceCount=50, TotalMemberCount=70)
│  └─ Form fields bound via @bind="Taleem.AttendanceCount"
│
├─ User clicks "Save Draft"
│  │
│  ├─ Call: SaveDraftAsync()
│  │  │
│  │  ├─ Serialize: Taleem object → JSON string
│  │  │  └─ Use: JsonSerializer.Serialize(Taleem)
│  │  │
│  │  ├─ Call: CurrentUserReportService.SaveDepartmentJsonAsync(
│  │  │         reportId, DepartmentType.Taleem, jsonString, markSubmitted=false)
│  │  │  │
│  │  │  ├─ Backend: ExecuteAsCurrentUserAsync()
│  │  │  │  └─ Gets current user context
│  │  │  │
│  │  │  ├─ Backend: UnifiedReportService.SaveDepartmentDataAsync(
│  │  │  │         reportId, dept, json, actor, false)
│  │  │  │
│  │  │  ├─ Validate: JSON format
│  │  │  ├─ Check: Authorization (CanEditDepartment)
│  │  │  │
│  │  │  ├─ Database:
│  │  │  │  ├─ UPDATE DepartmentReports SET ReportData=json, UpdatedAt=now
│  │  │  │  ├─ (IsSubmitted stays false)
│  │  │  │  ├─ INSERT ReportActivityLog { Action="DepartmentReportSaved" }
│  │  │  │  └─ COMMIT
│  │  │  │
│  │  │  └─ Return: DepartmentReport (updated)
│  │  │
│  │  └─ Display: "Draft saved"
│  │
│  └─ Form still editable
│
├─ User clicks "Submit Department"
│  │
│  ├─ Call: SubmitDepartmentAsync()
│  │  │
│  │  ├─ Serialize: Taleem object → JSON string
│  │  │
│  │  ├─ Call: CurrentUserReportService.SaveDepartmentJsonAsync(
│  │  │         reportId, DepartmentType.Taleem, jsonString, markSubmitted=true)
│  │  │  │
│  │  │  ├─ Backend: UnifiedReportService.SaveDepartmentDataAsync(
│  │  │  │         ..., actor, markSubmitted=true)
│  │  │  │
│  │  │  ├─ Database:
│  │  │  │  ├─ UPDATE DepartmentReports SET 
│  │  │  │  │       ReportData=json, 
│  │  │  │  │       IsSubmitted=true, 
│  │  │  │  │       SubmittedAt=now,
│  │  │  │  │       SubmittedByMemberId=actor.MemberId,
│  │  │  │  │       UpdatedAt=now
│  │  │  │  ├─ INSERT ReportActivityLog { Action="DepartmentReportSubmitted" }
│  │  │  │  └─ COMMIT
│  │  │  │
│  │  │  └─ Return: DepartmentReport (submitted)
│  │  │
│  │  └─ Display: "Department submitted"
│  │  └─ Form becomes read-only
│  │
│  └─ This department now appears as "submitted" in UnitPresidentDashboard
│
├─ User clicks "Submit Full Report" (if all depts submitted)
│  │
│  ├─ Check: CanInitiateCurrentUserReportSubmission()
│  │  └─ Verify: User is unit leader or can submit
│  │
│  ├─ Call: CurrentUserReportService.SubmitReportToPresidentAsync(reportId)
│  │  │
│  │  ├─ Backend: UnifiedReportService.SubmitReportToPresidentAsync(
│  │  │         actor, reportId)
│  │  │
│  │  ├─ Check: At least one DepartmentReport.IsSubmitted = true
│  │  │
│  │  ├─ Database:
│  │  │  ├─ UPDATE Report SET 
│  │  │  │       Status="SubmittedToPresident",
│  │  │  │       SubmittedToPresidentAt=now,
│  │  │  │       SubmittedByMemberId=actor.MemberId,
│  │  │  │       UpdatedAt=now
│  │  │  ├─ INSERT ReportActivityLog { Action="ReportSubmittedToPresident" }
│  │  │  └─ COMMIT
│  │  │
│  │  └─ Return: Report (submitted)
│  │
│  └─ Report now appears in UnitPresidentDashboard review queue
│
└─ End: Report submitted → navigates back to dashboard
```

---

## 3. DATA FLOW: Unit President Approving Report

```
Dashboard.razor
│
└─ Renders: <UnitPresidentDashboard CurrentUser="@CurrentUser" />


UnitPresidentDashboard.razor
│
├─ OnInitializedAsync()
│  │
│  ├─ Call: CurrentUserReportService.EnsureActiveCycleAsync()
│  │  └─ Display: cycle.CycleMonth
│  │
│  └─ Call: CurrentUserReportService.GetMyUnitReportsAsync(cycle.Id)
│     │
│     ├─ Backend: ExecuteAsCurrentUserAsync()
│     │  └─ Gets current user context (unit president)
│     │
│     ├─ Backend: UnifiedReportService.GetUnitReportsAsync(
│     │         actor, actor.UnitId, cycleId)
│     │
│     ├─ Check: Authorization (CanReviewAtUnitLevel)
│     │
│     ├─ Query: SELECT * FROM Report 
│     │          WHERE UnitId = actor.UnitId AND CycleId = cycleId
│     │  INCLUDE DepartmentReports
│     │  ORDER BY UpdatedAt DESC
│     │
│     ├─ Returns: List<Report>
│     │  ├─ Report #101
│     │  │  ├─ Status: SubmittedToPresident
│     │  │  ├─ SubmittedToPresidentAt: 2026-05-04 10:30
│     │  │  ├─ DepartmentReports:
│     │  │  │  ├─ Taleem (IsSubmitted=true)
│     │  │  │  ├─ Tabligh (IsSubmitted=true)
│     │  │  │  ├─ Welfare (IsSubmitted=false)
│     │  │  │  └─ [others...]
│     │  │  └─ Submitted depts: 2/11
│     │  │
│     │  └─ Report #102
│     │     ├─ Status: SubmittedToPresident
│     │     ├─ [etc.]
│     │
│     └─ Store: Reports = [Report #101, Report #102, ...]
│
├─ Display table:
│  │
│  │  Report ID │ Status │ Submitted │ Depts Submitted │ Note │ Actions
│  │  ────────────────────────────────────────────────────────────────
│  │  #101      │ ⓘ     │ 2026-05-04│ 2/11           │ [note]│ [Approve/Reject]
│  │  #102      │ ⓘ     │ 2026-05-04│ 3/11           │ [note]│ [Approve/Reject]
│  │
│  └─ NotesByReport[reportId] = "" (empty for new notes)
│
├─ User fills note: "Looks good, forwarding to state"
│  └─ NotesByReport[101] = "Looks good..."
│
├─ User clicks "Approve + Forward"
│  │
│  ├─ Call: Approve(reportId=101)
│  │  │
│  │  ├─ Call: CurrentUserReportService.ApproveAtUnitAsync(
│  │  │         reportId, notes="Looks good...")
│  │  │
│  │  ├─ Backend: ExecuteAsCurrentUserAsync()
│  │  │  └─ Gets current user (unit president)
│  │  │
│  │  ├─ Backend: UnifiedReportService.ApproveByUnitLeadershipAsync(
│  │  │         actor, reportId, notes)
│  │  │
│  │  ├─ Check: Authorization (CanReviewAtUnitLevel)
│  │  ├─ Check: Report.Status in [Draft, SubmittedToPresident, RejectedByPresident]
│  │  ├─ Check: At least 1 DepartmentReport.IsSubmitted
│  │  │
│  │  ├─ Database:
│  │  │  ├─ UPDATE Report SET
│  │  │  │       Status="SubmittedToState",
│  │  │  │       ApprovedByPresidentAt=now,
│  │  │  │       ApprovedByPresidentMemberId=actor.MemberId,
│  │  │  │       PresidentialNotes=notes,
│  │  │  │       UpdatedAt=now
│  │  │  ├─ INSERT ReportActivityLog { Action="ApprovedByUnitLeadership" }
│  │  │  └─ COMMIT
│  │  │
│  │  └─ Return: Report (now with Status=SubmittedToState)
│  │
│  ├─ Call: LoadAsync() [refresh table]
│  │  └─ Report #101 now shows Status="SubmittedToState"
│  │  └─ Action buttons disappear (no longer in "SubmittedToPresident" state)
│  │
│  └─ Display: "Report #101 approved and forwarded to state."
│
├─ Alternative: User clicks "Reject"
│  │
│  ├─ Call: Reject(reportId=101)
│  │  │
│  │  ├─ Call: CurrentUserReportService.RejectAtUnitAsync(
│  │  │         reportId, notes="Need more detail...")
│  │  │
│  │  ├─ Backend: UnifiedReportService.RejectByUnitLeadershipAsync(
│  │  │         actor, reportId, notes)
│  │  │
│  │  ├─ Check: Report.Status in [SubmittedToPresident, Draft]
│  │  │
│  │  ├─ Database:
│  │  │  ├─ UPDATE Report SET
│  │  │  │       Status="RejectedByPresident",
│  │  │  │       PresidentialNotes=notes,
│  │  │  │       UpdatedAt=now
│  │  │  ├─ INSERT ReportActivityLog { Action="RejectedByUnitLeadership" }
│  │  │  └─ COMMIT
│  │  │
│  │  └─ Return: Report (now with Status=RejectedByPresident)
│  │
│  ├─ Call: LoadAsync() [refresh]
│  │  └─ Report #101 now shows Status="RejectedByPresident"
│  │  └─ Department officer sees it can edit again
│  │
│  └─ Display: "Report #101 rejected to unit officer(s)."
│
└─ Report #101 now queues in StateGSDashboard (if approved)
```

---

## 4. DATA FLOW: State Leadership Submitting State Report

```
Dashboard.razor
│
└─ Renders: <StateGSDashboard CurrentUser="@CurrentUser" />


StateGSDashboard.razor
│
├─ OnInitializedAsync() / LoadAsync()
│  │
│  ├─ Call: CurrentUserReportService.EnsureActiveCycleAsync()
│  │  └─ Display: cycle information
│  │
│  ├─ Call: CurrentUserReportService.GetMyStateReportsAsync(cycle.Id)
│  │  │
│  │  ├─ Backend: UnifiedReportService.GetStateReportsAsync(
│  │  │         actor, actor.StateId, cycleId)
│  │  │
│  │  ├─ Query: SELECT * FROM Report 
│  │  │          WHERE StateId = actor.StateId AND CycleId = cycleId
│  │  │  INCLUDE DepartmentReports
│  │  │
│  │  └─ Returns: List<Report> (these are UNIT reports, not state report)
│  │  └─ Store: Reports = [unit reports to review]
│  │
│  ├─ Call: CurrentUserReportService.EnsureMyStateReportAsync(cycle.Id)
│  │  │
│  │  ├─ Backend: UnifiedReportService.EnsureStateReportAsync(
│  │  │         actor, actor.StateId, cycleId)
│  │  │
│  │  ├─ Query: SELECT * FROM StateReport 
│  │  │          WHERE StateId = actor.StateId AND CycleId = cycleId
│  │  │
│  │  ├─ If not found:
│  │  │  ├─ Create: StateReport { StateId, CycleId, Status=Draft }
│  │  │  ├─ Call: RecomputeStateAggregateAsync()
│  │  │  │  └─ Query all unit reports in this state/cycle
│  │  │  │  └─ Count: Total units, submitted units
│  │  │  │  └─ Calculate: UnitPerformanceRating
│  │  │  │  └─ Aggregate: DepartmentReports by department
│  │  │  │  └─ Create: StateReportProgram × N (auto from depts)
│  │  │  │
│  │  │  └─ INSERT StateReport, StateReportProgram entries
│  │  │
│  │  ├─ Returns: StateReport (fetched with Programs included)
│  │  │  ├─ StateReportId, StateId, CycleId
│  │  │  ├─ Status, SubmittedAt, etc.
│  │  │  ├─ UnitPresidentsAttended, TotalUnitPresidents
│  │  │  ├─ UnitPerformanceRating
│  │  │  ├─ ChallengesFaced, NationalSupportNeeded, OtherNotes
│  │  │  └─ Programs (aggregated from submitted depts)
│  │  │
│  │  └─ Store: CurrentStateReport = [state report]
│  │
│  ├─ Call: StateReportFormMapper.ToForm(CurrentStateReport)
│  │  ├─ Convert: StateReport entity → StateReportForm
│  │  ├─ Extract fields to editable form
│  │  └─ Store: StateForm = [form object]
│  │
│  └─ Display:
│     ├─ State report form with fields
│     ├─ Programs list (auto-aggregated, can edit)
│     ├─ Unit reports review table below
│     └─ Save/Submit buttons
│
├─ User fills state form:
│  │  ├─ UnitPresidentsAttended: 15
│  │  ├─ TotalUnitPresidents: 18
│  │  ├─ UnitPerformanceRating: 85
│  │  ├─ UnitImprovementPlan: "...plans..."
│  │  ├─ ChallengesFaced: "...challenges..."
│  │  ├─ NationalSupportNeeded: "...support..."
│  │  ├─ OtherNotes: "...notes..."
│  │  └─ Programs:
│  │     ├─ Program 1: Taleem, objectives, outcomes, attendance, beneficiaries
│  │     ├─ Program 2: Tabligh, ...
│  │     └─ [etc.]
│  │
│  └─ @bind form fields to StateForm properties
│
├─ User clicks "Save Draft"
│  │
│  ├─ Call: SaveStateDraftAsync()
│  │  │
│  │  ├─ Call: CurrentUserReportService.SaveMyStateReportAsync(
│  │  │         stateReportId=12, form=StateForm, markSubmitted=false)
│  │  │
│  │  ├─ Backend: ExecuteAsCurrentUserAsync()
│  │  │  └─ Gets current user (state leadership)
│  │  │
│  │  ├─ Backend: UnifiedReportService.SaveStateReportAsync(
│  │  │         actor, stateReportId, form, markSubmitted=false)
│  │  │
│  │  ├─ Check: Authorization (CanReviewAtStateLevel)
│  │  ├─ Check: Status not SubmittedToNational or Acknowledged
│  │  ├─ Validate: Form data (no negative numbers, etc.)
│  │  │
│  │  ├─ Database:
│  │  │  ├─ UPDATE StateReport SET
│  │  │  │       UnitPresidentsAttended=form.UnitPresidentsAttended,
│  │  │  │       TotalUnitPresidents=form.TotalUnitPresidents,
│  │  │  │       UnitPerformanceRating=form.UnitPerformanceRating,
│  │  │  │       UnitImprovementPlan=form.UnitImprovementPlan,
│  │  │  │       ChallengesFaced=form.ChallengesFaced,
│  │  │  │       NationalSupportNeeded=form.NationalSupportNeeded,
│  │  │  │       OtherNotes=form.OtherNotes,
│  │  │  │       UpdatedAt=now
│  │  │  │  (NOTE: Status stays Draft, no submission timestamp)
│  │  │  │
│  │  │  ├─ DELETE existing StateReportProgram entries
│  │  │  ├─ INSERT new StateReportProgram entries from form.Programs
│  │  │  │
│  │  │  └─ COMMIT
│  │  │
│  │  └─ Display: "Draft saved"
│  │
│  └─ Form remains editable
│
├─ User clicks "Submit State Report"
│  │
│  ├─ Call: SubmitStateReportAsync()
│  │  │
│  │  ├─ Call: CurrentUserReportService.SaveMyStateReportAsync(
│  │  │         stateReportId=12, form=StateForm, markSubmitted=true)
│  │  │
│  │  ├─ Backend: UnifiedReportService.SaveStateReportAsync(
│  │  │         actor, stateReportId, form, markSubmitted=true)
│  │  │
│  │  ├─ Database:
│  │  │  ├─ UPDATE StateReport SET
│  │  │  │       Status="SubmittedToNational",
│  │  │  │       SubmittedAt=now,
│  │  │  │       SubmittedByMemberId=actor.MemberId,
│  │  │  │       ... (and form fields as above)
│  │  │  │
│  │  │  ├─ INSERT StateReportProgram entries (updated list)
│  │  │  │
│  │  │  └─ COMMIT
│  │  │
│  │  └─ Return: StateReport (submitted)
│  │
│  ├─ Update: CurrentStateReport.Status = SubmittedToNational
│  ├─ Set: StateReportSubmitted = true
│  ├─ Form becomes read-only (DisableStateFormActions = true)
│  │
│  └─ Display: "State report submitted to national leadership"
│
├─ User then approves/rejects individual unit reports:
│  │
│  ├─ Unit reports table (Reports list)
│  │
│  ├─ For each report in SubmittedToState status:
│  │  │
│  │  ├─ User can fill note, click "Approve + Forward" or "Reject"
│  │  │
│  │  ├─ Approve:
│  │  │  │
│  │  │  ├─ Call: Approve(reportId=101)
│  │  │  │  │
│  │  │  │  ├─ Call: CurrentUserReportService.ApproveAtStateAsync(
│  │  │  │  │         reportId, notes)
│  │  │  │  │
│  │  │  │  ├─ Backend: UnifiedReportService.ApproveByStateLeadershipAsync(
│  │  │  │  │         actor, reportId, notes)
│  │  │  │  │
│  │  │  │  ├─ Check: Status == SubmittedToState (REQUIRED)
│  │  │  │  │
│  │  │  │  ├─ Database:
│  │  │  │  │  ├─ UPDATE Report SET
│  │  │  │  │  │       Status="SubmittedToNational",
│  │  │  │  │  │       ApprovedByStateAt=now,
│  │  │  │  │  │       ApprovedByStateMemberId=actor.MemberId,
│  │  │  │  │  │       StateNotes=notes,
│  │  │  │  │  │       UpdatedAt=now
│  │  │  │  │  ├─ INSERT ReportActivityLog
│  │  │  │  │  └─ COMMIT
│  │  │  │  │
│  │  │  │  └─ Report now queues in NationalDashboard
│  │  │  │
│  │  │  └─ Call: LoadAsync() [refresh]
│  │  │     └─ Report #101 disappears from action queue
│  │  │
│  │  └─ Reject (similar, Status→RejectedByState)
│  │
│  └─ All approved reports appear in NationalDashboard
│
└─ State report submitted, unit reports flowing to national
```

---

## 5. Authorization Check Points

```
FRONTEND (Informational - no security)
├─ Login.razor
│  └─ Check: User authenticated? (if not, show login form)
│
├─ Dashboard.razor
│  └─ Check: Has CurrentUser? (if not, redirect to /login)
│  └─ Render correct dashboard based on role
│
├─ DepartmentOfficerDashboard.razor
│  └─ Check: CanInitiateCurrentUserReportSubmission? (hide button if false)
│
├─ DepartmentReportEditor.razor
│  └─ Check: CanEditOwnUnitDepartment? (redirect if false)
│
└─ StateGSDashboard.razor
   └─ Check: CanInitiateCurrentUserReportSubmission? (hide button if false)


BACKEND (Security-Critical - ALWAYS enforced)
├─ UnifiedReportService.GetUnitReportsAsync()
│  └─ Check: _access.CanReviewAtUnitLevel(actor, unitId)
│     └─ Throws: UnauthorizedAccessException if false
│
├─ UnifiedReportService.GetStateReportsAsync()
│  └─ Check: _access.CanReviewAtStateLevel(actor, stateId)
│     └─ Throws: UnauthorizedAccessException if false
│
├─ UnifiedReportService.SaveDepartmentDataAsync()
│  └─ Check: _access.CanEditDepartment(actor, unitId, stateId, dept)
│     └─ Throws: UnauthorizedAccessException if false
│
├─ UnifiedReportService.ApproveByUnitLeadershipAsync()
│  └─ Check: _access.CanReviewAtUnitLevel(actor, unitId)
│     └─ Throws: UnauthorizedAccessException if false
│
├─ UnifiedReportService.ApproveByStateLeadershipAsync()
│  └─ Check: _access.CanReviewAtStateLevel(actor, stateId)
│     └─ Throws: UnauthorizedAccessException if false
│
├─ UnifiedReportService.AcknowledgeByNationalAsync()
│  └─ Check: _access.IsNationalLeadership(actor)
│     └─ Throws: UnauthorizedAccessException if false
│
└─ [All methods validate cycleId, reportId exist in database]
   └─ Throws: InvalidOperationException if not found
```

---

## 6. Component State Transitions

```
                        ┌─────────────┐
                        │  Nonexist   │ (User hasn't created)
                        │   ent       │
                        │  Report     │
                        └──────┬──────┘
                               │ CreateCurrentUserDraftAsync()
                               ↓
                    ┌──────────────────────┐
                    │  Report Created      │
                    │  Status: Draft       │
                    │  DepartmentReports:  │
                    │  All 11 depts        │
                    │  IsSubmitted: false  │
                    └──────────┬───────────┘
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
        │ (Add depts)          │ (Submit full)        │
        │                      │                      │
        ↓                      ↓                      ↓
   ┌─────────┐      ┌──────────────────┐      ┌──────────────┐
   │ Draft   │      │ SubmittedTo      │      │  Rejected    │
   │(edit)   │      │ President        │      │  ByPresident │
   │         │      │(in review)       │      │(revise)      │
   └────┬────┘      └────┬─────────────┘      └──────┬───────┘
        │                │                          │
        └────────────────┼──────────────────────────┘
                         │
              ApproveByUnitLeadershipAsync()
              RejectByUnitLeadershipAsync() ← stays in Draft/Submitted
                         │
                         ↓
            ┌─────────────────────────────┐
            │ SubmittedToState            │
            │ (forwarded to state)        │
            └────────────┬────────────────┘
                         │
        ┌────────────────┴────────────────┐
        │                                 │
        │ ApproveByStateLeadershipAsync() │ RejectByStateLeadershipAsync()
        │ (forward to national)           │ (send back)
        ↓                                 ↓
   ┌──────────────┐                  ┌──────────────┐
   │SubmittedTo   │                  │  RejectedBy  │
   │ National     │                  │    State     │
   │(in review)   │                  │  (revise)    │
   └───────┬──────┘                  └──────┬───────┘
           │                                │
           │ AcknowledgeByNationalAsync()   │
           │                                │
           ↓                                ↓
    ┌────────────┐              (Cycle: loops back to revision)
    │Acknowledged│
    │(complete)  │
    └────────────┘
```

This comprehensive diagram set shows how all the components and services work together in the AMSA Reporting System.
