# Spaces capability matrix

Status values: PASS, PARTIAL, MISSING, BLOCKED, UNKNOWN.

| Capability | Shared/domain | HUI | Linux status | Windows status | Notes |
|---|---|---|---|---|---|
| Built-in Spaces | PASS | PASS | BUILD GREEN | MISSING | Study/Shopping/Research/Agent reconciled from persistent state |
| Create custom Space | PASS | PASS | BUILD GREEN | MISSING | persistent registry + New Space HUI |
| Edit Space | PASS | PASS | BUILD GREEN | MISSING | name/description/model/instructions/thinking + one HUI example pair |
| Archive/restore | PASS | PASS | BUILD GREEN | MISSING | HUI can show archived records and restore selected Space |
| Fork | PASS | PASS | BUILD GREEN | MISSING | fork origin persisted; built-ins become editable custom copies |
| Delete custom safely | PASS | PASS | BUILD GREEN | MISSING | conversations detached first; built-in delete rejected |
| Attached files persist | PASS | PASS | PARTIAL | MISSING | metadata + permissions persist; real graphical file-picker adapter pending |
| Current Space scope | PASS | N/A | BUILD GREEN | MISSING | persisted and cleared by Home/unscoped Chat |
| Space conversation list | PASS | PASS | BUILD GREEN | MISSING | persistent standalone store proven; authoritative Chat repo adapter pending |
| New Chat assigned to Space | PASS | PASS | BUILD GREEN | MISSING | shared service creates assigned record |
| Reopen Space exposes conversations | PASS | PASS | BUILD GREEN | MISSING | persistent reopen + most-recent launch proven |
| Study routing | PASS | PASS | PARTIAL | MISSING | contract maps Study; real Study shell adapter not present in checkout |
| Tasks routing | PASS | PASS | PARTIAL | MISSING | Agent maps Tasks; real Tasks shell adapter not present in checkout |
| Research routing | PASS | PASS | PARTIAL | MISSING | Research maps configured workspace; real Chat shell adapter pending |
| Preferred model | PASS | PASS | BUILD GREEN | MISSING | preferred model + availability fallback tested |
| Instructions/context | PASS | PASS | BUILD GREEN | MISSING | launch context composed from purpose/instructions/examples/files |
| Thinking mode | PASS | PASS | BUILD GREEN | MISSING | persisted and carried in launch plan |
| Examples | PASS | PARTIAL | PARTIAL | MISSING | domain supports many; HUI editor currently exposes one pair |
| Generated surfaces | PASS | PARTIAL | PARTIAL | MISSING | data persists + HUI summary; live trusted renderer pending |
| Layout document | PASS | PARTIAL | PARTIAL | MISSING | typed nodes/ports/edges persist + layout action contract; graphical editor host pending |
| Edit with Cake planning | MISSING | MISSING | MISSING | MISSING | planner integration not implemented |
| Responsive/compact HUI | PASS | PASS | BUILD GREEN | MISSING | compact mode implemented; graphical visual QA pending |
| Linux graphical launch | N/A | PASS scene | BLOCKED | N/A | authoritative CakeOS Chat/Study/Tasks shell services are absent from current checkout |
| Linux graphical screenshot | N/A | N/A | MISSING | N/A | no runtime screenshot claim |
| Windows QA executable | N/A | shared HUI ready | N/A | MISSING | create only after Linux shell integration |
