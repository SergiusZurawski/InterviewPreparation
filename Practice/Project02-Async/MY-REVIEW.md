# My Review — Project 02 (PR #231)

**Time spent:** __ min

## Checklist (write this BEFORE reading the code: one line per AC and convention)

- [ ] AC1:
- [ ] AC2:
- [ ] AC3:
- [ ] AC4:
- [ ] AC5:
- [ ] AC6:
- [ ] AC7:
- [ ] Conventions:

## Findings

<!-- Copy this block for each finding. Severity: Blocker / Should fix / Nit / Question -->

### 1. POST /api/sync/{vendorId}  - vendor ID might be IDOR.  ANyone can specify vendor ID 
- **Where:*SyncController line 33*  '`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**

### 2. reverse order of DateTime.UtcNow - startedUtc
- **Where:** `UswageSyncWorker.cs:line47`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**

### 3. Exception will buble up break requriements that expception should not break backgroundworker.
- **Where:** `UsageSyncWorker.cs:line41-43`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 4. Cancelation Token None, we probably hould have some limit to avoid hanging and blocking worker
- **Where:** `UsageSyncWorker.cs:line41`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 5. infiite loop  for (var attempt = 1; ; attempt++)  ,  whre is excit condition , ? 
- **Where:** `VendorUsageClient.cs:line30`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 6. var url = $"{vendor.BaseUrl.TrimEnd('/')}/v2/usage?since={sinceUtc:yyyy-MM-ddTHH:mm:ssZ}"; url/path propaby should be in config
- **Where:** `File.cs:line`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 7. ancellationToken.None  again can hang the request.
- **Where:** `SyncController.cs:line 50`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 8. Task.Run( we need to await that taks, it will just schudule task but there is chance that it will never get executed
- **Where:** `SyncController:line45`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 1. <short title>
- **Where:** `File.cs:line`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 1. <short title>
- **Where:** `File.cs:line`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 1. <short title>
- **Where:** `File.cs:line`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 1. <short title>
- **Where:** `File.cs:line`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 1. <short title>
- **Where:** `File.cs:line`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**
### 1. <short title>
- **Where:** `File.cs:line`
- **Severity:**
- **Problem:**
- **Impact (what goes wrong, for whom, when):**
- **Fix:**

## Missing tests I'd ask for

-

## Questions for the author

-

## Verdict

Approve / Approve with comments / Request changes, because:
