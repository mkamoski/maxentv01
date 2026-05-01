# CartPole Chart Implementation Summary

## What Was Added

### 1. CartPoleChartService (Services/CartPoleChartService.cs)
A comprehensive service for generating and managing training charts:

**Features:**
- Generates 3-subplot charts showing:
  - Episode Rewards (total reward per episode)
  - Episode Duration (steps survived per episode)
  - Policy Entropy (exploration metric per episode)
- Each subplot includes raw data and 5-episode moving average
- Color-coded for clarity (green=rewards, blue=steps, orange=entropy)

**File Management:**
- Saves charts with timestamp format: `YYYY-MM-dd-HH-mm-ss-fffff-cartpole.png`
- Creates `output/` folder automatically
- Provides cleanup method to delete charts older than 28 days
- Opens charts in Windows 11 default image viewer

**Technical Details:**
- Uses ScottPlot 5.x for chart generation
- Uses ImageSharp for combining three plots into one image
- Creates three temporary plots and combines them vertically
- Cleans up temporary files after combining

### 2. Updated CartPole.razor Page
Added new functionality:
- **View Graph button**: Opens the most recent chart in default image viewer
- **Cleanup Old Charts button**: Removes charts older than 28 days
- **Training history tracking**: Records rewards, steps, and entropy per episode
- **Automatic chart generation**: After training completes, chart is automatically generated
- **Output log integration**: Displays chart save path in the log

### 3. Updated MauiProgram.cs
- Registered `CartPoleChartService` as a singleton service
- Available for dependency injection throughout the app

### 4. Updated .csproj
Added new package dependencies:
- `ScottPlot` (5.1.58) - Core plotting library
- `SixLabors.ImageSharp` (3.1.5) - Image manipulation for combining plots

### 5. Updated .gitignore
- Added `output/` folder to prevent committing chart images to git

## How It Works

### Training Flow
1. User clicks **Run**
2. Agent trains for specified episodes
3. Each episode's metrics (reward, steps, entropy) are recorded
4. After training completes:
   - `CartPoleChartService.GenerateTrainingChart()` is called
   - Three separate plots are created with ScottPlot
   - Plots are saved as temporary PNG files
   - ImageSharp combines the three images vertically
   - Final combined image is saved with timestamp
   - Temporary files are deleted
   - Full path is displayed in output log

### Viewing Charts
1. After chart generation, **View Graph** button becomes enabled
2. Clicking it calls `CartPoleChartService.OpenChartInViewer()`
3. Uses `Process.Start()` with `UseShellExecute = true`
4. Windows opens the PNG in default image viewer (Photos app)

### File Cleanup
1. Click **Cleanup Old Charts** button
2. Service scans `output/` folder for `*-cartpole.png` files
3. Compares `LastWriteTime` to cutoff date (28 days ago)
4. Deletes files older than cutoff
5. Reports number of files deleted in output log

## File Naming Convention
Format: `2025-05-01-15-45-30-12345-cartpole.png`
- `YYYY-MM-dd`: Date
- `HH-mm-ss`: Time
- `fffff`: Microseconds (5 digits for uniqueness)
- `cartpole`: Identifies the chart type
- `.png`: Image format

This ensures:
- Chronological sorting
- No filename collisions
- Easy identification
- Human-readable timestamps

## Testing Checklist
- [ ] Run training with 10 episodes
- [ ] Verify chart is generated in `output/` folder
- [ ] Check that output log shows chart path
- [ ] Click "View Graph" to open in Windows viewer
- [ ] Verify chart shows all three subplots
- [ ] Verify moving averages are displayed
- [ ] Run cleanup to test deletion logic
- [ ] Verify old files are removed (if any exist)

## Future Enhancements
Possible improvements:
- Add chart export to other formats (SVG, PDF)
- Add comparison view for multiple training runs
- Add real-time chart updates during training
- Add configurable chart styling options
- Add chart gallery view to browse past runs
- Add chart metadata (parameters used, training time, etc.)
