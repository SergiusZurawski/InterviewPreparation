# My Review — Project 01 (PR #212)

**Time spent:** __ min

## Findings

<!-- Copy this block for each finding. Severity: Blocker / Should fix / Nit / Question -->

### 1. I think scope might be incorrect builder.Services.AddSingleton<IActivityService, ActivityService>();
- **Where:** `Program.cs:line19`
- **Severity:* medim*
- **Problem:* creates only signle service ActivityService
- **Impact (what goes wrong, for whom):**
- **Fix:** I assume socped. Why, i am not sure explain to me 

### 2. we might be missing  app.UseRouting();
- **Where: Program.cs**
- **Severity:* significant*
- **Problem:* path/routes might be broken*
- **Impact:**
- **Fix:* add app.UseRouting()*


### 3. ActivityController.cs we might be missing annotaiton [Authorizaton] or [Authentication] my mememory is weak.
- **Where:*ActivityController  line 8-9*
- **Severity:* might be sever ,*
- **Problem:* IDOR, access to object wihout permion*
- **Impact:* not sure*
- **Fix:** Add anonations


### 4. we don't do validation of input nor line 19 nor 43 
- **Where:*ActivityController line 19 and 43*
- **Severity:* moderate*
- **Problem:* *
- **Impact:* *
- **Fix:* add validation of imput, null verificaitons etc.*


### 5 Logger is absent .
- **Where:* ActivityController*
- **Severity:*moderate *
- **Problem:*logger is absent is it an anty-pattern*
- **Impact:**
- **Fix:* add logger so whe validation fails we can log issue*

### 6.  change to async await, and then in controller as well var saved = _db.SaveChangesAsync().Result; in ActivityService
- **Where:*ActivityService*
- **Severity:* High*
- **Problem:* Old wait of interaction is Taks API *
- **Impact:**
- **Fix:*add exception handling *

### 7. Found context Ignore this - I don't see DBContext to be initialized however that might be my lack of knowledge of EF.
- **Where:* Project*
- **Severity:* Severe*
- **Problem:*DBContext is not initialized , there is no Migrations etc*
- **Impact:* call to DB might now work*
- **Fix:* Add db context * 

### 8. ToList is called too early.
- **Where:* ActivityService line 58*
- **Severity:*moderate*
- **Problem:* we are retrieving data before filtering*
- **Impact:* to much data*
- **Fix:* remove the line*

### 9. ! in ActivityService line 68
- **Where:* ActivityService line 68*
- **Severity:**
- **Problem:* ight be type , might be even left by me by misatke*
- **Impact:**
- **Fix:*remove*

### 10.wrong way of handling raw sql
- **Where:* ActivityService line 76*
- **Severity:*High*
- **Problem:* SQL won't be tracked be EF*
- **Impact:**
- **Fix:* i think recommended way is string format or someting like that*

### 11.  This might not work            .OrderByDescending(p => p.EventCount)
- **Where:* ActivityService line 94*
- **Severity:**
- **Problem:**
- **Impact:**
- **Fix:**

### 11.  No Unit Tests. We can mock mproably DB call and tests for exaple service ActivityService
- **Where:* ToL*
- **Severity:**
- **Problem:**
- **Impact:**
- **Fix:**


## Missing tests I'd ask for

-

## Questions for the author

-

## Verdict

Approve / Approve with comments / Request changes, because:
