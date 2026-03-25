# 🧪 Testing Guide - Frontend POC

**Objective:** Validate all 4 user role workflows work correctly in the proof-of-concept

**Time Required:** ~15 minutes for full test suite

---

## Prerequisites

✅ Application running at `https://localhost:7001/`  
✅ No compilation errors (build successful)  
✅ All 5 test users available (see below)  

---

## Test User Credentials

Copy these for quick testing:

| Test | MKAN ID | Password | Expected Dashboard |
|------|---------|----------|-------------------|
| Officer | 10001 | (any) | Single section, Edit Form button |
| President | 10002 | (any) | 9 dept grid, Approve button |
| State GS | 20001 | (any) | Stat cards, Units table |
| National GS | 30001 | (any) | System stats, Escalations |
| National Pres | 30002 | (any) | Same as National GS |

---

## Test Case 1: Login Page (5 minutes)

### 1.1 Page Load
- [ ] Open `https://localhost:7001/`
- [ ] See Login page with gradient background
- [ ] See "AMSA Reporting" heading
- [ ] See form with MKAN ID and password fields
- [ ] See blue "Login" button
- [ ] See help text with test credentials

### 1.2 Invalid Login
- [ ] Enter MKAN ID: `99999`, Password: `test`
- [ ] Click Login
- [ ] See error alert: "Invalid MKAN ID or password"
- [ ] Stay on Login page

### 1.3 Successful Login (Officer)
- [ ] Enter MKAN ID: `10001`, Password: `test`
- [ ] Click Login
- [ ] See "Logging in..." spinner briefly
- [ ] Redirected to `/dashboard`
- [ ] See navbar with "AMSA Reporting" and user dropdown
- [ ] See heading "Welcome, Ahmad!"

### 1.4 Logout
- [ ] Click user dropdown (top-right)
- [ ] See name "Ahmad Hassan" and role "Department Officer"
- [ ] Click "Logout"
- [ ] Redirected back to Login page
- [ ] Navbar disappeared

**Status:** ✅ PASS / ❌ FAIL

---

## Test Case 2: Officer Dashboard (3 minutes)

### 2.1 Dashboard Load (Officer)
- [ ] Login with MKAN ID: `10001`
- [ ] See gradient header "Welcome, Ahmad!"
- [ ] See role description: "Department Officer"
- [ ] See DepartmentOfficerDashboard component

### 2.2 Officer Dashboard Components
- [ ] See "Reporting Cycle: January 2025" info box
- [ ] See "Deadline: February 7, 2025" in orange
- [ ] See **1 section card** labeled "Islamic Education (Taleem)"
- [ ] See status badge "Draft" in yellow
- [ ] See "Edit Form →" button on section card
- [ ] See Progress bar showing "1 / 9 sections"
- [ ] See Notifications sidebar (top right) with 1 mock notification
- [ ] Notification reads: "Ready: Submit your department report"

### 2.3 Edit Form Navigation
- [ ] Click "Edit Form →" button on Taleem section
- [ ] Navigate to `/report/taleem` page
- [ ] See card header "Islamic Education (Taleem) Report - January 2025"
- [ ] See form with input fields

**Status:** ✅ PASS / ❌ FAIL

---

## Test Case 3: Taleem Report Form (4 minutes)

### 3.1 Form Fields Present
- [ ] See "Sessions Organized" input (number)
- [ ] See "Total Member Count" input (number)
- [ ] See "Average Attendance Count" input (number)
- [ ] See "Session Duration (minutes)" input
- [ ] See "Monthly Test Count" input
- [ ] See "Additional Notes" textarea
- [ ] See "Submit Report" button (disabled or visible)

### 3.2 Compliance - Test Compliant
- [ ] Enter Sessions: **5**
- [ ] Enter Total Members: **40**
- [ ] Enter Average Attendance: **35**
- [ ] Watch attendance % calculate: **35 ÷ 40 = 87.5%**
- [ ] See badge: **"✓ Sessions Compliant (≥4)"**
- [ ] See badge: **"✓ Attendance Compliant (≥75%)"**
- [ ] See: **"✓ COMPLIANT"** (green)

