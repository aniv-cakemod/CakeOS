# Spaces capability matrix

Status values: PASS, PARTIAL, MISSING, BLOCKED.

| Capability | Shared/domain | HUI | Linux | Windows | Notes |
|---|---|---|---|---|---|
| Built-in Spaces | PASS | MISSING | PARTIAL | MISSING | Study/Shopping/Research/Agent records implemented |
| Create custom Space | PASS | MISSING | PARTIAL | MISSING | Persistent store proven by smoke |
| Edit Space | PASS | MISSING | PARTIAL | MISSING | Model supports name/description/model/instructions/thinking/examples |
| Archive/restore | PASS | MISSING | PARTIAL | MISSING | Registry implemented |
| Fork | PASS | MISSING | PARTIAL | MISSING | Built-in/custom fork with origin |
| Delete custom safely | PARTIAL | MISSING | PARTIAL | MISSING | registry protects built-ins; conversation detach service exists; shell orchestration pending |
| Attached files persist | PASS | MISSING | PARTIAL | MISSING | reference + permission metadata persists; picker pending |
| Current Space scope | PASS | MISSING | PARTIAL | MISSING | persistence implemented |
| Space conversation list | PASS | MISSING | PARTIAL | MISSING | neutral store contract/service; real CakeOS adapter pending |
| New Chat assigned to Space | PASS | MISSING | PARTIAL | MISSING | shared service proven with in-memory adapter |
| Reopen Space exposes conversations | PASS | MISSING | PARTIAL | MISSING | shared query implemented; shell integration pending |
| Study routing | PASS | MISSING | PARTIAL | MISSING | launch policy maps to Study |
| Tasks routing | PASS | MISSING | PARTIAL | MISSING | Agent maps to Tasks |
| Research routing | PASS | MISSING | PARTIAL | MISSING | Research maps to Chat/configured workspace |
| Preferred model | PASS | MISSING | MISSING | MISSING | metadata/context available; provider adapter pending |
| Instructions/context | PASS | MISSING | MISSING | MISSING | context composition implemented |
| Thinking mode | PASS | MISSING | MISSING | MISSING | metadata preserved |
| Examples | PASS | MISSING | MISSING | MISSING | context composition implemented |
| Generated surfaces | PARTIAL | MISSING | MISSING | MISSING | metadata preserved only |
| Layout document | PARTIAL | MISSING | MISSING | MISSING | opaque layout JSON placeholder only; proper CakeOS layout contract pending |
| Edit with Cake planning | MISSING | MISSING | MISSING | MISSING | requires current CakeOS model/planner integration |
| Responsive UI | MISSING | MISSING | MISSING | MISSING | HUI work not started |
| Linux graphical launch | N/A | MISSING | MISSING | N/A | no graphical claim |
| Windows QA executable | N/A | MISSING | N/A | MISSING | must derive from Linux/shared implementation |
