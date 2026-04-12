---
name: Coding Standards
description: Coding guidelines for CTRM project — KISS/YAGNI, one class per file, Dapper, clean naming conventions
type: feedback
---

**Design Philosophy:**
- KISS and YAGNI — don't over-architect, don't add needless abstraction layers
- DRY — refactor repeated code into methods/utilities/extensions
- "Code first" — prioritize code over SQL. Avoid massive SQL stored procedures. SQL can become a DB bottleneck; code scales out.
- Code must be easy to follow and debug for less experienced developers

**Classes:**
- ONE class per file (no exceptions)
- Using statements go OUTSIDE the namespace

**C# Methods:**
- PascalCase for functions, properties, events, class names
- Intuitively named based on functionality (e.g., ReadDataRowsFromExcelFile)

**C# Variables:**
- camelCase for variables
- No underscore prefix or m_ prefix on variables/enums
- Descriptive names (countOfFailedRecords not x or numRec)
- Avoid `var` — use explicit types unless the type is not expressible (e.g., anonymous types)

**C# Return Statements:**
- A blank line must precede the final `return` statement in a method/block
- Exception: no blank line needed if the `return` is inside an `if` statement

**C# Control Flow:**
- Always use braces `{ }` for `if`, `else`, `foreach`, `for`, `while` — even for single-line bodies
- No braceless one-liners (avoids subtle bugs when someone adds a second statement later)

**Vala Code**
Follow best practices for coding style

**Database Access:**
- Use Dapper for data access in C# / Windows
- All entity DB calls go in repository/service classes
- AVOID DB calls inside loops — batch fetch before loops
- Build repository methods to be efficient
- Focus on performance, Dapper is first choice but write straight SQL if needed

**How to apply:** Follow these rules for all new code
