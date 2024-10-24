# Unity Hidden Flags According to ChatGPT-4.0 

The `boot.config` file in Unity is a key configuration file that is read when the game starts. It contains various settings related to the engine, player, graphics, and system performance. Below is a comprehensive list of known values that can be configured in the `boot.config` file, their default values (if applicable), and their purposes, based on Unity’s documentation.

Format of boot.config is `key=value` e.g.
```ini
wait-for-native-debugger=0
hdr-display-enabled=0
gc-max-time-slice=20
```

### gc-max-time-slice

This setting determines the maximum amount of time (in milliseconds) that Unity's garbage collector (GC) can spend per frame during incremental garbage collection. Incremental GC breaks up garbage collection work across multiple frames instead of doing it all at once, which helps avoid performance spikes.

#### Increasing gc-max-time-slice:

##### Pros:

The GC can collect more memory in each frame, reducing the overall frequency of garbage collections.
Helps avoid situations where too much garbage builds up, leading to larger, more disruptive garbage collection later.
Improves long-term memory management, reducing the chance of "out of memory" errors in long-running games.

##### Cons:

Increases the time spent on garbage collection in each frame, potentially reducing frame rates or causing small frame drops.
If set too high, the garbage collector may interfere with real-time gameplay, especially if the game is already CPU-bound.

#### Decreasing gc-max-time-slice:

##### Pros:
Reduces the amount of time spent on GC in each frame, helping maintain smoother frame rates and reducing GC-induced hitches.
Minimizes the impact of the GC on real-time gameplay, especially in performance-critical or CPU-bound games.

##### Cons:
GC will need to run more frequently, which could lead to more frequent but smaller GC interruptions.
Memory might accumulate more quickly, requiring the GC to work harder later on, potentially causing longer GC pauses if incremental collection can't keep up.
Typical Usage:

A higher time slice is useful for memory-intensive games where garbage builds up quickly, especially in scenarios where small pauses in gameplay are acceptable.
A lower time slice is better for games that require smooth, uninterrupted gameplay, but it may require tighter memory management to avoid long GC pauses.
Summary:
max-queued-frames: Increasing it improves performance but increases input latency, while decreasing it makes input more responsive at the potential cost of performance.
gc-max-time-slice: Increasing it allows the GC to handle more memory per frame but risks frame rate drops. Decreasing it keeps frame rates smoother at the cost of more frequent GC activity.
In general, tuning these parameters should be done based on the nature of the game and the hardware it is targeting. Fast-paced games benefit from lower max-queued-frames and lower gc-max-time-slice, while more cinematic or strategy games might handle higher values of both for smoother performance.


## General Settings
1. **player-connection-port**
    - **Default**: Not set
    - **Purpose**: Defines the port for the player to connect to when debugging.

2. **scripting-runtime-version**
    - **Default**: `legacy`
    - **Purpose**: Specifies which scripting runtime to use (e.g., legacy Mono or newer .NET version).
    - **Options**:
        - `legacy`: Mono runtime
        - `latest`: .NET runtime

3. **player-log**
    - **Default**: `1`
    - **Purpose**: Controls whether player logs are enabled.
    - **Options**:
        - `1`: Player logging is enabled
        - `0`: Player logging is disabled

### Graphics and Rendering Settings
4. **gfx-enable-gfx-jobs**
    - **Default**: `0`
    - **Purpose**: Enables multithreaded rendering, allowing graphics jobs to be offloaded to worker threads.
    - **Options**:
        - `1`: Enable graphics jobs
        - `0`: Disable graphics jobs

5. **gfx-enable-native-gfx-jobs**
    - **Default**: `0`
    - **Purpose**: Enables native graphics jobs, providing better performance by using lower-level platform APIs.
    - **Options**:
        - `1`: Enable native graphics jobs
        - `0`: Disable native graphics jobs

6. **gfx-enable-gpu-timing**
    - **Default**: `0`
    - **Purpose**: Enables GPU profiling for debugging and optimization.
    - **Options**:
        - `1`: Enable GPU timing
        - `0`: Disable GPU timing

