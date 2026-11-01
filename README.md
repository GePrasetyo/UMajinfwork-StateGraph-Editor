# 🕸️ Majinfwork - Unity State Graph

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6.6%2B-black?style=flat-square&logo=unity" alt="Unity 6.6+"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT License"/>
  <img src="https://img.shields.io/badge/Version-0.1.0-blue?style=flat-square" alt="Version"/>
</p>

**Majinfwork State Graph** is a **barebone** state machine tool for Unity designed for developers who prefer a minimalist starting point. States are plain ScriptableObjects executed by a direct virtual call — no graph interpretation at runtime — and graphs are authored in Unity's Graph Toolkit.

## 🚀 Features

*   **Graph Toolkit Editor:** Graphs are `.mstategraph` files edited in Unity's Graph Toolkit. Each node draws its state's fields through the state's own serialization, so every `PropertyDrawer` and attribute works. Long wires, such as a loop back to an earlier state, can be turned into **portals** (right-click a wire > **Convert to Portals**).
*   **Baked, Not Interpreted:** An importer bakes each graph into a `StateGraphAsset` with one ScriptableObject per state. Every exit knows the state it belongs to, so the runtime never searches for it.
*   **Runtime Debugging:** In Play Mode, open graphs mark the running states: the selected runner's state when a runner is selected, every runner's otherwise.
*   **Shared Graphs:** One graph asset drives any number of agents with no per-spawn clone (0.17 us / 92 B per agent, versus 1.39 ms / 2.9 KB to clone).
*   **Decoupled Logic:** Separate your game logic (States) from your physical actors.

---

## 🛠️ Core Components

### **1. State Graph (`.mstategraph`)**
The authored graph: an **Entry** node and one node per state, wired from exits to **Enter** ports.

### **2. State Graph Asset**
What the importer bakes from a State Graph and what a runner runs. It holds the states and the entry state. Assign the `.mstategraph` itself wherever a `StateGraphAsset` is expected.

### **3. State Node Asset**
The base class for all individual states. Override the lifecycle you need:
*   `Begin(StateContext)` / `Tick(StateContext)` / `End(StateContext)` — for states
    shared across many agents. Keep mutable data in `ctx.GetData<T>(this)`.
*   `Begin()` / `Tick()` / `End()` — for states there is only ever one of.

### **4. State Runner**
A MonoBehaviour component that lives on your GameObjects. It holds the **Runtime Graph** and manages the transition between states.

### **5. State Transition**
A field type that declares an exit. Each `StateTransition` field becomes an output port on the state's node.

---

## 📖 Usage Guide

### **Creating a New Graph**
1.  Right-click in the Project window.
2.  Select **MFramework > State Machine > State Graph**.
3.  Double-click the graph to open it in Graph Toolkit.

### **Making a State Available in Graphs**
Graph Toolkit lists one node class per entry, so each state type gets a one-line node class in an Editor assembly that references `MState_Editor`:

```csharp
using System;
using Majinfwork.StateGraph;

[Serializable] public class ChaseStateNode : StateNode<ChaseState> { }
```

### **Building the Graph**
*   **Add States:** Press space or right-click in the graph and pick a state.
*   **Edit State Data:** The state's fields are drawn inside its node. Its exits are the node's ports.
*   **Name a State:** Select its node and set **Name** in the Graph Inspector. The node shows the name, and the baked state is called by it.
*   **Set the Entry:** Wire the **Entry** node's **Start** port to the first state. A graph has exactly one Entry.
*   **Connect:** Drag from an exit port to another node's **Enter** port.
*   **Portals:** Right-click a wire and choose **Convert to Portals** to replace it with a pair of short stubs.

Saving the graph bakes it. Assign the graph to a `StateRunner` and press play.

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

Turn on **Shared Asset** on the graph's **Entry** node.

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
    await SomeOperation(ctx.ExitToken);
    ctx.Exit(onComplete); // picked up on the next tick
}
```

`ctx.ExitToken` is cancelled when the state ends. An exit that arrives after its state has ended is ignored: every exit knows the state it belongs to.

---

## 🔗 Framework Integration

With the core **Majingari Framework**, a state can point at objects in scenes through a `CrossSceneRef<T>` field. Pick the object on the node; the field keeps the id of the object's `CrossSceneAnchor` and resolves it at runtime while its scene is loaded.

---

## ⚙️ Requirements
*   **Unity:** 6000.6 or newer (Graph Toolkit).
*   **Dependencies:** Core Majingari Framework (optional, for cross-scene references).