### 3.3 Compliance - Test Non-Compliant (Sessions)
- [ ] Clear Sessions field
- [ ] Enter Sessions: **2**
- [ ] See badge: **"⚠ Sessions Below Threshold (≥4)"** (red)
- [ ] See: **"⚠ NOT COMPLIANT"** (yellow)

### 3.4 Compliance - Test Non-Compliant (Attendance)
- [ ] Clear Sessions field again
- [ ] Enter Sessions: **4**
- [ ] Clear Average Attendance
- [ ] Enter Average Attendance: **30**
- [ ] Calculate: **30 ÷ 40 = 75%** (exactly threshold)
- [ ] Should show: **"✓ Attendance Compliant"** (at threshold passes)

### 3.5 Auto-Save Behavior
- [ ] Enter Sessions: **5** (or clear and re-enter)
- [ ] Immediately see "Saving..." message
- [ ] Wait 1.5 seconds
- [ ] See "Saved at" timestamp (e.g., "14:32:45")
- [ ] Timestamp updates when you change another field
- [ ] Try filling Total Members: **50**
- [ ] See save spinner, then new timestamp

### 3.6 Last Save Display
- [ ] Make a change (e.g., Sessions: 6)
- [ ] See "Saving..." spinner
- [ ] After 1.5s, see "Saved at HH:MM:SS"
- [ ] Make another change immediately
- [ ] See new "Saving..." spinner
- [ ] See updated timestamp

### 3.7 Navigation Back
- [ ] Click back arrow or browser back button
- [ ] Return to DepartmentOfficerDashboard
- [ ] Verify you're back on `/dashboard`

**Status:** ✅ PASS / ❌ FAIL

---

## Test Case 4: President Dashboard (3 minutes)

### 4.1 Login as President
- [ ] Click user dropdown, logout
- [ ] Login with MKAN ID: `10002`, Password: `test`
- [ ] See heading "Welcome, Fatima!"
- [ ] See role description: "Unit President"

### 4.2 President Dashboard Components
- [ ] See **9 department cards** in grid layout
- [ ] Cards labeled: Taleem, Tabligh, Welfare, Sport, Finance, Health, SecondarySchool, Tajneed, General
- [ ] See icons on each card (book, megaphone, heart, etc.)
- [ ] See **8 cards with green ✓ badge** "Compliant"
- [ ] See **1 card (Finance) with yellow ⚠ badge** "Needs Review"

### 4.3 Compliance Summary
- [ ] Scroll to "Compliance Summary" section
- [ ] See stat: **"8 Compliant"** (green)
- [ ] See stat: **"1 Needs Review"** (yellow)
- [ ] See stat: **"0 Pending"** (gray)

### 4.4 Timeline Visualization
- [ ] Scroll to "Report Submission Timeline"
- [ ] See 4 timeline points:
  1. **Draft** - Jan 20 (past)
  2. **Submitted** - Jan 28 (past)
  3. **Awaiting Presidential Review** - Today (current)
  4. **Closes** - Feb 7 (future, gray)
- [ ] Current step is highlighted

### 4.5 Presidential Review Form
- [ ] Scroll to "Presidential Review" textarea
- [ ] Type sample notes: "All sections look good except Finance needs clarification on collections."
- [ ] See "Approve & Forward" button
- [ ] See "Reject" button (if available)

### 4.6 Approve & Forward
- [ ] Click "Approve & Forward" button
- [ ] See success message or navigation to state dashboard
- [ ] Report status changes from `SubmittedToPresident` to `ApprovedByPresident`

**Status:** ✅ PASS / ❌ FAIL

---

## Test Case 5: State GS Dashboard (2 minutes)

### 5.1 Login as State GS
- [ ] Logout
- [ ] Login with MKAN ID: `20001`, Password: `test`
- [ ] See heading "Welcome, Hassan!"
- [ ] See role description: "State General Secretary"

### 5.2 State Dashboard Stat Cards
- [ ] See **4 stat cards** at top:
  - **Units in State:** 12
  - **Reports Submitted:** 11
  - **Pending Submissions:** 1
  - **Compliant:** 9
- [ ] Stats have colors: blue, green, yellow, green

### 5.3 Units Table
- [ ] Scroll to "Units Submission Status" table
- [ ] See **12 rows** (one per unit)
- [ ] Columns visible: Unit Name, President Name, Status, Compliance, Actions

### 5.4 Compliance Bars
- [ ] Look at "Compliance" column
- [ ] See visual progress bars (e.g., "8/9 = 89%")
- [ ] Bars have gradient fill (light blue to dark blue)
- [ ] Hover over bar to see percentage

### 5.5 Review Unit
- [ ] Click "Review" button on a row
- [ ] See unit details (or navigate to unit view)

**Status:** ✅ PASS / ❌ FAIL

---

## Test Case 6: National GS Dashboard (2 minutes)

### 6.1 Login as National GS
- [ ] Logout
- [ ] Login with MKAN ID: `30001`, Password: `test`
- [ ] See heading "Welcome, Aisha!"
- [ ] See role description: "National General Secretary"

### 6.2 National Dashboard Stat Cards
- [ ] See **4 large stat cards**:
  - **States:** 36
  - **Units Submitted:** 450
  - **Needs Attention:** 50
  - **Compliance Rate:** 92%
- [ ] Cards styled with gradient backgrounds
- [ ] Numbers are large and prominent

### 6.3 Escalations List
- [ ] Scroll to "Escalations & Issues" section
- [ ] See **list of 4 escalations**:
  1. **Oyo State** - Issue: "Financial crisis"
  2. **Cross River State** - Issue: "Leadership transition"
  3. **Bauchi State** - Issue: "Participation concerns"
  4. **Rivers State** - Issue: "Compliance issues"
- [ ] Each item shows state name, issue, and severity badge

### 6.4 Submission Timeline
- [ ] See **timeline with 4 points**:
  - Opened: Jan 20
  - 450 Submitted: Jan 28
  - Currently Reviewing: Today
  - Deadline: Feb 7
- [ ] Current step highlighted

### 6.5 Top States
- [ ] See **"Top Performing States"** section
- [ ] List shows:
  - Lagos: 98% ⭐
  - Ogun: 96% ⭐
  - Kaduna: 94% ⭐

**Status:** ✅ PASS / ❌ FAIL

---

## Test Case 7: Navigation (2 minutes)

### 7.1 Navbar Links
- [ ] Login with any test user
- [ ] See navbar at top with "AMSA Reporting" logo
- [ ] Click "Dashboard" link → navigates to `/dashboard`
- [ ] See correct dashboard for that user

### 7.2 User Dropdown
- [ ] See user dropdown (top-right)
- [ ] Click to open
- [ ] See user first name displayed
- [ ] See user role displayed
- [ ] See "Logout" option

### 7.3 Mobile Navbar
- [ ] Resize browser window to mobile size (<768px)
- [ ] See hamburger menu icon
- [ ] Click hamburger → menu expands
- [ ] Click link → menu collapses
- [ ] Resize back to desktop → menu normal

**Status:** ✅ PASS / ❌ FAIL

---

## Test Case 8: Role-Based Access (2 minutes)

### 8.1 Officer Can't See President Options
- [ ] Login with MKAN ID: `10001` (Officer)
- [ ] Officer dashboard shows only 1 section
- [ ] No 9-department grid visible
- [ ] No approval form visible

### 8.2 President Can't See National Options
- [ ] Logout
- [ ] Login with MKAN ID: `10002` (President)
- [ ] President dashboard shows 9 departments
- [ ] No "Escalations" list visible
- [ ] No national stats visible

### 8.3 State GS Can't See National Options
- [ ] Logout
- [ ] Login with MKAN ID: `20001` (State GS)
- [ ] See units table (state-level)
- [ ] No escalations visible
- [ ] No "Top Performing States" visible

