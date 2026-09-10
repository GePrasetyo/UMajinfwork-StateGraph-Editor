# 🕸️ Majinfwork - Unity State Graph

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6.4%2B-black?style=flat-square&logo=unity" alt="Unity 6.4+"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT License"/>
  <img src="https://img.shields.io/badge/Version-0.0.1--preview-blue?style=flat-square" alt="Version"/>
</p>

**Majinfwork State Graph** is a **barebone** state machine tool for Unity designed for developers who prefer a minimalist starting point. States are plain ScriptableObjects executed by a direct virtual call — no graph interpretation at runtime — and authoring runs on Unity's Graph Toolkit.

<img width="1695" height="893" alt="image" src="https://github.com/user-attachments/assets/53938a0d-2ba2-40fe-bf1a-d7b321cc2f1a" />

## 🚀 Features

*   **Visual Node Editor:** A custom GraphView-based editor. Node fields are drawn with `PropertyField`, so every custom `PropertyDrawer` and attribute works, and backward transitions route orthogonally instead of cutting through nodes.
*   **ScriptableObject Backend:** Graphs and nodes are saved as assets, making them highly reusable and memory-efficient.
*   **Runtime Debugging:** Real-time visual highlighting of the currently active state during Play Mode.
*   **Shared Graphs:** One graph asset drives any number of agents with no per-spawn clone (0.17 us / 92 B per agent, versus 1.39 ms / 2.9 KB to clone).
*   **Decoupled Logic:** Separate your game logic (States) from your physical actors.
*   **Majingari Integration:** Built-in support for resolving cross-scene references when used alongside the **Majingari Framework**.

---

## 🛠️ Core Components

### **1. State Graph Asset**
The container for your state machine. It stores all node data and identifies the **Entry Node** where the logic begins.

### **2. State Node Asset**
The base class for all individual states. Override the lifecycle you need:
*   `Begin(StateContext)` / `Tick(StateContext)` / `End(StateContext)` — for states
    shared across many agents. Keep mutable data in `ctx.GetData<T>(this)`.
*   `Begin()` / `Tick()` / `End()` — for states there is only ever one of.

### **3. State Runner**
A MonoBehaviour component that lives on your GameObjects. It holds the **Runtime Graph** and manages the transition between states.

### **4. State Transition**
A simple class used to define "Exit" ports on your nodes. These appear as output ports in the editor, allowing you to visually link nodes.

---

## 📖 Usage Guide

### **Creating a New Graph**
1.  Right-click in the Project window.
2.  Select **MFramework > State Machine > Graph Asset**.
3.  Double-click the asset to open the **State Machine Editor**.

### **Adding and Connecting Nodes**
*   **Create Nodes:** Right-click inside the editor or press spacebar to open the
    search window, which lists every `StateNodeAsset` type in the project. The node
    is created as a sub-asset of the graph.
*   **Edit State Data:** Serialized fields are drawn inside the node itself.
*   **Set Entry:** The first node becomes the entry automatically. Right-click any
    node and choose **Set as Entry Node** to change it.
*   **Connect:** Drag from an **Output Port** (a `StateTransition` field) to another
    node's **Enter** port.

Assign the graph asset to a `StateRunner` and press play. There is no build step —
the asset you author is the asset that runs.

### **Optional: Graph Toolkit front-end**
`Editor/Gtk/` holds an experimental Graph Toolkit front-end that authors a
`.mstategraph` and bakes into a `StateGraphAsset`. It is not the primary editor:
Graph Toolkit has no public API for custom property drawers or nested
`[Serializable]` types, so states holding those — anything using
`[CrossSceneReference]`, for instance — cannot be edited on the node. It also has
no wire routing; backward transitions are tidied manually with portals.

### **Standard Node Library**
The package includes several pre-built states to get you started:
*   **Wait State:** Pauses execution for a specified duration.
*   **Log State:** Prints a message to the Unity Console.
*   **Flip Flop:** Alternates between two different exit paths.

---

## 💻 Custom State Example

States come in two flavours. Pick by how many agents will run the graph.

### Shared graph — many agents, no per-spawn clone

Keep every mutable value in the `StateContext`. One graph asset then drives any
number of agents, so spawning costs an allocation instead of a graph clone
(measured: **0.17 µs / 92 B per agent**, versus **1.39 ms / 2.9 KB** to clone).
This is what you want for AI.

```csharp
using Majinfwork.StateGraph;
using UnityEngine;

public class ChaseState : StateNodeAsset {
    public StateTransition onReached;
    public float stoppingDistance = 1.5f;

    // One instance of this per agent, created on first entry.
    private sealed class Data {
        public float elapsed;
    }

    public override void Begin(StateContext ctx) {
        ctx.GetData<Data>(this).elapsed = 0f;
    }

    public override void Tick(StateContext ctx) {
        var data = ctx.GetData<Data>(this);
        data.elapsed += Time.deltaTime;

        var agent = ctx.Runner.transform;
        if (agent.position.magnitude < stoppingDistance) {
            TriggerExit(onReached);
        }
    }
}
```

Set **Shared Asset** on the graph (the `Entry` node's option in the Graph Toolkit
editor, or the `SharedAsset` field on `StateGraphAsset`).

### Single-instance state — mutable fields are fine

For a state there is only ever one of, such as a game-level state, plain fields
work and the no-argument lifecycle is enough. The graph is cloned per runner.

```csharp
public class TitleScreenState : StateNodeAsset {
    public StateTransition toGame;
    private bool menuOpen;

    public override void Begin() { menuOpen = false; }

    public override void Tick() {
        if (Input.GetKeyDown(KeyCode.Space)) TriggerExit(toGame);
    }
}
```

### Exiting from async code

`TriggerExit` resolves the current agent only while a lifecycle call is on the
stack. After an `await`, capture the context and use it directly:

```csharp
public override void Begin(StateContext ctx) => LoadAsync(ctx);

private async void LoadAsync(StateContext ctx) {
    await SomeOperation();
    ctx.Exit(onComplete); // picked up on the next tick
}
```

### Reporting progress

Implement `IStateProgress` and the Graph Toolkit editor draws a fill bar on the
running node during Play Mode.

```csharp
public float GetProgress(StateContext ctx) => /* 0..1 for this agent */;
```

---

## 🔗 Framework Integration

If you are using the core **Majingari Framework**, the State Graph automatically supports **Cross-Scene References**.
*   Nodes can reference objects in different scenes using the `[CrossSceneReference]` attribute.
*   The `StateGraphAsset` will automatically resolve these links when cloned for runtime use.

---

## ⚙️ Requirements
*   **Unity:** 6000.3.1f1 or newer. The optional Graph Toolkit front-end needs 6000.4+.
*   **Dependencies:** Core Majingari Framework (Optional, for cross-scene support).