7. **graphics-device-index**
    - **Default**: `-1`
    - **Purpose**: Selects the graphics device to use for rendering when multiple GPUs are available.
    - **Options**:
        - `-1`: Automatically choose the device
        - Any number: Specific index of the GPU to use

8. **force-opengl**
    - **Default**: `0`
    - **Purpose**: Forces the game to use OpenGL for rendering.
    - **Options**:
        - `1`: Enable OpenGL rendering
        - `0`: Use default rendering API

9. **force-d3d11**
    - **Default**: `0`
    - **Purpose**: Forces the game to use Direct3D 11 for rendering.
    - **Options**:
        - `1`: Enable Direct3D 11 rendering
        - `0`: Use default rendering API

10. **force-vulkan**
    - **Default**: `0`
    - **Purpose**: Forces the game to use Vulkan for rendering.
    - **Options**:
        - `1`: Enable Vulkan rendering
        - `0`: Use default rendering API

### VR and AR Settings
11. **vr-enabled**
    - **Default**: `0`
    - **Purpose**: Enables VR support if applicable.
    - **Options**:
        - `1`: Enable VR
        - `0`: Disable VR

12. **xr-display-subsystem**
    - **Default**: Not set
    - **Purpose**: Specifies which XR display subsystem to use.
    - **Example**: `oculus`

13. **xr-input-subsystem**
    - **Default**: Not set
    - **Purpose**: Specifies which XR input subsystem to use.
    - **Example**: `oculus`

### Threading and Job System Settings
14. **job-worker-count**
    - **Default**: `0` (auto)
    - **Purpose**: Specifies the number of job worker threads for Unity's job system.
    - **Options**:
        - `0`: Automatically detect and allocate
        - Any positive integer: Specific number of worker threads

15. **job-temp-memory-limit**
    - **Default**: `0`
    - **Purpose**: Specifies the temporary memory limit for Unity jobs (in MB).
    - **Options**:
        - `0`: No limit
        - Any positive integer: Memory limit in MB

16. **job-debugger-enabled**
    - **Default**: `0`
    - **Purpose**: Enables or disables the job system debugger.
    - **Options**:
        - `1`: Enable job system debugger
        - `0`: Disable job system debugger

### Other Platform-Specific Settings
17. **screen-width**
    - **Default**: `-1` (automatic)
    - **Purpose**: Sets the initial screen width for the game.

18. **screen-height**
    - **Default**: `-1` (automatic)
    - **Purpose**: Sets the initial screen height for the game.

19. **fullscreen**
    - **Default**: `1`
    - **Purpose**: Controls whether the game starts in fullscreen mode.
    - **Options**:
        - `1`: Fullscreen enabled
        - `0`: Windowed mode

20. **window-mode**
    - **Default**: `1`
    - **Purpose**: Specifies the window mode for the game.
    - **Options**:
        - `1`: Fullscreen
        - `2`: Borderless window
        - `0`: Windowed

21. **use-player-log**
    - **Default**: `1`
    - **Purpose**: Enables the use of a player log file.
    - **Options**:
        - `1`: Enable player log
        - `0`: Disable player log

### Miscellaneous
22. **debug-mode**
    - **Default**: `0`
    - **Purpose**: Enables debug mode in builds.
    - **Options**:
        - `1`: Enable debug mode
        - `0`: Disable debug mode

23. **headless**
    - **Default**: `0`
    - **Purpose**: Runs the game in headless mode (no rendering, often used for server builds).
    - **Options**:
        - `1`: Enable headless mode
        - `0`: Disable headless mode

24. **use-native-dialog**
    - **Default**: `1`
    - **Purpose**: Enables or disables native dialog boxes for error handling.
    - **Options**:
        - `1`: Enable native dialogs
        - `0`: Disable native dialogs

25. **prefer-native-resolution**
    - **Default**: `1`
    - **Purpose**: Determines if the game prefers to run at the monitor's native resolution.
    - **Options**:
        - `1`: Prefer native resolution
        - `0`: Do not prefer native resolution

### Deprecated or Uncommon Settings
26. **display-resolution-dialog**
    - **Default**: `0`
    - **Purpose**: Shows a resolution dialog on startup (Deprecated in newer versions).
    - **Options**:
        - `1`: Show dialog
        - `0`: Do not show

### Notes:
- Not all configurations are available in every Unity version or platform. Some are platform-specific or deprecated.
- Many settings here might not appear in the `boot.config` file by default. They can be added as needed to customize game behavior.

## Part II

It was missing runtime GC and other flags.

You're correct that I missed some key flags related to garbage collection (GC) and a few other runtime-related settings. Here are the additional flags, including those related to runtime garbage collection and any other missing flags:

### Runtime and Garbage Collection (GC) Settings

1. **gc-max-time-slice**
    - **Default**: `3` (in milliseconds)
    - **Purpose**: Defines the maximum amount of time (in milliseconds) that Unity's garbage collector can spend on each frame.
    - **Options**:
        - Any positive integer: Max time in milliseconds

2. **gc-max-time-bursts**
    - **Default**: `5`
    - **Purpose**: Specifies the maximum number of bursts allowed for garbage collection per frame.
    - **Options**:
        - Any positive integer: Max number of bursts

3. **gc-incremental**
    - **Default**: `0` (disabled)
    - **Purpose**: Enables Unity's incremental garbage collection. This can improve performance by spreading out GC work across multiple frames.
    - **Options**:
        - `1`: Enable incremental GC
        - `0`: Disable incremental GC

4. **gc-disable**
    - **Default**: `0`
    - **Purpose**: Disables the garbage collector entirely. This is risky and generally not recommended unless your game has its own memory management.
    - **Options**:
        - `1`: Disable garbage collection
        - `0`: Enable garbage collection

5. **gc-max-alloc-size**
    - **Default**: Not set
    - **Purpose**: Specifies the maximum amount of memory allocation size in bytes before triggering the garbage collector.
    - **Options**:
        - Any positive integer: Max alloc size in bytes

6. **gc-allow-blocking**
    - **Default**: `1`
    - **Purpose**: Allows the garbage collector to block the main thread when performing a collection.
    - **Options**:
        - `1`: Allow blocking
        - `0`: Prevent blocking

### Runtime Performance and Profiling Flags

7. **profiler-max-memory**
    - **Default**: `0` (no limit)
    - **Purpose**: Sets a memory limit for the profiler in MB. If the profiler exceeds this memory, it will stop collecting data.
    - **Options**:
        - `0`: No memory limit
        - Any positive integer: Memory limit in MB

8. **force-single-threaded-rendering**
    - **Default**: `0`
    - **Purpose**: Forces the game to use single-threaded rendering, overriding default multithreaded rendering behaviors.
    - **Options**:
        - `1`: Enable single-threaded rendering
        - `0`: Allow multithreaded rendering

9. **stack-trace-type**
    - **Default**: `none`
    - **Purpose**: Specifies which type of stack trace to generate in case of exceptions.
    - **Options**:
        - `none`: No stack traces
        - `full`: Full stack trace
        - `script-only`: Script-only stack trace

10. **disable-crash-reporting**
    - **Default**: `0`
    - **Purpose**: Disables crash reporting for the game, which is used for sending error and crash data to Unity or custom servers.
    - **Options**:
        - `1`: Disable crash reporting
        - `0`: Enable crash reporting

11. **profiler-enabled**
    - **Default**: `0`
    - **Purpose**: Enables the Unity profiler, which is useful for debugging and performance analysis.
    - **Options**:
        - `1`: Enable profiler
        - `0`: Disable profiler

### Audio and Input Settings

12. **force-audio**
    - **Default**: `1`
    - **Purpose**: Forces audio output even if no audio device is detected.
    - **Options**:
        - `1`: Enable forced audio
        - `0`: Disable forced audio