### 8.4 National GS Sees All
- [ ] Logout
- [ ] Login with MKAN ID: `30001` (National GS)
- [ ] See system-wide stats
- [ ] See escalations
- [ ] See top performing states

**Status:** ✅ PASS / ❌ FAIL

---

## Test Case 9: Edge Cases (2 minutes)

### 9.1 Unauthenticated Access
- [ ] Logout (see Login page)
- [ ] Try to navigate directly to `/dashboard`
- [ ] Browser redirects to `/login`
- [ ] Try to navigate to `/report/taleem`
- [ ] Browser redirects to `/login`

### 9.2 Empty Form Submission
- [ ] Login with MKAN ID: `10001`
- [ ] Click "Edit Form"
- [ ] Leave all fields empty
- [ ] See compliance badges: all red/yellow (non-compliant)

### 9.3 Extreme Values
- [ ] Login with MKAN ID: `10001`
- [ ] Click "Edit Form"
- [ ] Sessions: 999 (way above threshold) → Compliant ✓
- [ ] Average Attendance: 1 (way below) → Non-compliant ⚠
- [ ] See realistic badge updates

### 9.4 Browser Back Button
- [ ] Login
- [ ] Navigate to `/report/taleem`
- [ ] Click back button
- [ ] Return to `/dashboard`
- [ ] Everything works correctly

**Status:** ✅ PASS / ❌ FAIL

---

## Summary Checklist

### Test Cases to Complete
- [ ] Test Case 1: Login Page (5 minutes)
- [ ] Test Case 2: Officer Dashboard (3 minutes)
- [ ] Test Case 3: Taleem Form (4 minutes)
- [ ] Test Case 4: President Dashboard (3 minutes)
- [ ] Test Case 5: State GS Dashboard (2 minutes)
- [ ] Test Case 6: National GS Dashboard (2 minutes)
- [ ] Test Case 7: Navigation (2 minutes)
- [ ] Test Case 8: Role-Based Access (2 minutes)
- [ ] Test Case 9: Edge Cases (2 minutes)

### Overall Assessment
- [ ] All 9 test cases passed
- [ ] No console errors
- [ ] No browser warnings
- [ ] Navigation works smoothly
- [ ] Forms update in real-time
- [ ] Compliance badges work correctly
- [ ] All 5 test users work
- [ ] Responsive design works (mobile, tablet, desktop)

**Total Test Time:** ~20 minutes  
**Pass Criteria:** All checkboxes checked ✓

---

## Reporting Issues

If any test fails:
1. **Note the test case number** (e.g., "Test Case 3.5")
2. **Describe what went wrong** (e.g., "Save timestamp not updating")
3. **Check browser console** for errors (F12)
4. **Take screenshot** for documentation
5. **Report to development team**

### Common Issues & Solutions

| Issue | Likely Cause | Solution |
|-------|--------------|----------|
| "404 Not Found" | Wrong port | Check app console for correct HTTPS port |
| Login fails (empty dashboard) | MockAuthService issue | Check MKAN IDs are 10001, 10002, 20001, 30001, 30002 |
| Compliance badges don't update | Event handler not firing | Try changing another field first |
| Page looks broken | CSS not loaded | Hard refresh browser (Ctrl+Shift+R) |
| Navbar collapsed but no hamburger | Mobile breakpoint issue | Resize browser window |

---

## Success Criteria

✅ **POC is successful if:**
1. All 5 test users can login
2. Each user sees correct dashboard for their role
3. Taleem form shows real-time compliance badges
4. Auto-save debounce works (1.5s delay visible)
5. Navigation between pages works
6. Logout returns to Login page
7. No console errors
8. Responsive design works on mobile

✅ **All criteria met = Frontend POC is production-ready**

---

**Happy Testing! 🎉**

For questions, refer to [QUICK_START_POC.md](./QUICK_START_POC.md) for detailed instructions.
