# 🕸️ Majinfwork - Unity State Graph

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6.3%2B-black?style=flat-square&logo=unity" alt="Unity 6.3+"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="MIT License"/>
  <img src="https://img.shields.io/badge/Version-0.0.1--preview-blue?style=flat-square" alt="Version"/>
</p>

**Majinfwork State Graph** is a **barebone** visual scripting tool for Unity designed for developers who prefer a minimalist starting point. It provides the essential infrastructure for ScriptableObject-based state machines while remaining intentionally lightweight, allowing you to expand the logic without dealing with bloated, unnecessary features.

<img width="1695" height="893" alt="image" src="https://github.com/user-attachments/assets/53938a0d-2ba2-40fe-bf1a-d7b321cc2f1a" />

## 🚀 Features

*   **Visual Node Editor:** A custom GraphView-based editor for creating and connecting states.
*   **ScriptableObject Backend:** Graphs and nodes are saved as assets, making them highly reusable and memory-efficient.
*   **Runtime Debugging:** Real-time visual highlighting of the currently active state during Play Mode.
*   **Decoupled Logic:** Separate your game logic (States) from your physical actors.
*   **Majingari Integration:** Built-in support for resolving cross-scene references when used alongside the **Majingari Framework**.

---

## 🛠️ Core Components

### **1. State Graph Asset**
The container for your state machine. It stores all node data and identifies the **Entry Node** where the logic begins.

### **2. State Node Asset**
The base class for all individual states. To create custom logic, inherit from this class and override the following methods:
*   `Begin()`: Triggered when entering the state.
*   `Tick()`: Triggered every frame while the state is active.
*   `End()` : Triggered when exiting the state.

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
*   **Create Nodes:** Right-click inside the editor or press the spacebar to open the **Search Window**.
*   **Set Entry:** Right-click a node and select **Set as Entry Node**.
*   **Connect:** Click and drag from an **Output Port** (Transition) to an **Input Port** (Enter) of another node.

### **Standard Node Library**
The package includes several pre-built nodes to get you started:
*   **Wait State:** Pauses execution for a specified duration.
*   **Log State:** Prints a message to the Unity Console.
*   **Flip Flop:** Alternates between two different exit paths.

---

## 💻 Custom State Example

Creating your own logic is straightforward. Simply create a new C# script:

```csharp
using Majinfwork.StateGraph;
using UnityEngine;

public class MyCustomState : StateNodeAsset {
    public StateTransition OnConditionMet;

    public override void Begin() {
        Debug.Log("Entered Custom State");
    }

    public override void Tick() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            TriggerExit(OnConditionMet); // Transitions to the linked node
        }
    }

    public override void End() { }
}
```

---

## 🔗 Framework Integration

If you are using the core **Majingari Framework**, the State Graph automatically supports **Cross-Scene References**.
*   Nodes can reference objects in different scenes using the `[CrossSceneReference]` attribute.
*   The `StateGraphAsset` will automatically resolve these links when cloned for runtime use.

---

## ⚙️ Requirements
*   **Unity:** 6000.3.1f1 or newer.
*   **Dependencies:** Core Majingari Framework (Optional, for cross-scene support).

---

**Analogy for Understanding:**
Think of the **State Graph** as a **Train Track Layout**. The `StateGraphAsset` is the **Map** of the entire station. Each `StateNodeAsset` is a **Station Stop** where specific actions happen (like picking up passengers). The `StateRunner` is the **Train** itself, moving from station to station based on the tracks (Transitions) you laid out in the editor.