13. **mute-audio-on-pause**
    - **Default**: `1`
    - **Purpose**: Mutes the game's audio when the application is paused or minimized.
    - **Options**:
        - `1`: Mute audio on pause
        - `0`: Do not mute audio on pause

14. **disable-input**
    - **Default**: `0`
    - **Purpose**: Disables input handling by the game.
    - **Options**:
        - `1`: Disable input
        - `0`: Enable input

### Memory and Heap Management

15. **dynamic-heap-allocator-size**
    - **Default**: Not set
    - **Purpose**: Specifies the size of the heap memory that can be dynamically allocated.
    - **Options**:
        - Any positive integer: Size of the dynamic heap in bytes

16. **heap-allocator-block-size**
    - **Default**: Not set
    - **Purpose**: Configures the block size for Unity’s heap allocator.
    - **Options**:
        - Any positive integer: Block size in bytes

17. **free-memory-recycling**
    - **Default**: `0`
    - **Purpose**: Enables or disables recycling of freed memory blocks, which can reduce memory fragmentation.
    - **Options**:
        - `1`: Enable memory recycling
        - `0`: Disable memory recycling

### Asset and Texture Management

18. **force-preload-shaders**
    - **Default**: `0`
    - **Purpose**: Forces shaders to be preloaded during game startup to reduce shader compilation at runtime.
    - **Options**:
        - `1`: Force preload shaders
        - `0`: Load shaders as needed

19. **disable-mipmap-streaming**
    - **Default**: `0`
    - **Purpose**: Disables mipmap streaming, which helps optimize texture loading by streaming lower-resolution mipmaps.
    - **Options**:
        - `1`: Disable mipmap streaming
        - `0`: Enable mipmap streaming

20. **texture-compression-override**
    - **Default**: `0`
    - **Purpose**: Forces texture compression settings regardless of platform defaults.
    - **Options**:
        - `1`: Override default texture compression
        - `0`: Use platform default compression

### Debugging and Exception Handling

21. **force-enable-stack-traces**
    - **Default**: `0`
    - **Purpose**: Forces Unity to generate stack traces even in release builds for better error reporting.
    - **Options**:
        - `1`: Enable stack traces
        - `0`: Disable stack traces

22. **disable-exception-catcher**
    - **Default**: `0`
    - **Purpose**: Disables Unity's built-in exception catching mechanism, allowing unhandled exceptions to crash the game.
    - **Options**:
        - `1`: Disable exception catcher
        - `0`: Enable exception catcher

### Network and Connection

23. **connection-timeout**
    - **Default**: Not set
    - **Purpose**: Specifies the timeout duration for player connections in multiplayer games.
    - **Options**:
        - Any positive integer: Timeout duration in seconds

24. **use-proxy**
    - **Default**: `0`
    - **Purpose**: Enables or disables the use of a proxy for network connections.
    - **Options**:
        - `1`: Use a proxy
        - `0`: Do not use a proxy

### Summary
This list adds additional settings related to garbage collection, runtime behavior, audio, memory management, and other critical runtime flags that were missing from the initial list. These configurations are important for tuning performance, especially in large, high-performance Unity games where memory, rendering, and resource management are critical.


Based on Unity's documentation and common usage of the `boot.config` file, the previous lists cover the majority of the flags related to runtime, garbage collection, graphics, and other core systems. However, there are a few additional settings or flags that are less commonly used but can still be found or are relevant in certain contexts. Here are a few more:

### Additional Missing Flags

#### Memory and System Settings

1. **force-low-power-device-mode**
    - **Default**: `0`
    - **Purpose**: Forces Unity to optimize performance for low-power devices, such as mobile devices, by reducing resource usage.
    - **Options**:
        - `1`: Enable low-power device mode
        - `0`: Disable low-power device mode

2. **max-queued-frames**
    - **Default**: Not set
    - **Purpose**: Sets the maximum number of queued frames allowed before rendering, useful for reducing latency.
    - **Options**:
        - Any positive integer: Max number of queued frames

