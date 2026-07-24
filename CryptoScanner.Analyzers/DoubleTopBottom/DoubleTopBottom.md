# Double Top / Double Bottom (DoubleTopBottom)

## Overview

The **DoubleTopBottom** strategy is an experimental placeholder for recognising classic double top and double bottom reversal patterns. It is currently **non-functional** — the signal logic is entirely commented out and no strategies are registered. The source notes: "This was just a thought, there is lots of noise!!!!"

## Status

- No strategies registered (the `Strategies` list in the plugin is empty).
- The only signal file (`SignalDoubleTopBottomShort.cs`) has its `IsSignal()` logic fully commented out.
- This strategy serves as a skeleton for future experimentation.

## Intended approach

The original concept was to detect double-top and double-bottom chart patterns, but the high false-positive rate ("lots of noise") prevented a working implementation.

## File structure

```
CryptoScanner.Analyzers/DoubleTopBottom/
├── DoubleTopBottomPlugin.cs                  # Plugin shell (no strategies registered)
├── DoubleTopBottomSettings.cs                # Settings (base only, sound files)
├── DoubleTopBottom.md                        # This document
└── Signal/
    └── SignalDoubleTopBottomShort.cs          # Entirely commented out
```

## Strategy type

- **Pattern recognition (placeholder / non-functional)**
- Experimental (DEBUG-only, no active registration)

## Registration

Not registered. The plugin exists but has an empty strategies list.
