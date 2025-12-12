# Handheld Camera Usage Guide

## Overview
The handheld camera is a device that allows you to take photos in-game and place them on walls as decorations.

## How to Use

### Taking Photos
1. Get a camera (spawn with admin commands: `/spawn HandheldCamera`)
2. Hold the camera in your hand
3. Press the use key (default: Z) to take a photo
4. A photo item will appear in your hands (or drop if hands are full)
5. Wait 2 seconds before taking another photo (cooldown)

### Placing Photos on Walls
1. Hold a photo item in your hand
2. Press the use key (default: Z) to convert it to a framed photo
3. The framed photo will spawn at your location
4. Pick up the framed photo
5. Aim at a wall and click to place it (like placing posters or signs)

## Technical Details

### Entity IDs
- `HandheldCamera` - The camera item
- `Photo` - The photo item (before framing)
- `PhotoFrame` - The framed photo (wallmount)

### Spawning Items
Use these commands as an admin:
- `/spawn HandheldCamera` - Spawn a camera
- `/spawn Photo` - Spawn a photo item
- `/spawn PhotoFrame` - Spawn a framed photo

### Audio
- Camera uses: `/Audio/Machines/shutter.ogg`
- Photo framing: `/Audio/Effects/poster_being_set.ogg`
- Photo destruction: `/Audio/Effects/glass_break1.ogg`

### Sprites
- Camera: `Objects/Devices/camera.rsi`
- Photo: `Objects/Devices/photo.rsi`
- Frame: `Structures/Wallmounts/photo_frames.rsi`

## Notes
- Photos are placeholders with simple colored sprites
- Future enhancements could include actual screenshot capture
- Photos can be destroyed by dealing 10+ damage to them
