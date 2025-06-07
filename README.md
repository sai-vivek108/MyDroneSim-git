# Real-Time Drone Swarm Control Testbed with AI Navigation and Telemetry

## Author
Sai Vivek Chava

## Demo
![Demo Video](ss/MyDroneSimDemo.mp4)
## Table of Contents
1. [Core Control System](#core-control-system)
2. [UI and Visualization](#ui-and-visualization)
3. [Input Handling System](#input-handling-system)
4. [Network and Communication](#network-and-communication)
5. [Management and Coordination](#management-and-coordination)
6. [System Architecture](#system-architecture)
7. [References](#references)

## Core Control System

### DroneController.cs
**Purpose**: The central controller for drone behavior, movement, and state management.

**Primary Functions**:
- Handle drone physics and movement calculations
- Manage input type switching (Keyboard, AI, etc.)
- Control network ownership and synchronization
- Implement drone selection and control transfer
- Provide status updates for UI elements

**Important Methods**:
- `HandleDroneSelection`: Manages drone selection and ownership transfer
- `SetInputType`: Switches between different input handlers
- `UpdateStatus`: Sends position, velocity, and grounded state updates

**Customization Options**:
- **Flight Characteristics**: Adjust `maxSpeed`, `pitchAmount`, `yawAmount`, and `rollAmount` to change flight behavior
- **Hover Settings**: Modify `enableHover`, `hoverAmplitude`, and `hoverFrequency` to adjust hover behavior
- **Ground Interaction**: Change `groundCheckDistance`, `decelerateOnGround`, and `decelSpeedOnGround` to modify ground interaction
- **Network Settings**: Adjust `networkUpdateInterval` to change how frequently network updates are sent

**Implementation Notes**:
- To add new flight modes, extend the `DroneController` class and override the `UpdateMovement` method
- To implement battery functionality, add a battery property and update the UI accordingly
- To add new status information, modify the `OnStatusUpdate` event to include additional parameters

### BaseFlyController.cs
**Purpose**: Base class providing common flight mechanics for all flying vehicles.

**Key Features**:
- Implements basic flight physics
- Provides movement calculations
- Handles rotation and thrust

**Customization Options**:
- **Physics Parameters**: Adjust `maxSpeed`, `thrustForce`, and other physics-related variables
- **Movement Constraints**: Modify `disablePitch`, `disableRoll`, and `disableYaw` to restrict movement
- **Auto Movement**: Change `autoForwardMovement` to enable/disable automatic forward movement

**Implementation Notes**:
- To create a new flying vehicle type, inherit from `BaseFlyController` and customize the movement behavior
- To add special effects (like afterburners), override the `Update` method and add your effects there
- To implement different flight modes, add a flight mode enum and modify the `UpdateMovement` method accordingly

## UI and Visualization

### DroneVideoPanelItem.cs
**Purpose**: Manages individual drone video panels in the UI.

**Key Features**:
- Displays drone camera feed
- Shows altitude, speed, and battery information
- Provides selection functionality
- Updates status indicators

**Important Methods**:
- `Initialize`: Sets up the panel with a specific drone
- `UpdateStatus`: Updates UI elements with current drone data
- `OnSelectButtonClicked`: Handles drone selection

**Customization Options**:
- **UI Elements**: Add new UI elements by creating serialized fields and connecting them in the Inspector
- **Update Interval**: Change `updateInterval` to adjust how frequently the UI updates
- **Status Colors**: Modify `normalStatusColor`, `warningStatusColor`, and `dangerStatusColor` to change status indicators
- **Selection Color**: Adjust `selectedColor` to change the appearance of selected drones

**Implementation Notes**:
- To add new status information, modify the `UpdateStatus` method to display additional data
- To change the video feed resolution, adjust the `RenderTexture` creation parameters
- To implement custom selection behavior, modify the `OnSelectButtonClicked` method

### DroneVideoPanel.cs
**Purpose**: Manages the collection of drone video panels.

**Key Features**:
- Creates and arranges video panels in a grid
- Handles adding and removing drone views
- Manages camera feed connections

**Important Methods**:
- `AddDroneView`: Creates a new panel for a drone
- `RemoveDroneView`: Removes a panel when a drone is disconnected
- `UpdatePanelLayout`: Arranges panels in a grid layout

**Customization Options**:
- **Panel Layout**: Adjust `maxPanels`, `panelSize`, and `panelSpacing` to change the grid layout
- **Video Quality**: Modify the `RenderTexture` creation parameters to change video quality
- **Panel Prefab**: Create a custom panel prefab and assign it to `videoPanelPrefab`

**Implementation Notes**:
- To implement a different layout system, override the `UpdatePanelLayout` method
- To add filtering or sorting of drones, modify the `AddDroneView` method
- To implement panel grouping, add a grouping system to the `DroneVideoPanel` class

### MinimapManager.cs
**Purpose**: Manages the minimap display showing drone positions.

**Key Features**:
- Displays drone positions on a minimap
- Updates positions in real-time
- Provides navigation assistance

**Customization Options**:
- **Map Size**: Adjust the map scale and boundaries
- **Icon Appearance**: Modify the appearance of drone icons on the minimap
- **Update Frequency**: Change how often positions are updated

**Implementation Notes**:
- To add terrain features to the minimap, modify the minimap texture or add terrain markers
- To implement zoom functionality, add zoom controls and adjust the map scale accordingly
- To add path visualization, implement a path drawing system in the `MinimapManager`

## Input Handling System

### IInputHandler.cs
**Purpose**: Interface defining the contract for all input handlers.

**Key Features**:
- Defines properties for flight controls (Pitch, Roll, Yaw, Lift)
- Specifies methods for input handling

**Implementation Notes**:
- To add new input properties, extend the interface with additional properties
- To implement a new input type, create a class that implements this interface
- To add new input methods, extend the interface with additional method signatures

### BaseInputHandler.cs
**Purpose**: Abstract base class implementing common input handling functionality.

**Key Features**:
- Implements IInputHandler interface
- Provides common input processing logic
- Manages input state tracking

**Customization Options**:
- **Input Evaluation**: Modify the `EvaluateAnyKeyDown` method to change how input state is determined
- **Update Logic**: Override the `Update` method to implement custom update behavior

**Implementation Notes**:
- To add new input properties, extend the class with additional properties
- To implement custom input processing, override the `UpdateInputs` method
- To add input validation, implement validation logic in the `HandleInputs` method

### KeyboardInputHandler.cs
**Purpose**: Handles keyboard input for drone control.

**Key Features**:
- Maps keyboard keys to flight controls
- Implements smooth input transitions
- Provides configurable input sensitivity

**Important Methods**:
- `HandleInputs`: Processes keyboard input
- `CalculateSmoothedInput`: Applies smoothing to raw input
- `CalculateRotorResponse`: Simulates rotor response delay

**Customization Options**:
- **Key Mapping**: Modify the `KeyInputs` class to change which keys control which actions
- **Input Sensitivity**: Adjust `inputSmoothness`, `inputAcceleration`, and `inputDeceleration` to change input feel
- **Rotor Response**: Modify `rotorResponseDelay`, `rotorSmoothness`, `rotorAcceleration`, and `rotorDeceleration` to change rotor behavior

**Implementation Notes**:
- To add new keyboard controls, modify the `HandleInputs` method to detect additional keys
- To implement key combinations, add logic to detect multiple keys pressed simultaneously
- To add input profiles, create a system to switch between different key mappings

### AIInputHandler.cs
**Purpose**: Provides AI-controlled flight behavior.

**Key Features**:
- Implements various AI behaviors (Follow, Wander, Waypoint)
- Handles obstacle avoidance
- Uses PID controllers for stable flight

**Important Methods**:
- `HandleFollow`: Implements target following behavior
- `HandleWander`: Implements random wandering behavior
- `HandlePatrol`: Implements waypoint patrolling
- `DetectObstacles`: Implements obstacle detection and avoidance

**Customization Options**:
- **AI Behavior**: Change `aiType` to switch between different AI behaviors
- **Obstacle Avoidance**: Adjust `obstacleDetectionDistance`, `numberOfRays`, and `raySpreadAngle` to modify obstacle avoidance
- **PID Controllers**: Modify the PID controller parameters to change flight stability
- **Wander Settings**: Adjust `wanderRadius` and `wanderTimer` to change wandering behavior
- **Waypoint Settings**: Configure `wayPointHolder` to set up patrol routes

**Implementation Notes**:
- To add a new AI behavior, create a new method similar to `HandleFollow` and add a new enum value to `AIType`
- To improve obstacle avoidance, enhance the `DetectObstacles` method with more sophisticated detection
- To implement formation flying, add formation logic to coordinate multiple drones

### JoystickInputHandler.cs
**Purpose**: Handles joystick/gamepad input for drone control.

**Key Features**:
- Maps joystick axes to flight controls
- Provides configurable sensitivity
- Implements input smoothing

**Customization Options**:
- **Axis Mapping**: Modify which joystick axes control which flight actions
- **Sensitivity**: Adjust sensitivity parameters to change input responsiveness
- **Deadzone**: Implement deadzone settings to ignore small joystick movements

**Implementation Notes**:
- To support additional joystick buttons, extend the input handling to detect button presses
- To implement different joystick profiles, create a system to switch between configurations
- To add force feedback, implement haptic feedback based on flight conditions

### MobileInputHandler.cs
**Purpose**: Handles touch input for mobile devices.

**Key Features**:
- Maps touch gestures to flight controls
- Provides mobile-specific UI elements
- Adapts to different screen sizes

**Customization Options**:
- **Touch Controls**: Modify how touch gestures are interpreted
- **UI Layout**: Adjust the mobile control UI layout
- **Sensitivity**: Change touch sensitivity parameters

**Implementation Notes**:
- To add new touch gestures, extend the touch detection logic
- To implement tilt controls, add accelerometer-based control
- To support different mobile devices, add device-specific adjustments

## Network and Communication

### DroneCommunication.cs
**Purpose**: Handles communication between drones and external systems.

**Key Features**:
-	Implements message sending and receiving
-	Manages communication protocols
-	Handles status updates

**Customization Options**:
-	Message Format: Modify the message format to include additional information
-	Update Frequency: Change how often status updates are sent
-	Protocol Settings: Adjust communication protocol parameters

**Implementation Notes**:
-	To add new message types, extend the message handling system
-	To implement secure communication, add encryption to the message system
-	To support different communication protocols, create a protocol abstraction layer

 
 
### CommunicationManager.cs
**Purpose**: Manages overall communication in the system.

**Key Features**:
-	Coordinates communication between components
-	Handles message routing
-	Manages communication protocols

**Customization Options**:
-	Routing Rules: Modify how messages are routed between components
-	Protocol Selection: Change which communication protocols are used
-	Message Filtering: Implement message filtering based on content or source

**Implementation Notes**:
-	To add new communication channels, extend the communication system
-	To implement message prioritization, add priority levels to messages
-	To support different communication modes, create a mode switching system


 
### NetworkManagerUI.cs
**Purpose**: Provides UI for network connection management.

**Key Features**:
-	Displays connection status
-	Provides host/client selection
-	Shows connection options

**Customization Options**:
-	UI Layout: Modify the network UI layout
-	Connection Options: Add additional connection options
-	Status Display: Change how connection status is displayed

**Implementation Notes**:
-	To add new connection types, extend the connection options
-	To implement connection diagnostics, add diagnostic tools to the UI
-	To support different network configurations, create a configuration system

 
## Management and Coordination

### DroneManager.cs
**Purpose**: Central manager for all drones in the system.

**Key Features**:
-	Registers and tracks all drones
-	Manages drone spawning and despawning
-	Coordinates drone selection
-	Handles network client connections

**Important Methods**:
•	‘RegisterDrone’: Adds a drone to the management system
•	‘SpawnDroneForClient’: Creates a new drone for a connecting client
•	‘OnClientConnected’: Handles new client connections
•	‘OnClientDisconnected’: Handles client disconnections

**Customization Options**:
-	Spawn Behavior: Modify ‘GetRandomSpawnPosition’ to change where drones spawn
-	Drone Registration: Change how drones are registered and tracked
-	Client Handling: Adjust how client connections are managed

**Implementation Notes**:
-	To implement drone teams, add team assignment logic to the registration system
-	To add drone persistence, implement a system to save and load drone states
-	To support different drone types, extend the drone spawning system


# System Architecture

The drone simulation system follows a modular architecture with clear separation of concerns:

1. **Core Control**: The `DroneController` and `BaseFlyController` provide the fundamental flight mechanics.
2. **Input Handling**: Various input handlers implement the `IInputHandler` interface to provide different control methods.
3. **UI and Visualization**: The video panel system provides visual feedback and control interfaces.
4. **Network and Communication**: Communication components enable multiplayer functionality.
5. **Management**: The `DroneManager` coordinates all components and manages the overall system.

## Interaction Flow

- 1.	User selects a drone through the UI
- 2.	‘DroneVideoPanelItem’ triggers the ‘OnDroneSelected’ event
- 3.	‘DroneController’ receives the selection and:
    * 1.	Changes network ownership to the host
    * 2.	Switches input type to keyboard
    * 3.	Updates visual indicators
- 4.	The host can now control the selected drone
- 5.	Status updates are sent to the UI for display

![Workflow Architecture Diagram](/ss/System%20architechture.png "Workflow Architecture")

## Extension Points

The system is designed with several extension points to facilitate future development:

1. **New Input Types**: Implement new classes that inherit from `BaseInputHandler` to add new control methods
2. **New Drone Types**: Create new classes that inherit from `DroneController` to implement specialized drones
3. **New UI Elements**: Extend the `DroneVideoPanelItem` to display additional information
4. **New AI Behaviors**: Add new behavior methods to `AIInputHandler` to implement new AI patterns
5. **New Communication Protocols**: Extend the communication system to support additional protocols

# References

### Unity Assets and Packages
- **[Easy Flying System with AI Addon](https://assetstore.unity.com/packages/templates/packs/easy-flying-system-with-ai-add-on-228389)**: Base drone control system and AI implementation
- **Unity Standard Assets**: Used for basic input handling and camera controls
- **Unity UI System**: For creating the video panel interface and minimap
- **[CodeMonkey](https://unitycodemonkey.com/kitchenchaosmultiplayercourse.php)**: Best content for beginners with no previous experience with unity.
- **[Suburbs](https://assetstore.unity.com/packages/3d/environments/urban/suburb-neighborhood-house-pack-modular-72712)**
- **[CityScape](https://assetstore.unity.com/packages/3d/environments/urban/town-constructor-3-71070)**


### External Libraries
- **Unity Networking**: For multiplayer functionality and network synchronization
- **Unity Input System**: For handling various input devices (keyboard, joystick, mobile)
- **Unity UI Toolkit**: For creating responsive and dynamic user interfaces

### Development Tools
- **Unity Editor**: Version 2022.3 LTS
- **Visual Studio**: For C# development and debugging
- **Git**: For version control and collaboration

### Documentation Resources
- [Unity Documentation](https://docs.unity3d.com/)
- [Unity Input System Documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html)
- [Unity Networking Documentation](https://docs.unity3d.com/Manual/UNetOverview.html)
- [Unity AI Navigation](https://docs.unity3d.com/Manual/nav-Overview.html)

### Acknowledgments
- **Unity Technologies**: For providing the game engine and development tools
- **RageRun Games**: For the Easy Flying System with AI Addon
- **Open Source Community**: For various tools and resources used in development
- **AI Tools(ChatGPT and Gemini)**: For error resolution and structured format of project and documentation.