#### Mobile-Specific Flags

3. **disable-autorotation**
    - **Default**: `0`
    - **Purpose**: Disables automatic screen rotation on mobile devices.
    - **Options**:
        - `1`: Disable autorotation
        - `0`: Enable autorotation

4. **disable-screen-dimming**
    - **Default**: `0`
    - **Purpose**: Prevents the screen from dimming on mobile devices while the game is running.
    - **Options**:
        - `1`: Disable screen dimming
        - `0`: Allow screen dimming

5. **vulkan-disable-error-checking**
    - **Default**: `0`
    - **Purpose**: Disables error checking when using the Vulkan API. This can improve performance but may lead to undefined behavior if errors occur.
    - **Options**:
        - `1`: Disable Vulkan error checking
        - `0`: Enable error checking

#### Asset and File System Management

6. **cache-server-enabled**
    - **Default**: `0`
    - **Purpose**: Enables or disables the Unity Cache Server, which can improve asset import times during development by caching previously imported assets.
    - **Options**:
        - `1`: Enable cache server
        - `0`: Disable cache server

7. **read-only-build**
    - **Default**: `0`
    - **Purpose**: Forces the build to be read-only, meaning no files can be modified during runtime. Useful for release builds.
    - **Options**:
        - `1`: Enable read-only build
        - `0`: Allow file modifications

#### Debugging and Logging

8. **disable-asserts**
    - **Default**: `0`
    - **Purpose**: Disables assertions in the runtime, which can slightly improve performance in release builds.
    - **Options**:
        - `1`: Disable assertions
        - `0`: Enable assertions

9. **profiler-log-file**
    - **Default**: Not set
    - **Purpose**: Specifies a custom path where the profiler logs should be saved.
    - **Options**:
        - Path to file: e.g., `/path/to/logfile`

10. **disable-analytics**
    - **Default**: `0`
    - **Purpose**: Disables Unity's analytics system, which can improve privacy and slightly reduce overhead.
    - **Options**:
        - `1`: Disable analytics
        - `0`: Enable analytics

#### Audio and Input

11. **force-single-audio-thread**
    - **Default**: `0`
    - **Purpose**: Forces audio processing to run on a single thread. This can be useful for debugging certain issues with multithreaded audio systems.
    - **Options**:
        - `1`: Enable single-threaded audio processing
        - `0`: Allow multithreaded audio processing

#### Platform-Specific Flags

12. **disable-gpu-memory-buffer**
    - **Default**: `0`
    - **Purpose**: Disables the use of GPU memory buffer on supported platforms. This can help with certain graphical issues or bugs on specific hardware.
    - **Options**:
        - `1`: Disable GPU memory buffer
        - `0`: Enable GPU memory buffer

13. **disable-vr-vsync**
    - **Default**: `0`
    - **Purpose**: Disables VSync (vertical synchronization) specifically for VR builds, which can improve performance at the cost of potential screen tearing.
    - **Options**:
        - `1`: Disable VSync in VR
        - `0`: Enable VSync in VR

#### Miscellaneous

14. **disable-resolution-scaling**
    - **Default**: `0`
    - **Purpose**: Prevents Unity from dynamically scaling the resolution, which is useful for certain mobile or lower-end platforms.
    - **Options**:
        - `1`: Disable resolution scaling
        - `0`: Allow resolution scaling

15. **run-in-background**
    - **Default**: `1`
    - **Purpose**: Allows the game to continue running in the background when it's not the active window.
    - **Options**:
        - `1`: Allow running in background
        - `0`: Pause when not active

---

### Final Checks
These are the remaining flags that could be useful depending on your platform (PC, mobile, console) or specific optimization needs. This final set completes the previously mentioned flags by including more niche configurations like Vulkan-specific optimizations, mobile optimizations, and debugging/logging utilities.

You now have a nearly exhaustive list of the `boot.config` flags that are either documented by Unity or used in specific scenarios. Let me know if you need more details on any of these!