# Model compatibility

| CineForge version | Wan pack | State | Hardware verified | Notes |
|---|---|---|---|---|
| 0.4.0 RC | Wan 2.2 I2V A14B scaled-FP8 | Release candidate | RTX 4070 12 GB | Native load, two-expert generation, telemetry, finite frames, and MP4 export passed; clean-machine install remains. |
| 0.2.0 | None | Engineering preview | RTX 4070 12 GB diagnostic inference | The native engine was verified with a small diagnostic image pipeline, not Wan. |

The CineForge Desktop installer creates an isolated CineForge Library and automatically downloads the four core components from `TheBaldDudeCo/CineForge-Wan-Models`. The download path is not a compatibility claim; the pack remains experimental until native generation passes every validation gate.

Compatibility is earned through real generation, not inferred from filenames or successful discovery.
