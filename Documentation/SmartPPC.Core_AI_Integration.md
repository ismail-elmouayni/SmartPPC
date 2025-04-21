# Design Document: Integrating AI for DDMRP Buffer Placement in SmartPPC.Core

**Version:** 1.0
**Date:** April 21, 2025

## 1. Introduction

### 1.1 Current System

The `SmartPPC.Core` library currently employs a Genetic Algorithm (GA) based solver (`GnSolver`) to determine the optimal placement of decoupling buffers in a production line modeled according to Demand Driven Material Requirements Planning (DDMRP) principles. The GA explores various buffer configurations (`Chromosome`), evaluates their effectiveness using a simulation model (`ProductionControlModel`) and a fitness function (`Fitness`), aiming to minimize buffer count while maximizing demand satisfaction (`MinBuffersAndMaxDemandsObjective`).

### 1.2 Goal

This document proposes integrating Artificial Intelligence (AI) techniques, specifically Neural Networks (NN) or potentially Large Language Models (LLM), to enhance or replace the GA-based buffer placement strategy. The objectives could include:

*   **Faster Optimization:** AI models, once trained, might predict near-optimal configurations much faster than the iterative GA process.
*   **Improved Solution Quality:** AI might learn complex relationships within the production system that the GA struggles to find efficiently, potentially leading to better buffer placements.
*   **Adaptability:** An AI model could potentially be designed to adapt more quickly to changes in input parameters or system dynamics (though this requires more advanced techniques like online learning).

## 2. Proposed AI Approach: Predictive Buffer Placement Model

The primary proposed approach is to train an AI model to directly predict an effective buffer configuration (`buffersActivation` array) given the characteristics of the production system (`ModelInputs`).

### 2.1 Model Type

*   **Neural Network (NN):** A Feedforward Neural Network (FNN) is a suitable starting point.
    *   **Input Layer:** Represents the engineered features derived from `ModelInputs`.
    *   **Hidden Layers:** One or more layers to learn complex patterns.
    *   **Output Layer:** Neurons corresponding to each potential buffer location (station). The output could be probabilities (using sigmoid activation) indicating the likelihood that a buffer should be placed at each station. These probabilities would then be thresholded (e.g., > 0.5) to generate the final binary `buffersActivation` array.
*   **Large Language Model (LLM):** Using an LLM directly for this structured prediction task is less conventional. It might be explored by framing the problem as generating a textual representation of the buffer configuration based on a detailed textual description of the `ModelInputs`. However, this presents significant challenges in ensuring valid output formats and likely requires fine-tuning on specialized data. **Recommendation:** Start with NN approaches due to their better fit for structured input/output tasks.

### 2.2 Input Features (Feature Engineering)

This is a critical step. The raw `ModelInputs` need to be transformed into a fixed-size numerical vector suitable for an NN. Potential features include:

*   **Global Features:** `PlanningHorizon`, `PastHorizon`, `PeakHorizon`.
*   **Station-Specific Features (potentially flattened or aggregated):**
    *   Processing times.
    *   Lead times (for input stations).
    *   Demand forecast statistics (mean, variance, max, min) for output stations.
    *   Demand variability (for output stations).
    *   Indicators for input/output stations.
*   **Relational Features (encoding the Bill of Materials / Workflow):**
    *   Graph-based features (e.g., node degrees, centrality measures from the `StationPrecedences` graph).
    *   Flattened adjacency matrices (`StationPrecedences`, `StationInputPrecedences`).
    *   Calculated cumulative lead times or flow times along paths.

### 2.3 Output

The model's output layer will have `N` neurons, where `N` is the number of stations (`ModelInputs.NumberOfStations`). Each neuron `i` outputs a value (e.g., probability `p_i`) representing the model's confidence that station `i` should have a buffer.

**Post-processing:** Convert the output probabilities `[p_0, p_1, ..., p_{N-1}]` into the binary `buffersActivation` array `[b_0, b_1, ..., b_{N-1}]` using a threshold (e.g., `b_i = 1` if `p_i > 0.5`, else `0`).

### 2.4 Training Data Generation

The AI model needs supervised training data. This data can be generated using the existing infrastructure:

1.  **Generate Diverse Inputs:** Create a large set of varied `ModelInputs` JSON files, covering different numbers of stations, structures, lead times, demand patterns, etc.
2.  **Run GA Solver:** For each generated `ModelInputs` file, run the current `GnSolver` to find the best `buffersActivation` array it can discover.
3.  **Create Training Pairs:** Each pair consists of:
    *   **Input:** The engineered feature vector derived from a `ModelInputs` file.
    *   **Target Output:** The corresponding optimal `buffersActivation` array found by the GA.

### 2.5 Training Process

*   **Framework:** Use a standard ML framework (e.g., TensorFlow.NET, ML.NET, PyTorch.NET, or Python frameworks like TensorFlow/PyTorch with model serving/ONNX).
*   **Loss Function:** Binary Cross-Entropy is suitable for comparing the predicted probabilities against the target binary `buffersActivation` array.
*   **Optimizer:** Standard optimizers like Adam.
*   **Validation:** Split the generated data into training and validation sets to monitor for overfitting.

## 3. Implementation Changes in `SmartPPC.Core`

### 3.1 New AI Solver

*   Create a new class `AISolver` (e.g., `SmartPPC.Core.Solver.AI.AISolver`) that implements `IProductionControlSolver`.
*   `Initialize(string configFilePath)`: Loads `ModelInputs` similar to `GnSolver`. It might also load the pre-trained AI model (e.g., from an ONNX file or a saved model format).
*   `Resolve()`:
    1.  Perform feature engineering on the loaded `_modelInputs` to create the input vector for the AI model.
    2.  Feed the input vector to the loaded AI model to get the predicted output (e.g., probabilities).
    3.  Post-process the AI output to get the final `buffersActivation` array.
    4.  Create the `ProductionControlModel` using `ModelBuilder.CreateFromInputs(_modelInputs)`.
    5.  Apply the predicted `buffersActivation` using `controlModel.PlanBasedOnBuffersPositions(buffersActivation)`.
    6.  Return an `OptimizationResult` containing the configured `controlModel`. (Note: The concept of a "fitness curve" doesn't directly apply here as the AI gives a single prediction, but we could potentially return a confidence score from the model).

### 3.2 AI Model Integration

*   Decide on the model format (e.g., ONNX for interoperability).
*   Add necessary NuGet packages for the chosen ML framework/runtime (e.g., `Microsoft.ML.OnnxRuntime`).
*   Implement model loading and prediction logic within `AISolver`.

### 3.3 Feature Engineering Component

*   Create helper classes/methods responsible for transforming `ModelInputs` into the numerical feature vector required by the AI model. This logic could reside within the `SmartPPC.Core.Solver.AI` namespace.

## 4. Alternative AI Integrations (Brief Discussion)

*   **AI-Guided GA:** Use an NN to predict promising areas of the search space to initialize the GA population or guide mutation/crossover operators. This retains the explorative nature of the GA but potentially speeds it up.
*   **AI-based Fitness Evaluation:** Train an NN to *predict* the `ObjectiveFunctionValue` for a given `buffersActivation` and `ModelInputs`, potentially replacing the expensive `PlanBasedOnBuffersPositions` simulation *within* the GA's fitness evaluation loop. This could drastically speed up the GA if the prediction is accurate enough.

## 5. Challenges and Considerations

*   **Data Generation:** Creating a sufficiently large and diverse dataset representing various production scenarios is crucial and potentially time-consuming.
*   **Feature Engineering:** Designing effective features that capture the complex dynamics of the DDMRP system is non-trivial.
*   **Model Training & Tuning:** Standard ML challenges apply (hyperparameter tuning, avoiding overfitting, choosing the right architecture).
*   **Interpretability:** NN models can be "black boxes". Understanding *why* the AI chose a specific buffer configuration might be difficult.
*   **Optimality:** The AI model learns from the data generated by the GA. It might struggle to find solutions significantly *better* than the GA's best, unless the training process or model architecture allows for discovering novel patterns. Its main advantage might be speed.
*   **Maintenance:** AI models require retraining if the underlying system dynamics or desired objectives change significantly.

## 6. Future Work

*   Explore Reinforcement Learning (RL) where an agent learns to place buffers by interacting with the simulation environment (`ProductionControlModel`).
*   Investigate online learning to allow the model to adapt to changing demand patterns or production parameters over time.
*   Hybrid approaches combining GA exploration with AI prediction.
