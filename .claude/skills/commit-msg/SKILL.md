---
name: commit-msg
description: Write a commit message for the current changes and print it as text only — never runs the commit. Use whenever the user asks for a commit message, "اديني نص commit", "اكتبلي رسالة الكوميت", "commit message", "نص الكوميت", or wants to know how to describe the work they just did.
allowed-tools: Bash(git status:*), Bash(git diff:*), Bash(git log:*), Bash(git show:*), Read, Grep, Glob
model: sonnet
---

# Commit Message Writer

Produce a commit message for the work currently in the working tree. **Output text only.**

## Hard rules

1. **NEVER create the commit.** Do not run `git commit`, `git add`, `git stage`, `git push`, `git tag`, `git reset`, `git checkout`, or any other command that changes repository or index state. Read-only git commands only.
2. **Never offer to commit** and never ask "should I commit this for you?". The user runs the commit themselves. Ending the response with an offer to commit is a failure.
3. **The message is the deliverable.** Print it in a single fenced code block so it can be copied straight into `git commit -F` or the editor. No preamble longer than one line.

## Procedure

1. Inspect the changes, read-only:
   - `git status`
   - `git diff --staged --stat` and `git diff --stat`
   - Full diffs for the changed files (`git diff --staged`, `git diff`)
   - For untracked files listed by `git status`, `Read` them directly — `git diff` does not show them.
2. **Scope rule:** if anything is staged, describe **only the staged changes**. If nothing is staged, describe all uncommitted changes (modified + untracked). Say in one line which scope you used.
3. Match the repo's existing style — run `git log --oneline -15` and follow what you see:
   - the language of past subjects (this repo writes commits in English)
   - whether Conventional Commits prefixes are used (`feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`)
   - whether a ticket/task id is appended to the subject, e.g. `(TASK-004)` — if the diff touches a task file such as `mentor/tasks/TASK-00X.md`, or the branch name carries an id, include it the same way
4. Write the message.

## Message shape

```
<type>: <subject — imperative mood, ≤ 72 chars, no trailing period>

<body: wrapped at ~72 chars. WHAT changed and WHY, not a file list.
Group related changes; drop trivia like formatting-only edits.>
```

- **Subject** — imperative ("add", "convert", "fix"), never past tense ("added") or gerund ("adding").
- **Body** — only when the change needs a reason or has more than one meaningful part. A genuinely small change gets a subject line alone.
- **Never** mention Claude, AI assistance, or add a `Co-Authored-By` trailer. This is the user's commit.
- Add `BREAKING CHANGE: <what breaks>` as a final paragraph when the public API/contract changes (e.g. a request or response field changes type).

## When the changes are unrelated

If the working tree mixes genuinely independent changes, do not force one message. Say so briefly, then give a suggested split — for each part, the files it covers and its own message — so the user can stage and commit them separately.

## Response format

Keep it to: one line naming the scope you described → the code block → at most one line of notes (e.g. a suggested split, or a fact you could not infer such as a ticket id). Nothing else.
